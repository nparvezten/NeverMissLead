using NeverMissLead.Domain.Common;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Domain.Events;

/// <summary>Raised when the RAG pipeline cannot answer a question and escalates to the business owner.</summary>
public sealed record HandoffRequestedEvent(
    Guid ConversationId,
    Guid BusinessId,
    HandoffReason Reason,
    DateTime OccurredAt) : IDomainEvent;
