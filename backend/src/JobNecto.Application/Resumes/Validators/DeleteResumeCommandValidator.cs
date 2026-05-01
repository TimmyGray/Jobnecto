using FluentValidation;

namespace JobNecto.Application.Resumes.Validators;

/// <summary>
/// Validator for <see cref="DeleteResumeCommand"/>.
/// </summary>
public class DeleteResumeCommandValidator : AbstractValidator<DeleteResumeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteResumeCommandValidator"/> class.
    /// </summary>
    public DeleteResumeCommandValidator()
    {
        RuleFor(x => x.ResumeId)
            .NotEmpty().WithMessage("resumeId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("userId is required.");
    }
}
