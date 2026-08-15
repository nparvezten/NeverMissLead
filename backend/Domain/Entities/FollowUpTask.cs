using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Domain.Entities;

/// <summary>
/// A scheduled follow-up action for a captured lead.
/// Polled by <c>FollowUpHostedService</c> every N minutes.
/// </summary>
public class FollowUpTask
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public DateTime ScheduledFor { get; private set; }
    public FollowUpChannel Channel { get; private set; }
    public string MessageDraft { get; private set; } = string.Empty;
    public FollowUpStatus Status { get; private set; }
    public DateTime? SentAt { get; private set; }

    // Navigation
    public Lead Lead { get; private set; } = null!;

    private FollowUpTask() { }

    /// <summary>Schedules a follow-up for a lead.</summary>
    public static FollowUpTask Schedule(
        Guid leadId,
        DateTime scheduledFor,
        string messageDraft,
        FollowUpChannel channel = FollowUpChannel.Email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageDraft);

        return new FollowUpTask
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            ScheduledFor = scheduledFor,
            Channel = channel,
            MessageDraft = messageDraft,
            Status = FollowUpStatus.Pending
        };
    }

    /// <summary>Marks the follow-up as sent.</summary>
    public void MarkSent()
    {
        Status = FollowUpStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    /// <summary>Skips this follow-up (e.g., lead already responded).</summary>
    public void Skip() => Status = FollowUpStatus.Skipped;
}
