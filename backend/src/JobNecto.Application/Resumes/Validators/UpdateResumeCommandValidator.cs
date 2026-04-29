using FluentValidation;
using JobNecto.Domain.Enums;

namespace JobNecto.Application.Resumes.Validators;

/// <summary>
/// Validator for <see cref="UpdateResumeCommand"/>.
/// </summary>
public class UpdateResumeCommandValidator : AbstractValidator<UpdateResumeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateResumeCommandValidator"/> class.
    /// </summary>
    public UpdateResumeCommandValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneUpdatableField)
            .WithMessage("At least one updatable field must be provided.");

        RuleFor(x => x.ResumeId)
            .NotEmpty().WithMessage("resumeId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("userId is required.");

        RuleFor(x => x.Title)
            .MaximumLength(500)
            .When(x => x.Title != null)
            .WithMessage("title must be at most 500 characters long.");

        RuleFor(x => x.Skills)
            .Must(skills => skills == null || skills.Length > 0)
            .WithMessage("skills must contain at least one value when provided.");

        When(x => x.Skills != null && x.Skills.Length > 0, () =>
        {
            RuleForEach(x => x.Skills)
                .NotEmpty().WithMessage("skills cannot contain empty values.")
                .MaximumLength(30).WithMessage("each skill must be at most 30 characters long.");
        });

        RuleFor(x => x.WorkLocationType)
            .Must(value => value == null || Enum.TryParse<WorkLocationType>(value, true, out _))
            .WithMessage("workLocationType must be a valid WorkLocationType enum value (remote, office, hybrid).");

        RuleFor(x => x.Currency)
            .Must(value => value == null || Enum.TryParse<Currency>(value, true, out _))
            .WithMessage("currency must be a valid Currency enum value (e.g. USD, EUR, UAH).");

        RuleFor(x => x.Experience)
            .Must(value => value == null || Enum.TryParse<Experience>(value, true, out _))
            .WithMessage("experience must be a valid Experience enum value.");

        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Salary.HasValue)
            .WithMessage("salary cannot be negative.");
    }

    private static bool HasAtLeastOneUpdatableField(UpdateResumeCommand command)
    {
        return command.Title != null
               || command.Salary.HasValue
               || command.Currency != null
               || command.Skills != null
               || command.WorkLocationType != null
               || command.Experience != null
               || command.Projects != null
               || command.Certifications != null
               || command.Languages != null
               || command.Locations != null
               || command.ExcludedWords != null;
    }
}
