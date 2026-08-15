namespace NeverMissLead.Domain.Entities;

/// <summary>
/// A single text chunk from a <see cref="KbDocument"/>, with its embedding stored
/// as a <c>vector(1536)</c> column in Postgres (via pgvector). The embedding column
/// is managed via raw SQL / Dapper — EF Core sees it as a <c>byte[]</c> placeholder
/// but never queries it through LINQ. Cosine-distance retrieval is done in the
/// Python RAG service.
/// </summary>
public class KbChunk
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string ChunkText { get; private set; } = string.Empty;
    public int TokenCount { get; private set; }

    // Navigation
    public KbDocument Document { get; private set; } = null!;

    private KbChunk() { }

    /// <summary>Creates a new chunk for a document. Embedding is set later by the RAG service.</summary>
    public static KbChunk Create(Guid documentId, string chunkText, int tokenCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chunkText);

        return new KbChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkText = chunkText,
            TokenCount = tokenCount
        };
    }
}
