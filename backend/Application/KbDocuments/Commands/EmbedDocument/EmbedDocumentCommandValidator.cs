using FluentValidation;

namespace NeverMissLead.Application.KbDocuments.Commands.EmbedDocument;

/// <summary>FluentValidation validator for <see cref="EmbedDocumentCommand"/>.</summary>
public sealed class EmbedDocumentCommandValidator : AbstractValidator<EmbedDocumentCommand>
{
    public EmbedDocumentCommandValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("BusinessId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.RawText)
            .NotEmpty().WithMessage("Document text is required.")
            .MinimumLength(10).WithMessage("Document text must be at least 10 characters.")
            .MaximumLength(1_000_000).WithMessage("Document text must not exceed 1 MB.");
    }
}
