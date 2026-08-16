using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Conversations.Commands.SendMessage;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;
using NeverMissLead.Infrastructure.Ai;
using NeverMissLead.Infrastructure.Persistence;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.Conversations;

public sealed class SendMessageCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IRagClient _ragClient;
    private readonly ILlmClient _llmClient;
    private readonly Guid _businessId;

    public SendMessageCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _ragClient = Substitute.For<IRagClient>();
        _llmClient = Substitute.For<ILlmClient>();

        // Seed a test business
        var business = Business.Create("Bright Minds Coaching", "private tutors", "Asia/Kolkata");
        _businessId = business.Id;
        _db.Businesses.Add(business);
        _db.SaveChanges();
    }

    [Fact]
    public async Task Handle_HappyPath_WithNullLlmClient_ReturnsCitedAnswer()
    {
        // Arrange
        var chunkId = Guid.NewGuid();
        var chunks = new List<RetrievedChunk>
        {
            new(chunkId, "High school math tutoring is $60 per hour.", 0.85)
        };

        _ragClient.QueryAsync(_businessId, "How much is math?", 4, Arg.Any<CancellationToken>())
            .Returns(chunks);

        var nullLlmClient = new NullLlmClient();
        var handler = new SendMessageCommandHandler(_db, _ragClient, nullLlmClient);

        var command = new SendMessageCommand(_businessId, null, "How much is math?");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.NeedsHuman.ShouldBeFalse();
        response.AssistantMessage.ShouldContain("Based on our FAQ: High school math tutoring is $60 per hour.");
        response.CitedChunkIds.ShouldContain(chunkId);
        response.ConversationId.ShouldNotBe(Guid.Empty);

        var messages = await _db.Messages.Where(m => m.ConversationId == response.ConversationId).ToListAsync();
        messages.Count.ShouldBe(2); // 1 User + 1 Assistant
        messages[0].Role.ShouldBe(MessageRole.User);
        messages[1].Role.ShouldBe(MessageRole.Assistant);
        messages[1].CitedChunkIds.ShouldContain(chunkId);
    }

    [Fact]
    public async Task Handle_Abstention_WhenNoChunksFound_FlagsNeedsHuman()
    {
        // Arrange
        _ragClient.QueryAsync(_businessId, "Do you offer scuba diving lessons?", 4, Arg.Any<CancellationToken>())
            .Returns(new List<RetrievedChunk>());

        var handler = new SendMessageCommandHandler(_db, _ragClient, _llmClient);
        var command = new SendMessageCommand(_businessId, null, "Do you offer scuba diving lessons?");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.NeedsHuman.ShouldBeTrue();
        response.AssistantMessage.ShouldBe("I'm not sure about that — let me connect you with the owner.");
        response.CitedChunkIds.ShouldBeEmpty();

        var conv = await _db.Conversations.FirstAsync(c => c.Id == response.ConversationId);
        conv.NeedsHuman.ShouldBeTrue();
        conv.HandoffReason.ShouldBe(HandoffReason.NoCitedChunks);
    }

    [Fact]
    public async Task Handle_LeadIntentKeywords_DetectsLeadIntent()
    {
        // Arrange
        var chunkId = Guid.NewGuid();
        _ragClient.QueryAsync(_businessId, "What is the fee and pricing for physics?", 4, Arg.Any<CancellationToken>())
            .Returns([new RetrievedChunk(chunkId, "Physics fee is $50/hr.", 0.9)]);

        _llmClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<RetrievedChunk>>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResponse("Physics fee is $50/hr.", [chunkId]));

        var handler = new SendMessageCommandHandler(_db, _ragClient, _llmClient);
        var command = new SendMessageCommand(_businessId, null, "What is the fee and pricing for physics?");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.LeadIntentDetected.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NewConversation_CreatesAndReturnsNewId()
    {
        // Arrange
        _ragClient.QueryAsync(_businessId, Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([new RetrievedChunk(Guid.NewGuid(), "Sample text", 0.8)]);

        _llmClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<RetrievedChunk>>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResponse("Sample answer", [Guid.NewGuid()]));

        var handler = new SendMessageCommandHandler(_db, _ragClient, _llmClient);
        var command = new SendMessageCommand(_businessId, null, "Hello there!", "visitor-123");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.ConversationId.ShouldNotBe(Guid.Empty);
        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == response.ConversationId);
        conversation.ShouldNotBeNull();
        conversation.BusinessId.ShouldBe(_businessId);
        conversation.VisitorRef.ShouldBe("visitor-123");
    }

    [Fact]
    public async Task Handle_ExistingConversation_AppendsMessagesToSameConversation()
    {
        // Arrange
        var conv = Conversation.Start(_businessId, "visitor-999");
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync();

        _ragClient.QueryAsync(_businessId, Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([new RetrievedChunk(Guid.NewGuid(), "Sample FAQ", 0.9)]);

        _llmClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<RetrievedChunk>>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResponse("Answer text", [Guid.NewGuid()]));

        var handler = new SendMessageCommandHandler(_db, _ragClient, _llmClient);
        var command = new SendMessageCommand(_businessId, conv.Id, "Follow-up question");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.ConversationId.ShouldBe(conv.Id);
        var messages = await _db.Messages.Where(m => m.ConversationId == conv.Id).ToListAsync();
        messages.Count.ShouldBe(2);
    }

    public void Dispose() => _db.Dispose();
}
