using FluentValidation;

namespace JobNecto.Application.CoverLetters.Validators;

/// <summary>
/// Validator for <see cref="UpdateCoverLetterCommand"/>.
/// </summary>
public class UpdateCoverLetterCommandValidator : AbstractValidator<UpdateCoverLetterCommand>
{
    /// <summary>
    /// Initializes validation rules for cover letter update requests.
    /// </summary>
    public UpdateCoverLetterCommandValidator()
    {
        RuleFor(x => x.CoverLetterId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .MinimumLength(50)
            .MaximumLength(10000);
    }
}