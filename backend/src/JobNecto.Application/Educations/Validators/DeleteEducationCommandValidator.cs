using FluentValidation;

namespace JobNecto.Application.Educations.Validators;

/// <summary>
/// Validator for <see cref="DeleteEducationCommand"/>.
/// </summary>
public class DeleteEducationCommandValidator : AbstractValidator<DeleteEducationCommand>
{
    /// <summary>
    /// Initializes validation rules for education delete requests.
    /// </summary>
    public DeleteEducationCommandValidator()
    {
        RuleFor(x => x.EducationId)
            .NotEmpty().WithMessage("educationId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("userId is required.");
    }
}
