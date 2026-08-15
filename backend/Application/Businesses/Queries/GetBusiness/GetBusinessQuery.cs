using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.Application.Businesses.Queries.GetBusiness;

/// <summary>Fetches a business by its ID.</summary>
public sealed record GetBusinessQuery(Guid BusinessId) : IRequest<BusinessDto?>;

/// <summary>Read-model DTO for a business — no PII, safe to return in API responses.</summary>
public sealed record BusinessDto(
    Guid Id,
    string Name,
    string Niche,
    string Timezone,
    DateTime CreatedAt,
    string? WidgetGreeting,
    string? BrandColor);
