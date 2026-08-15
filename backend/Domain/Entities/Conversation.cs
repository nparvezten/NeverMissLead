using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Domain.Entities;

/// <summary>
/// A single visitor chat session with a business's widget.
/// When <see cref="NeedsHuman"/> is true the owner receives an email notification
/// and the conversation appears in the "unanswered questions" dashboard view.
/// </summary>
public class Conversation
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }

    /// <summary>
    /// Anonymous reference token for the visitor — never a real name or contact.
    /// Used only to correlate messages in the same session.
    /// </summary>
    public string VisitorRef { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public ConversationStatus Status { get; private set; }
    public bool NeedsHuman { get; private set; }
    public HandoffReason HandoffReason { get; private set; }

    // Navigation
    public Business Business { get; private set; } = null!;
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private readonly List<Message> _messages = [];

    private Conversation() { }

    /// <summary>Starts a new conversation for a visitor on a business widget.</summary>
    public static Conversation Start(Guid businessId, string visitorRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visitorRef);

        return new Conversation
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            VisitorRef = visitorRef,
            StartedAt = DateTime.UtcNow,
            Status = ConversationStatus.Active,
            NeedsHuman = false,
            HandoffReason = HandoffReason.None
        };
    }

    /// <summary>Flags this conversation for human handoff.</summary>
    public void RequestHandoff(HandoffReason reason)
    {
        NeedsHuman = true;
        HandoffReason = reason;
        Status = ConversationStatus.HandedOff;
    }

    /// <summary>Closes the conversation.</summary>
    public void Close() => Status = ConversationStatus.Closed;
}
