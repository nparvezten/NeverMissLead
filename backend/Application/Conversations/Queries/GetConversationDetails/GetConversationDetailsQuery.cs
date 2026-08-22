using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Application.Conversations.Queries.GetConversationDetails;

public sealed record MessageDto(
    Guid Id,
    string Role,
    string Content,
    IReadOnlyList<Guid> CitedChunkIds,
    DateTime CreatedAt);

public sealed record ConversationDetailsDto(
    Guid Id,
    Guid BusinessId,
    string VisitorRef,
    DateTime StartedAt,
    string Status,
    bool NeedsHuman,
    string HandoffReason,
    IReadOnlyList<MessageDto> Messages);

public sealed record GetConversationDetailsQuery(
    Guid ConversationId,
    Guid BusinessId) : IRequest<ConversationDetailsDto?>;

public sealed class GetConversationDetailsQueryHandler : IRequestHandler<GetConversationDetailsQuery, ConversationDetailsDto?>
{
    private readonly IApplicationDbContext _db;

    public GetConversationDetailsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ConversationDetailsDto?> Handle(GetConversationDetailsQuery request, CancellationToken cancellationToken = default)
    {
        var conversation = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == request.ConversationId && c.BusinessId == request.BusinessId)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
            return null;

        var messages = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                m.Id,
                m.Role.ToString().ToLowerInvariant(),
                m.Content,
                m.CitedChunkIds.ToList().AsReadOnly(),
                m.CreatedAt))
            .ToList();

        return new ConversationDetailsDto(
            conversation.Id,
            conversation.BusinessId,
            conversation.VisitorRef,
            conversation.StartedAt,
            conversation.Status.ToString(),
            conversation.NeedsHuman,
            conversation.HandoffReason.ToString(),
            messages.AsReadOnly());
    }
}
