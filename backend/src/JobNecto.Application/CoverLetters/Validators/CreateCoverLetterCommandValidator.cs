using FluentValidation;

namespace JobNecto.Application.CoverLetters.Validators;

/// <summary>
/// Validator for <see cref="CreateCoverLetterCommand"/>.
/// </summary>
public class CreateCoverLetterCommandValidator : AbstractValidator<CreateCoverLetterCommand>
{
    /// <summary>
    /// Initializes validation rules for cover letter creation requests.
    /// </summary>
    public CreateCoverLetterCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.VacancyId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MinimumLength(50).MaximumLength(10000);
    }
}