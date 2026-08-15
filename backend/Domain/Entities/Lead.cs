using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Domain.Entities;

/// <summary>
/// A qualified lead captured when a visitor shows buying intent.
/// PII fields (<see cref="NameEnc"/>, <see cref="PhoneEnc"/>, <see cref="EmailEnc"/>)
/// are AES-256 encrypted at rest; decryption happens in the Infrastructure layer.
/// </summary>
public class Lead
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid BusinessId { get; private set; }

    /// <summary>AES-256 encrypted visitor name.</summary>
    public string? NameEnc { get; private set; }

    /// <summary>AES-256 encrypted visitor phone number.</summary>
    public string? PhoneEnc { get; private set; }

    /// <summary>AES-256 encrypted visitor email address.</summary>
    public string? EmailEnc { get; private set; }

    /// <summary>Short plain-text summary of purchase intent (not PII).</summary>
    public string? IntentSummary { get; private set; }
    public int QualificationScore { get; private set; }
    public LeadStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Conversation Conversation { get; private set; } = null!;
    public IReadOnlyCollection<FollowUpTask> FollowUpTasks => _followUpTasks.AsReadOnly();

    private readonly List<FollowUpTask> _followUpTasks = [];

    private Lead() { }

    /// <summary>
    /// Captures a new lead. Callers must pass pre-encrypted values for PII fields.
    /// </summary>
    public static Lead Capture(
        Guid conversationId,
        Guid businessId,
        string? nameEnc = null,
        string? phoneEnc = null,
        string? emailEnc = null,
        string? intentSummary = null,
        int qualificationScore = 0)
    {
        return new Lead
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            BusinessId = businessId,
            NameEnc = nameEnc,
            PhoneEnc = phoneEnc,
            EmailEnc = emailEnc,
            IntentSummary = intentSummary,
            QualificationScore = qualificationScore,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Advances the lead status through its lifecycle.</summary>
    public void UpdateStatus(LeadStatus newStatus) => Status = newStatus;
}
