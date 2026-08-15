using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.KbDocuments.Commands.EmbedDocument;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Infrastructure.Persistence;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.KbDocuments;

/// <summary>
/// Unit tests for <see cref="EmbedDocumentCommandHandler"/>.
/// Uses an in-memory DB and NSubstitute fake for <see cref="IRagClient"/> —
/// no real embedding API is ever called.
/// </summary>
public sealed class EmbedDocumentCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IRagClient _ragClient;
    private readonly EmbedDocumentCommandHandler _handler;

    public EmbedDocumentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _ragClient = Substitute.For<IRagClient>();
        _handler = new EmbedDocumentCommandHandler(
            _db,
            _ragClient,
            new EmbedDocumentCommandValidator());
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsDocumentAndCallsRagService()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var fakeChunkIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _ragClient
            .EmbedAsync(businessId, Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fakeChunkIds);

        var command = new EmbedDocumentCommand(
            BusinessId: businessId,
            Title: "Bright Minds FAQ",
            RawText: "We offer math tuition for grades 6–12. Our fees are ₹3,000 per month.");

        // Act
        var result = await _handler.Handle(command);

        // Assert — document persisted
        result.DocumentId.ShouldNotBe(Guid.Empty);
        var doc = await _db.KbDocuments.FindAsync(result.DocumentId);
        doc.ShouldNotBeNull();
        doc!.Title.ShouldBe("Bright Minds FAQ");
        doc.BusinessId.ShouldBe(businessId);

        // Assert — RAG client called exactly once with correct IDs
        await _ragClient.Received(1).EmbedAsync(
            businessId,
            result.DocumentId,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Assert — chunk IDs forwarded from RAG service
        result.ChunkIds.ShouldBe(fakeChunkIds);
    }

    [Fact]
    public async Task Handle_EmptyTitle_ThrowsValidationException()
    {
        var command = new EmbedDocumentCommand(Guid.NewGuid(), "", "Some text here and more.");
        await Should.ThrowAsync<FluentValidation.ValidationException>(() => _handler.Handle(command));
    }

    [Fact]
    public async Task Handle_EmptyText_ThrowsValidationException()
    {
        var command = new EmbedDocumentCommand(Guid.NewGuid(), "My FAQ", "");
        await Should.ThrowAsync<FluentValidation.ValidationException>(() => _handler.Handle(command));
    }

    [Fact]
    public async Task Handle_EmptyBusinessId_ThrowsValidationException()
    {
        var command = new EmbedDocumentCommand(Guid.Empty, "My FAQ", "Some text here and more.");
        await Should.ThrowAsync<FluentValidation.ValidationException>(() => _handler.Handle(command));
    }

    public void Dispose() => _db.Dispose();
}
