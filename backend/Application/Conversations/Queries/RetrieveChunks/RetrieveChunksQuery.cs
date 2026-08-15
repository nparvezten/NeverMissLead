using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Application.Conversations.Queries.RetrieveChunks;

/// <summary>
/// Retrieves the top-k most relevant KB chunks for a visitor question,
/// scoped strictly to the specified business (cross-tenant safety is enforced
/// at the RAG service level and also verified by business_id in the DB query).
/// </summary>
public sealed record RetrieveChunksQuery(
    Guid BusinessId,
    string Question,
    int TopK = 4) : IRequest<IReadOnlyList<RetrievedChunk>>;
