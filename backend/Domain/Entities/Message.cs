using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Domain.Entities;

/// <summary>
/// A single message within a <see cref="Conversation"/>.
/// <see cref="CitedChunkIds"/> records which <see cref="KbChunk"/> rows were used
/// to generate the answer — an empty list is a signal to flag <c>NeedsHuman</c>.
/// Raw PII is never stored here; <see cref="Content"/> contains the visible text only.
/// </summary>
public class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public IReadOnlyCollection<Guid> CitedChunkIds => _citedChunkIds.AsReadOnly();
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Conversation Conversation { get; private set; } = null!;

    private readonly List<Guid> _citedChunkIds = [];

    private Message() { }

    /// <summary>Creates a new message and records which KB chunks supported the answer.</summary>
    public static Message Create(
        Guid conversationId,
        MessageRole role,
        string content,
        IEnumerable<Guid>? citedChunkIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var msg = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        if (citedChunkIds is not null)
            msg._citedChunkIds.AddRange(citedChunkIds);

        return msg;
    }
}
