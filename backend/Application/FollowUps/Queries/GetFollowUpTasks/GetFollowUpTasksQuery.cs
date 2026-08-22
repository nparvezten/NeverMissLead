using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Application.FollowUps.Queries.GetFollowUpTasks;

public sealed record FollowUpTaskDto(
    Guid Id,
    Guid BusinessId,
    Guid? LeadId,
    Guid? ConversationId,
    DateTime ScheduledFor,
    string Channel,
    string MessageDraft,
    string Status,
    DateTime? SentAt);

public sealed record GetFollowUpTasksQuery(Guid BusinessId) : IRequest<IReadOnlyList<FollowUpTaskDto>>;

public sealed class GetFollowUpTasksQueryHandler : IRequestHandler<GetFollowUpTasksQuery, IReadOnlyList<FollowUpTaskDto>>
{
    private readonly IApplicationDbContext _db;

    public GetFollowUpTasksQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FollowUpTaskDto>> Handle(GetFollowUpTasksQuery request, CancellationToken cancellationToken = default)
    {
        var tasks = await _db.FollowUpTasks
            .AsNoTracking()
            .Where(t => t.BusinessId == request.BusinessId)
            .OrderByDescending(t => t.ScheduledFor)
            .ToListAsync(cancellationToken);

        var list = tasks.Select(t => new FollowUpTaskDto(
            t.Id,
            t.BusinessId,
            t.LeadId,
            t.ConversationId,
            t.ScheduledFor,
            t.Channel.ToString(),
            t.MessageDraft,
            t.Status.ToString(),
            t.SentAt)).ToList();

        return list.AsReadOnly();
    }
}
