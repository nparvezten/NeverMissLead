using FluentValidation;

namespace NeverMissLead.Application.Leads.Commands.ContactCapture;

public sealed class ContactCaptureCommandValidator : AbstractValidator<ContactCaptureCommand>
{
    public ContactCaptureCommandValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("BusinessId is required.");

        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("ConversationId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("At least one contact method (email or phone) must be provided.");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email address format.")
                .MaximumLength(320).WithMessage("Email cannot exceed 320 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () =>
        {
            RuleFor(x => x.Phone)
                .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");
        });
    }
}
