using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Application.Conversations.Commands.SendMessage;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRagClient _ragClient;
    private readonly ILlmClient _llmClient;

    private const string AbstainMessage = "I'm not sure about that — let me connect you with the owner.";

    private static readonly string[] LeadIntentKeywords =
    [
        "price", "pricing", "cost", "fee", "fees",
        "enroll", "enrollment", "register", "registration", "join", "sign up",
        "sign-up", "demo", "trial", "free trial", "batch", "schedule", "timing",
        "available", "availability", "seat", "seats", "admission"
    ];

    public SendMessageCommandHandler(
        IApplicationDbContext dbContext,
        IRagClient ragClient,
        ILlmClient llmClient)
    {
        _dbContext = dbContext;
        _ragClient = ragClient;
        _llmClient = llmClient;
    }

    public async Task<SendMessageResponse> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify business exists
        var businessExists = await _dbContext.Businesses
            .AnyAsync(b => b.Id == request.BusinessId, cancellationToken);

        if (!businessExists)
        {
            throw new KeyNotFoundException($"Business '{request.BusinessId}' was not found.");
        }

        // 2. Load or create conversation (strictly scoped to BusinessId)
        Conversation conversation;
        if (request.ConversationId.HasValue)
        {
            conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.BusinessId == request.BusinessId, cancellationToken)
                ?? throw new KeyNotFoundException($"Conversation '{request.ConversationId.Value}' was not found for business '{request.BusinessId}'.");
        }
        else
        {
            var visitorRef = string.IsNullOrWhiteSpace(request.VisitorRef)
                ? Guid.NewGuid().ToString("N")[..8]
                : request.VisitorRef.Trim();

            conversation = Conversation.Start(request.BusinessId, visitorRef);
            _dbContext.Conversations.Add(conversation);
        }

        // 3. Insert user message
        var userMsg = Message.Create(conversation.Id, MessageRole.User, request.VisitorMessage);
        _dbContext.Messages.Add(userMsg);

        // 4. Retrieve candidate chunks from RAG service
        var candidateChunks = await _ragClient.QueryAsync(request.BusinessId, request.VisitorMessage, topK: 4, cancellationToken);
        var chunks = candidateChunks?.Where(c => c.Score >= 0.25).ToList() ?? [];

        string assistantMessageText;
        IReadOnlyList<Guid> citedChunkIds = [];
        bool needsHuman = false;

        if (chunks.Count == 0)
        {
            conversation.RequestHandoff(HandoffReason.NoCitedChunks);
            needsHuman = true;
            assistantMessageText = AbstainMessage;
        }
        else
        {
            // 5. Generate response with LLM
            var llmResponse = await _llmClient.GenerateAsync(
                "You are a helpful assistant for a service business.",
                request.VisitorMessage,
                chunks,
                cancellationToken);

            if (llmResponse == null || llmResponse.CitedChunkIds.Count == 0)
            {
                conversation.RequestHandoff(HandoffReason.NoCitedChunks);
                needsHuman = true;
                assistantMessageText = AbstainMessage;
            }
            else
            {
                assistantMessageText = llmResponse.AnswerText;
                citedChunkIds = llmResponse.CitedChunkIds;
            }
        }

        // 6. Insert assistant message
        var assistantMsg = Message.Create(conversation.Id, MessageRole.Assistant, assistantMessageText, citedChunkIds);
        _dbContext.Messages.Add(assistantMsg);

        // 7. Lead intent classification
        bool leadIntentDetected = DetectLeadIntent(request.VisitorMessage);

        // 8. Follow-Up Trigger Rule 2: Escalation to owner after 4 hours if conversation needs human
        if (needsHuman)
        {
            var escalationTask = FollowUpTask.ScheduleForHandoff(
                conversation.Id,
                request.BusinessId,
                DateTime.UtcNow.AddHours(4),
                $"Owner Escalation: Conversation '{conversation.Id}' requires human attention. Unanswered question: '{request.VisitorMessage}'",
                FollowUpChannel.Email);

            _dbContext.FollowUpTasks.Add(escalationTask);
        }

        // 9. Save all changes
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SendMessageResponse(
            conversation.Id,
            assistantMessageText,
            needsHuman,
            leadIntentDetected,
            citedChunkIds);
    }

    private static bool DetectLeadIntent(string message) =>
        LeadIntentKeywords.Any(kw => message.Contains(kw, StringComparison.OrdinalIgnoreCase));
}
