using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.Application.KbDocuments.Commands.EmbedDocument;

/// <summary>
/// Creates a <c>kb_documents</c> row and calls the RAG service to chunk + embed the text,
/// writing chunk rows to <c>kb_chunks</c> via the Python microservice.
/// </summary>
/// <param name="BusinessId">The owning business — enforces tenant isolation.</param>
/// <param name="Title">Human-readable document title (e.g. "FAQ – March 2025").</param>
/// <param name="RawText">Plain-text content to be chunked and embedded.</param>
public sealed record EmbedDocumentCommand(
    Guid BusinessId,
    string Title,
    string RawText) : IRequest<EmbedDocumentResult>;

/// <summary>Result returned after successful document embedding.</summary>
/// <param name="DocumentId">PK of the newly created <c>kb_documents</c> row.</param>
/// <param name="ChunkIds">IDs of the <c>kb_chunks</c> rows written by the RAG service.</param>
public sealed record EmbedDocumentResult(Guid DocumentId, IReadOnlyList<Guid> ChunkIds);
