using FluentValidation;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;

namespace NeverMissLead.Application.Businesses.Commands.CreateBusiness;

/// <summary>
/// Handles <see cref="CreateBusinessCommand"/>.
/// Validates the request, creates the <see cref="Business"/> aggregate and its
/// initial <see cref="BusinessSettings"/>, and persists them in a single transaction.
/// </summary>
public sealed class CreateBusinessCommandHandler
    : IRequestHandler<CreateBusinessCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IValidator<CreateBusinessCommand> _validator;

    public CreateBusinessCommandHandler(
        IApplicationDbContext db,
        IValidator<CreateBusinessCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    /// <inheritdoc />
    public async Task<Guid> Handle(
        CreateBusinessCommand request,
        CancellationToken cancellationToken = default)
    {
        var result = await _validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var business = Business.Create(request.Name, request.Niche, request.Timezone);

        var settings = BusinessSettings.Create(
            business.Id,
            request.HandoffEmail,
            request.WidgetGreeting ?? "Hi! How can I help you today?",
            request.BrandColor ?? "#6366f1");

        _db.Businesses.Add(business);
        _db.BusinessSettings.Add(settings);

        await _db.SaveChangesAsync(cancellationToken);

        return business.Id;
    }
}
