using FluentValidation;

namespace NeverMissLead.Application.Businesses.Commands.CreateBusiness;

/// <summary>FluentValidation rules for <see cref="CreateBusinessCommand"/>.</summary>
public sealed class CreateBusinessCommandValidator : AbstractValidator<CreateBusinessCommand>
{
    public CreateBusinessCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Niche)
            .NotEmpty().WithMessage("Niche is required.")
            .MaximumLength(100);

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.HandoffEmail)
            .NotEmpty().WithMessage("Handoff email is required.")
            .EmailAddress().WithMessage("Handoff email must be a valid email address.")
            .MaximumLength(320);

        RuleFor(x => x.BrandColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.BrandColor))
            .WithMessage("Brand color must be a valid hex color (e.g. #6366f1).");
    }
}
