using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;

namespace NeverMissLead.Application.Leads.Commands.ContactCapture;

public sealed class ContactCaptureCommandHandler : IRequestHandler<ContactCaptureCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;

    public ContactCaptureCommandHandler(
        IApplicationDbContext dbContext,
        IEncryptionService encryptionService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
    }

    public async Task<Guid> Handle(ContactCaptureCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify conversation exists for this business
        var conversationExists = await _dbContext.Conversations
            .AnyAsync(c => c.Id == request.ConversationId && c.BusinessId == request.BusinessId, cancellationToken);

        if (!conversationExists)
        {
            throw new KeyNotFoundException(
                $"Conversation '{request.ConversationId}' was not found for business '{request.BusinessId}'.");
        }

        // 2. Encrypt sensitive PII fields before database insertion
        var nameEnc = _encryptionService.Encrypt(request.Name);
        var phoneEnc = _encryptionService.Encrypt(request.Phone);
        var emailEnc = _encryptionService.Encrypt(request.Email);

        // 3. Calculate qualification score
        bool hasPhone = !string.IsNullOrWhiteSpace(request.Phone);
        bool hasEmail = !string.IsNullOrWhiteSpace(request.Email);

        int qualificationScore = (hasPhone, hasEmail) switch
        {
            (true, true) => 100,
            (false, true) => 60,
            (true, false) => 40,
            _ => 0
        };

        // 4. Create Lead entity
        var lead = Lead.Capture(
            request.ConversationId,
            request.BusinessId,
            nameEnc,
            phoneEnc,
            emailEnc,
            request.IntentSummary,
            qualificationScore);

        _dbContext.Leads.Add(lead);

        // 5. Follow-Up Trigger Rule 1: Schedule 1-hour follow-up nudge for lead
        var followUpTask = FollowUpTask.ScheduleForLead(
            lead.Id,
            lead.BusinessId,
            lead.CreatedAt.AddHours(1),
            $"Automated lead follow-up for inquiry on conversation '{request.ConversationId}'",
            Domain.Enums.FollowUpChannel.Email,
            request.ConversationId);

        _dbContext.FollowUpTasks.Add(followUpTask);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return lead.Id;
    }
}
