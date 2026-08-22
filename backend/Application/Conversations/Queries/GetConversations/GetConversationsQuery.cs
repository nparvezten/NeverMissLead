using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Application.Conversations.Queries.GetConversations;

public sealed record ConversationSummaryDto(
    Guid Id,
    string VisitorRef,
    DateTime StartedAt,
    string Status,
    bool NeedsHuman,
    string HandoffReason,
    int MessageCount,
    string? LastMessage,
    DateTime? LastMessageAt);

public sealed record GetConversationsQuery(Guid BusinessId) : IRequest<IReadOnlyList<ConversationSummaryDto>>;

public sealed class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetConversationsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken = default)
    {
        var conversations = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.BusinessId == request.BusinessId)
            .Include(c => c.Messages)
            .OrderByDescending(c => c.StartedAt)
            .ToListAsync(cancellationToken);

        var list = conversations.Select(c =>
        {
            var lastMsg = c.Messages.OrderBy(m => m.CreatedAt).LastOrDefault();
            return new ConversationSummaryDto(
                c.Id,
                c.VisitorRef,
                c.StartedAt,
                c.Status.ToString(),
                c.NeedsHuman,
                c.HandoffReason.ToString(),
                c.Messages.Count,
                lastMsg?.Content,
                lastMsg?.CreatedAt);
        }).ToList();

        return list.AsReadOnly();
    }
}
