namespace NeverMissLead.Application.Interfaces;

/// <summary>
/// Contract for the Python RAG service client.
/// Kept behind an interface so the HTTP implementation can be swapped with
/// a fake in unit tests and a stub during Week 2 wiring.
/// </summary>
public interface IRagClient
{
    /// <summary>
    /// Sends a document's raw text to the RAG service to be chunked, embedded,
    /// and written to <c>kb_chunks</c>. Returns the IDs of the created chunks.
    /// </summary>
    Task<IReadOnlyList<Guid>> EmbedAsync(
        Guid businessId,
        Guid documentId,
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the top-k most relevant KB chunks for the given question,
    /// scoped strictly to the specified business.
    /// </summary>
    Task<IReadOnlyList<RetrievedChunk>> QueryAsync(
        Guid businessId,
        string question,
        int topK = 4,
        CancellationToken cancellationToken = default);
}

/// <summary>A single chunk returned by the RAG retrieval service.</summary>
/// <param name="ChunkId">PK of the <c>kb_chunks</c> row.</param>
/// <param name="ChunkText">Raw text content of the chunk.</param>
/// <param name="Score">Cosine similarity score (higher = more relevant).</param>
public sealed record RetrievedChunk(Guid ChunkId, string ChunkText, double Score);

