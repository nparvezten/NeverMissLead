using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Application.Leads.Commands.UpdateLeadStatus;

public sealed record UpdateLeadStatusCommand(
    Guid LeadId,
    Guid BusinessId,
    LeadStatus NewStatus) : IRequest<bool>;

public sealed class UpdateLeadStatusCommandHandler : IRequestHandler<UpdateLeadStatusCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public UpdateLeadStatusCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(UpdateLeadStatusCommand request, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == request.LeadId && l.BusinessId == request.BusinessId, cancellationToken);

        if (lead is null)
        {
            throw new KeyNotFoundException($"Lead '{request.LeadId}' not found for business '{request.BusinessId}'.");
        }

        lead.UpdateStatus(request.NewStatus);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
