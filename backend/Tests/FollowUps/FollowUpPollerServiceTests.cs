using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;
using NeverMissLead.Infrastructure.BackgroundServices;
using NeverMissLead.Infrastructure.Persistence;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.FollowUps;

public sealed class FollowUpPollerServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeTimeProvider _fakeTime;
    private readonly Guid _businessId;
    private readonly Guid _leadId;

    public FollowUpPollerServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        _serviceProvider = services.BuildServiceProvider();
        _db = _serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Set initial test time: 2026-08-22 10:00:00 UTC
        _fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 8, 22, 10, 0, 0, TimeSpan.Zero));

        var business = Business.Create("Bright Minds Coaching", "private tutors");
        _businessId = business.Id;
        _db.Businesses.Add(business);

        var conv = Conversation.Start(_businessId, "visitor-1");
        _db.Conversations.Add(conv);

        var lead = Lead.Capture(conv.Id, _businessId, "ENC_Name", "ENC_Phone", "ENC_Email", "Trial class inquiry", 100);
        _leadId = lead.Id;
        _db.Leads.Add(lead);

        _db.SaveChanges();
    }

    [Fact]
    public async Task ProcessPendingTasks_DueTask_MarksAsSent()
    {
        // Arrange: Task scheduled 30 minutes in the past
        var scheduledTime = _fakeTime.GetUtcNow().UtcDateTime.AddMinutes(-30);
        var task = FollowUpTask.ScheduleForLead(
            _leadId,
            _businessId,
            scheduledTime,
            "Hi, checking in on your inquiry!");

        _db.FollowUpTasks.Add(task);
        await _db.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var poller = new FollowUpPollerService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            config,
            NullLogger<FollowUpPollerService>.Instance,
            _fakeTime);

        // Act
        var processedCount = await poller.ProcessPendingTasksAsync(CancellationToken.None);

        // Assert
        processedCount.ShouldBe(1);

        var updatedTask = await _db.FollowUpTasks.AsNoTracking().FirstAsync(t => t.Id == task.Id);
        updatedTask.Status.ShouldBe(FollowUpStatus.Sent);
        updatedTask.SentAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task ProcessPendingTasks_FutureTask_IsNotProcessed()
    {
        // Arrange: Task scheduled 2 hours in the future
        var scheduledTime = _fakeTime.GetUtcNow().UtcDateTime.AddHours(2);
        var task = FollowUpTask.ScheduleForLead(
            _leadId,
            _businessId,
            scheduledTime,
            "Future follow up");

        _db.FollowUpTasks.Add(task);
        await _db.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var poller = new FollowUpPollerService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            config,
            NullLogger<FollowUpPollerService>.Instance,
            _fakeTime);

        // Act
        var processedCount = await poller.ProcessPendingTasksAsync(CancellationToken.None);

        // Assert
        processedCount.ShouldBe(0);

        var updatedTask = await _db.FollowUpTasks.FirstAsync(t => t.Id == task.Id);
        updatedTask.Status.ShouldBe(FollowUpStatus.Pending);
        updatedTask.SentAt.ShouldBeNull();
    }

    [Fact]
    public async Task ProcessPendingTasks_AlreadySentTask_DoesNotDoubleFire()
    {
        // Arrange: Task scheduled in the past, but already Sent
        var scheduledTime = _fakeTime.GetUtcNow().UtcDateTime.AddHours(-1);
        var task = FollowUpTask.ScheduleForLead(
            _leadId,
            _businessId,
            scheduledTime,
            "Already sent follow up");
        task.MarkSent();

        _db.FollowUpTasks.Add(task);
        await _db.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var poller = new FollowUpPollerService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            config,
            NullLogger<FollowUpPollerService>.Instance,
            _fakeTime);

        // Act
        var processedCount = await poller.ProcessPendingTasksAsync(CancellationToken.None);

        // Assert
        processedCount.ShouldBe(0);
    }

    [Fact]
    public async Task ContactCapture_SchedulesOneHourFollowUpTask()
    {
        // Arrange
        var encryptionService = Substitute.For<IEncryptionService>();
        encryptionService.Encrypt(Arg.Any<string>()).Returns("ENC");
        var handler = new Application.Leads.Commands.ContactCapture.ContactCaptureCommandHandler(_db, encryptionService);

        var conv = Conversation.Start(_businessId, "visitor-cc");
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync();

        var command = new Application.Leads.Commands.ContactCapture.ContactCaptureCommand(
            _businessId,
            conv.Id,
            Name: "Test Lead",
            Phone: "555-1234",
            Email: "lead@test.com",
            IntentSummary: "Test intent");

        // Act
        var leadId = await handler.Handle(command, CancellationToken.None);

        // Assert
        var task = await _db.FollowUpTasks.AsNoTracking().FirstOrDefaultAsync(t => t.LeadId == leadId);
        task.ShouldNotBeNull();
        task.BusinessId.ShouldBe(_businessId);
        task.Status.ShouldBe(FollowUpStatus.Pending);
        // ScheduledFor should be ~1 hour after creation
        task.ScheduledFor.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(50));
        task.ScheduledFor.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddMinutes(65));
    }

    [Fact]
    public async Task SendMessage_Abstention_SchedulesFourHourEscalationTask()
    {
        // Arrange
        var ragClient = Substitute.For<IRagClient>();
        ragClient.QueryAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<RetrievedChunk>>([]));

        var llmClient = Substitute.For<ILlmClient>();
        var handler = new Application.Conversations.Commands.SendMessage.SendMessageCommandHandler(_db, ragClient, llmClient);

        var conv = Conversation.Start(_businessId, "visitor-handoff");
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync();

        var command = new Application.Conversations.Commands.SendMessage.SendMessageCommand(
            _businessId,
            conv.Id,
            "Do you provide scuba diving lessons?");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.NeedsHuman.ShouldBeTrue();
        var escalationTask = await _db.FollowUpTasks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ConversationId == conv.Id && t.BusinessId == _businessId);

        escalationTask.ShouldNotBeNull();
        escalationTask.Status.ShouldBe(FollowUpStatus.Pending);
        // ScheduledFor should be ~4 hours from now
        escalationTask.ScheduledFor.ShouldBeGreaterThan(DateTime.UtcNow.AddHours(3.8));
        escalationTask.ScheduledFor.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddHours(4.2));
    }

    public void Dispose()
    {
        _db.Dispose();
        _serviceProvider.Dispose();
    }
}
