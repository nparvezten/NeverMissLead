using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Conversations.Queries.GetConversationDetails;
using NeverMissLead.Application.Conversations.Queries.GetConversations;
using NeverMissLead.Application.Conversations.Queries.GetUnansweredQuestions;
using NeverMissLead.Application.FollowUps.Queries.GetFollowUpTasks;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Application.Leads.Commands.UpdateLeadStatus;
using NeverMissLead.Application.Leads.Queries.GetLeads;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;
using NeverMissLead.Infrastructure.Persistence;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.Dashboard;

public sealed class DashboardQueriesTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IEncryptionService _encryptionService;
    private readonly Guid _businessAId;
    private readonly Guid _businessBId;
    private readonly Guid _convA1Id;
    private readonly Guid _convA2HandoffId;
    private readonly Guid _leadAId;
    private readonly Guid _leadBId;

    public DashboardQueriesTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _encryptionService = Substitute.For<IEncryptionService>();

        _encryptionService.Decrypt(Arg.Any<string>())
            .Returns(x => x[0] != null ? x[0].ToString()!.Replace("ENC_", "") : null);

        // Seed Business A
        var bA = Business.Create("Bright Minds Coaching", "tutors");
        _businessAId = bA.Id;
        _db.Businesses.Add(bA);

        // Seed Business B
        var bB = Business.Create("Ace Tutors", "tutors");
        _businessBId = bB.Id;
        _db.Businesses.Add(bB);

        // Seed Conversation A1 (Normal)
        var convA1 = Conversation.Start(_businessAId, "visitor-A1");
        _convA1Id = convA1.Id;
        var msgA1User = Message.Create(convA1.Id, MessageRole.User, "What is your hourly rate?");
        var citedChunkGuid = Guid.NewGuid();
        var msgA1Assistant = Message.Create(convA1.Id, MessageRole.Assistant, "Our fee is $35/hr.", [citedChunkGuid]);
        _db.Conversations.Add(convA1);
        _db.Messages.AddRange(msgA1User, msgA1Assistant);

        // Seed Conversation A2 (NeedsHuman = true)
        var convA2 = Conversation.Start(_businessAId, "visitor-A2");
        _convA2HandoffId = convA2.Id;
        convA2.RequestHandoff(HandoffReason.NoCitedChunks);
        var msgA2User = Message.Create(convA2.Id, MessageRole.User, "Do you offer scuba diving classes?");
        var msgA2Assistant = Message.Create(convA2.Id, MessageRole.Assistant, "I'm not sure, let me get the owner.");
        _db.Conversations.Add(convA2);
        _db.Messages.AddRange(msgA2User, msgA2Assistant);

        // Seed Lead for Business A
        var leadA = Lead.Capture(
            convA1.Id,
            _businessAId,
            nameEnc: "ENC_Alice Morgan",
            phoneEnc: "ENC_+15550001",
            emailEnc: "ENC_alice@example.com",
            intentSummary: "Hourly rate query",
            qualificationScore: 100);
        _leadAId = leadA.Id;
        _db.Leads.Add(leadA);

        // Seed Lead for Business B
        var leadB = Lead.Capture(
            Guid.NewGuid(),
            _businessBId,
            nameEnc: "ENC_Bob Vance",
            phoneEnc: "ENC_+15550002",
            emailEnc: "ENC_bob@example.com",
            intentSummary: "Business B inquiry",
            qualificationScore: 60);
        _leadBId = leadB.Id;
        _db.Leads.Add(leadB);

        // Seed FollowUpTask for Business A
        var taskA = FollowUpTask.ScheduleForLead(
            _leadAId,
            _businessAId,
            DateTime.UtcNow.AddMinutes(-10),
            "Checking in with Alice");
        _db.FollowUpTasks.Add(taskA);

        _db.SaveChanges();
    }

    [Fact]
    public async Task GetLeads_ReturnsDecryptedPii_StrictlyScopedByBusiness()
    {
        // Arrange
        var handler = new GetLeadsQueryHandler(_db, _encryptionService);

        // Act
        var leads = await handler.Handle(new GetLeadsQuery(_businessAId), CancellationToken.None);

        // Assert
        leads.Count.ShouldBe(1);
        leads[0].Id.ShouldBe(_leadAId);
        leads[0].Name.ShouldBe("Alice Morgan");
        leads[0].Phone.ShouldBe("+15550001");
        leads[0].Email.ShouldBe("alice@example.com");
        leads[0].QualificationScore.ShouldBe(100);

        // Multi-tenant check: Business B's lead must NOT appear
        leads.ShouldNotContain(l => l.Id == _leadBId);
    }

    [Fact]
    public async Task GetConversationDetails_ReturnsThreadWithCitedChunks()
    {
        // Arrange
        var handler = new GetConversationDetailsQueryHandler(_db);

        // Act
        var details = await handler.Handle(new GetConversationDetailsQuery(_convA1Id, _businessAId), CancellationToken.None);

        // Assert
        details.ShouldNotBeNull();
        details.Id.ShouldBe(_convA1Id);
        details.Messages.Count.ShouldBe(2);
        details.Messages[1].Role.ShouldBe("assistant");
        details.Messages[1].CitedChunkIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetConversationDetails_CrossTenantAccess_ReturnsNull()
    {
        // Arrange: Business B tries to access Conversation A1
        var handler = new GetConversationDetailsQueryHandler(_db);

        // Act
        var details = await handler.Handle(new GetConversationDetailsQuery(_convA1Id, _businessBId), CancellationToken.None);

        // Assert
        details.ShouldBeNull();
    }

    [Fact]
    public async Task GetUnansweredQuestions_ReturnsOnlyHandoffConversations()
    {
        // Arrange
        var handler = new GetUnansweredQuestionsQueryHandler(_db);

        // Act
        var unanswered = await handler.Handle(new GetUnansweredQuestionsQuery(_businessAId), CancellationToken.None);

        // Assert
        unanswered.Count.ShouldBe(1);
        unanswered[0].ConversationId.ShouldBe(_convA2HandoffId);
        unanswered[0].Question.ShouldBe("Do you offer scuba diving classes?");
        unanswered[0].HandoffReason.ShouldBe(HandoffReason.NoCitedChunks.ToString());
    }

    [Fact]
    public async Task GetFollowUpTasks_ScopedByBusiness()
    {
        // Arrange
        var handler = new GetFollowUpTasksQueryHandler(_db);

        // Act
        var tasksA = await handler.Handle(new GetFollowUpTasksQuery(_businessAId), CancellationToken.None);
        var tasksB = await handler.Handle(new GetFollowUpTasksQuery(_businessBId), CancellationToken.None);

        // Assert
        tasksA.Count.ShouldBe(1);
        tasksA[0].LeadId.ShouldBe(_leadAId);

        tasksB.Count.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateLeadStatus_UpdatesStatus_WhenBusinessMatches()
    {
        // Arrange
        var handler = new UpdateLeadStatusCommandHandler(_db);

        // Act
        var result = await handler.Handle(
            new UpdateLeadStatusCommand(_leadAId, _businessAId, LeadStatus.Contacted),
            CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        var lead = await _db.Leads.FirstAsync(l => l.Id == _leadAId);
        lead.Status.ShouldBe(LeadStatus.Contacted);
    }

    [Fact]
    public async Task UpdateLeadStatus_CrossTenantAttempt_ThrowsKeyNotFound()
    {
        // Arrange: Business B tries to update Lead A
        var handler = new UpdateLeadStatusCommandHandler(_db);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new UpdateLeadStatusCommand(_leadAId, _businessBId, LeadStatus.Converted),
                CancellationToken.None));
    }

    public void Dispose() => _db.Dispose();
}
