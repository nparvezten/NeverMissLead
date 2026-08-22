using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Application.Leads.Queries.GetLeads;

public sealed class GetLeadsQueryHandler : IRequestHandler<GetLeadsQuery, IReadOnlyList<LeadDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEncryptionService _encryptionService;

    public GetLeadsQueryHandler(
        IApplicationDbContext db,
        IEncryptionService encryptionService)
    {
        _db = db;
        _encryptionService = encryptionService;
    }

    public async Task<IReadOnlyList<LeadDto>> Handle(GetLeadsQuery request, CancellationToken cancellationToken = default)
    {
        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.BusinessId == request.BusinessId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtoList = new List<LeadDto>(leads.Count);

        foreach (var lead in leads)
        {
            var name = _encryptionService.Decrypt(lead.NameEnc);
            var phone = _encryptionService.Decrypt(lead.PhoneEnc);
            var email = _encryptionService.Decrypt(lead.EmailEnc);

            dtoList.Add(new LeadDto(
                lead.Id,
                lead.ConversationId,
                lead.BusinessId,
                name,
                phone,
                email,
                lead.IntentSummary,
                lead.QualificationScore,
                lead.Status.ToString(),
                lead.CreatedAt));
        }

        return dtoList.AsReadOnly();
    }
}
