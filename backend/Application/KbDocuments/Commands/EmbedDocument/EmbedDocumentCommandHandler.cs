using FluentValidation;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Application.KbDocuments.Commands.EmbedDocument;


/// <summary>
/// Handles <see cref="EmbedDocumentCommand"/>.
/// Flow:
/// 1. Validate input.
/// 2. Persist a <c>kb_documents</c> row via EF Core.
/// 3. Call <c>IRagClient.EmbedAsync</c> → Python RAG service chunks + embeds the text
///    and writes the resulting <c>kb_chunks</c> rows directly to Postgres.
/// 4. Return the document ID and chunk IDs.
/// </summary>
public sealed class EmbedDocumentCommandHandler
    : IRequestHandler<EmbedDocumentCommand, EmbedDocumentResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IRagClient _ragClient;
    private readonly IValidator<EmbedDocumentCommand> _validator;

    public EmbedDocumentCommandHandler(
        IApplicationDbContext db,
        IRagClient ragClient,
        IValidator<EmbedDocumentCommand> validator)
    {
        _db = db;
        _ragClient = ragClient;
        _validator = validator;
    }

    /// <inheritdoc />
    public async Task<EmbedDocumentResult> Handle(
        EmbedDocumentCommand request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // 1. Persist the document row
        var document = KbDocument.Create(
            request.BusinessId,
            request.Title,
            request.RawText,
            SourceType.PlainText);

        _db.KbDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        // 2. Ask the RAG service to chunk + embed the text and write to kb_chunks
        var chunkIds = await _ragClient.EmbedAsync(
            request.BusinessId,
            document.Id,
            request.RawText,
            cancellationToken);

        return new EmbedDocumentResult(document.Id, chunkIds);
    }
}
