using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Domain.Entities;

/// <summary>
/// A knowledge document uploaded by the business owner (FAQ, pricing sheet, etc.).
/// Raw text is stored here; chunked embeddings live in <see cref="KbChunk"/>.
/// </summary>
public class KbDocument
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public SourceType SourceType { get; private set; }
    public string RawText { get; private set; } = string.Empty;
    public DateTime UploadedAt { get; private set; }

    // Navigation
    public Business Business { get; private set; } = null!;
    public IReadOnlyCollection<KbChunk> Chunks => _chunks.AsReadOnly();

    private readonly List<KbChunk> _chunks = [];

    private KbDocument() { }

    /// <summary>Creates a new knowledge document for a business.</summary>
    public static KbDocument Create(
        Guid businessId,
        string title,
        string rawText,
        SourceType sourceType = SourceType.PlainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawText);

        return new KbDocument
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Title = title.Trim(),
            RawText = rawText,
            SourceType = sourceType,
            UploadedAt = DateTime.UtcNow
        };
    }
}
