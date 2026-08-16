using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Application.Leads.Commands.ContactCapture;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;
using NeverMissLead.Infrastructure.Persistence;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.Leads;

public sealed class ContactCaptureCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IEncryptionService _encryptionService;
    private readonly Guid _businessId;
    private readonly Guid _conversationId;

    public ContactCaptureCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _encryptionService = Substitute.For<IEncryptionService>();

        // Default mock encryption behavior
        _encryptionService.Encrypt(Arg.Any<string>())
            .Returns(x => x[0] != null ? $"ENC_{x[0]}" : null);

        // Seed Business & Conversation
        var business = Business.Create("Bright Minds Coaching", "private tutors", "UTC");
        _businessId = business.Id;
        _db.Businesses.Add(business);

        var conversation = Conversation.Start(_businessId, "visitor-123");
        _conversationId = conversation.Id;
        _db.Conversations.Add(conversation);

        _db.SaveChanges();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesLeadWithEncryptedPii()
    {
        // Arrange
        var handler = new ContactCaptureCommandHandler(_db, _encryptionService);
        var command = new ContactCaptureCommand(
            _businessId,
            _conversationId,
            Name: "John Doe",
            Phone: "+15551234567",
            Email: "john@example.com",
            IntentSummary: "Interested in Math tutoring for Grade 10");

        // Act
        var leadId = await handler.Handle(command, CancellationToken.None);

        // Assert
        leadId.ShouldNotBe(Guid.Empty);

        var lead = await _db.Leads.FirstAsync(l => l.Id == leadId);
        lead.BusinessId.ShouldBe(_businessId);
        lead.ConversationId.ShouldBe(_conversationId);
        lead.NameEnc.ShouldBe("ENC_John Doe");
        lead.PhoneEnc.ShouldBe("ENC_+15551234567");
        lead.EmailEnc.ShouldBe("ENC_john@example.com");
        lead.IntentSummary.ShouldBe("Interested in Math tutoring for Grade 10");
        lead.Status.ShouldBe(LeadStatus.New);
        lead.QualificationScore.ShouldBe(100); // Both phone & email
    }

    [Theory]
    [InlineData("+15551234567", "john@example.com", 100)]
    [InlineData(null, "john@example.com", 60)]
    [InlineData("+15551234567", null, 40)]
    public async Task Handle_QualificationScore_CalculatedCorrectly(string? phone, string? email, int expectedScore)
    {
        // Arrange
        var handler = new ContactCaptureCommandHandler(_db, _encryptionService);
        var command = new ContactCaptureCommand(
            _businessId,
            _conversationId,
            Name: "Jane Smith",
            Phone: phone,
            Email: email,
            IntentSummary: "Wants pricing");

        // Act
        var leadId = await handler.Handle(command, CancellationToken.None);

        // Assert
        var lead = await _db.Leads.FirstAsync(l => l.Id == leadId);
        lead.QualificationScore.ShouldBe(expectedScore);
    }

    [Fact]
    public async Task Handle_MissingPhone_StillSucceedsWithEmail()
    {
        // Arrange
        var handler = new ContactCaptureCommandHandler(_db, _encryptionService);
        var command = new ContactCaptureCommand(
            _businessId,
            _conversationId,
            Name: "Alice Walker",
            Phone: null,
            Email: "alice@example.com",
            IntentSummary: "Physics batch schedule");

        // Act
        var leadId = await handler.Handle(command, CancellationToken.None);

        // Assert
        var lead = await _db.Leads.FirstAsync(l => l.Id == leadId);
        lead.PhoneEnc.ShouldBeNull();
        lead.EmailEnc.ShouldBe("ENC_alice@example.com");
        lead.QualificationScore.ShouldBe(60);
    }

    public void Dispose() => _db.Dispose();
}
