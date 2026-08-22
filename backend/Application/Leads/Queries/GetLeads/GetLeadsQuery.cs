using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.Application.Leads.Queries.GetLeads;

public sealed record GetLeadsQuery(Guid BusinessId) : IRequest<IReadOnlyList<LeadDto>>;
