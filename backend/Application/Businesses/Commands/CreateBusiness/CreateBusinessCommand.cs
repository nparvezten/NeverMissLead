using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.Application.Businesses.Commands.CreateBusiness;

/// <summary>Creates a new business and returns its new ID.</summary>
public sealed record CreateBusinessCommand(
    string Name,
    string Niche,
    string Timezone,
    string HandoffEmail,
    string? WidgetGreeting,
    string? BrandColor) : IRequest<Guid>;
