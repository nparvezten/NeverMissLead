using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Application.Conversations.Queries.GetUnansweredQuestions;

public sealed record UnansweredQuestionDto(
    Guid ConversationId,
    string VisitorRef,
    string Question,
    string HandoffReason,
    DateTime AskedAt);

public sealed record GetUnansweredQuestionsQuery(Guid BusinessId) : IRequest<IReadOnlyList<UnansweredQuestionDto>>;

public sealed class GetUnansweredQuestionsQueryHandler : IRequestHandler<GetUnansweredQuestionsQuery, IReadOnlyList<UnansweredQuestionDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUnansweredQuestionsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UnansweredQuestionDto>> Handle(GetUnansweredQuestionsQuery request, CancellationToken cancellationToken = default)
    {
        var conversations = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.BusinessId == request.BusinessId && c.NeedsHuman)
            .Include(c => c.Messages)
            .OrderByDescending(c => c.StartedAt)
            .ToListAsync(cancellationToken);

        var list = new List<UnansweredQuestionDto>();

        foreach (var conv in conversations)
        {
            // Find the user question that triggered the handoff / abstention
            var userMsg = conv.Messages
                .Where(m => m.Role == MessageRole.User)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            var questionText = userMsg?.Content ?? "(No user message recorded)";
            var askedAt = userMsg?.CreatedAt ?? conv.StartedAt;

            list.Add(new UnansweredQuestionDto(
                conv.Id,
                conv.VisitorRef,
                questionText,
                conv.HandoffReason.ToString(),
                askedAt));
        }

        return list.AsReadOnly();
    }
}
