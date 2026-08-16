using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.Application.Conversations.Commands.SendMessage;

/// <summary>
/// Command to process a visitor's message in the chat widget.
/// </summary>
public sealed record SendMessageCommand(
    Guid BusinessId,
    Guid? ConversationId,
    string VisitorMessage,
    string? VisitorRef = null) : IRequest<SendMessageResponse>;

/// <summary>
/// Response returned to the chat widget after processing the message.
/// </summary>
public sealed record SendMessageResponse(
    Guid ConversationId,
    string AssistantMessage,
    bool NeedsHuman,
    bool LeadIntentDetected,
    IReadOnlyList<Guid> CitedChunkIds);
