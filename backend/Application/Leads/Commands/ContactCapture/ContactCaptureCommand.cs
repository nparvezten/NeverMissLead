using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.Application.Leads.Commands.ContactCapture;

/// <summary>
/// Command to capture lead contact details from the chat widget.
/// </summary>
public sealed record ContactCaptureCommand(
    Guid BusinessId,
    Guid ConversationId,
    string Name,
    string? Phone,
    string? Email,
    string? IntentSummary = null) : IRequest<Guid>;
