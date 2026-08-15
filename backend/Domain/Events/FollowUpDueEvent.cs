using NeverMissLead.Domain.Common;

namespace NeverMissLead.Domain.Events;

/// <summary>Raised by the follow-up poller when a scheduled task is due.</summary>
public sealed record FollowUpDueEvent(Guid FollowUpTaskId, Guid LeadId, DateTime OccurredAt) : IDomainEvent;
