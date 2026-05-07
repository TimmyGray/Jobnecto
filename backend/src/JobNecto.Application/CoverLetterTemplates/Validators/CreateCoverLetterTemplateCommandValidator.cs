using FluentValidation;

namespace JobNecto.Application.CoverLetterTemplates.Validators;

/// <summary>
/// Validator for <see cref="CreateCoverLetterTemplateCommand"/>.
/// </summary>
public class CreateCoverLetterTemplateCommandValidator : AbstractValidator<CreateCoverLetterTemplateCommand>
{
    /// <summary>
    /// Initializes validation rules for cover letter template creation requests.
    /// </summary>
    public CreateCoverLetterTemplateCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Content).NotEmpty().MinimumLength(50).MaximumLength(10000);
    }
}
