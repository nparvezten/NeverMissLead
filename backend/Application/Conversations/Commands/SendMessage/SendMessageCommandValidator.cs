using FluentValidation;

namespace NeverMissLead.Application.Conversations.Commands.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("BusinessId is required.");

        RuleFor(x => x.VisitorMessage)
            .NotEmpty().WithMessage("Visitor message cannot be empty.")
            .MaximumLength(2000).WithMessage("Visitor message cannot exceed 2000 characters.");
    }
}
