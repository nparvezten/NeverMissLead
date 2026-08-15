using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Application.Conversations.Queries.RetrieveChunks;

/// <summary>
/// Handles <see cref="RetrieveChunksQuery"/> by delegating to the RAG service
/// via <see cref="IRagClient"/>. The client is swappable/mockable in tests.
/// </summary>
public sealed class RetrieveChunksQueryHandler
    : IRequestHandler<RetrieveChunksQuery, IReadOnlyList<RetrievedChunk>>
{
    private readonly IRagClient _ragClient;

    public RetrieveChunksQueryHandler(IRagClient ragClient) => _ragClient = ragClient;

    /// <inheritdoc />
    public Task<IReadOnlyList<RetrievedChunk>> Handle(
        RetrieveChunksQuery request,
        CancellationToken cancellationToken = default)
        => _ragClient.QueryAsync(request.BusinessId, request.Question, request.TopK, cancellationToken);
}
