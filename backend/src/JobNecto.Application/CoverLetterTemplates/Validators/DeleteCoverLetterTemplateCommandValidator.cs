using FluentValidation;

namespace JobNecto.Application.CoverLetterTemplates.Validators;

/// <summary>
/// Validator for <see cref="DeleteCoverLetterTemplateCommand"/>.
/// </summary>
public class DeleteCoverLetterTemplateCommandValidator : AbstractValidator<DeleteCoverLetterTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCoverLetterTemplateCommandValidator"/> class.
    /// </summary>
    public DeleteCoverLetterTemplateCommandValidator()
    {
        RuleFor(x => x.CoverLetterTemplateId)
            .NotEmpty().WithMessage("coverLetterTemplateId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("userId is required.");
    }
}
