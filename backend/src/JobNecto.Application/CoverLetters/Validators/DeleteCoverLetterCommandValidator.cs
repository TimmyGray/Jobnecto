using FluentValidation;

namespace JobNecto.Application.CoverLetters.Validators;

/// <summary>
/// Validator for <see cref="DeleteCoverLetterCommand"/>.
/// </summary>
public class DeleteCoverLetterCommandValidator : AbstractValidator<DeleteCoverLetterCommand>
{
    /// <summary>
    /// Initializes validation rules for cover letter delete requests.
    /// </summary>
    public DeleteCoverLetterCommandValidator()
    {
        RuleFor(x => x.CoverLetterId)
            .NotEmpty()
            .WithMessage("coverLetterId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("userId is required.");
    }
}