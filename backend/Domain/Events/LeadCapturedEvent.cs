using NeverMissLead.Domain.Common;

namespace NeverMissLead.Domain.Events;

/// <summary>Raised when a visitor's intent triggers lead capture.</summary>
public sealed record LeadCapturedEvent(Guid LeadId, Guid BusinessId, DateTime OccurredAt) : IDomainEvent;
