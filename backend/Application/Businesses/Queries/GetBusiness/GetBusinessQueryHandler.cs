using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Application.Businesses.Queries.GetBusiness;

/// <summary>
/// Handles <see cref="GetBusinessQuery"/>.
/// Returns <see langword="null"/> if the business is not found (controller maps to 404).
/// </summary>
public sealed class GetBusinessQueryHandler
    : IRequestHandler<GetBusinessQuery, BusinessDto?>
{
    private readonly IApplicationDbContext _db;

    public GetBusinessQueryHandler(IApplicationDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<BusinessDto?> Handle(
        GetBusinessQuery request,
        CancellationToken cancellationToken = default)
    {
        var business = await _db.Businesses
            .Include(b => b.Settings)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BusinessId, cancellationToken);

        if (business is null)
            return null;

        return new BusinessDto(
            business.Id,
            business.Name,
            business.Niche,
            business.Timezone,
            business.CreatedAt,
            business.Settings?.WidgetGreeting,
            business.Settings?.BrandColor);
    }
}
