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
            .Custom((command, context) =>
            {
                if (!HasAtLeastOneUpdatableField(command))
                {
                    context.AddFailure(nameof(UpdateResumeCommand.Title), "At least one updatable field must be provided.");
                }
            });

        RuleFor(x => x.ResumeId)
            .NotEmpty().WithMessage("resumeId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("userId is required.");

        RuleFor(x => x.Title)
            .MaximumLength(500)
            .When(x => x.Title != null)
            .WithMessage("title must be at most 500 characters long.");

        When(x => x.Skills != null && x.Skills.Length > 0, () =>
        {
            RuleForEach(x => x.Skills)
                .NotEmpty().WithMessage("skills cannot contain empty values.")
                .MaximumLength(30).WithMessage("each skill must be at most 30 characters long.");
        });

        // Null leaves the field unchanged and an empty (or whitespace-only) string clears it,
        // matching ResumeMappers.ApplyUpdates; only a non-blank value must name an enum member.
        RuleFor(x => x.WorkLocationType)
            .Must(IsBlankOrEnumValue<WorkLocationType>)
            .WithMessage("workLocationType must be a valid WorkLocationType enum value (OnSite, Remote, Hybrid).");

        RuleFor(x => x.Currency)
            .Must(IsBlankOrEnumValue<Currency>)
            .WithMessage("currency must be a valid Currency enum value (e.g. USD, EUR, UAH).");

        RuleFor(x => x.Experience)
            .Must(IsBlankOrEnumValue<Experience>)
            .WithMessage("experience must be a valid Experience enum value.");

        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Salary.HasValue)
            .WithMessage("salary cannot be negative.");

        RuleFor(x => x.Salary)
            .Null()
            .When(x => x.ClearSalary)
            .WithMessage("salary cannot be provided together with clearSalary.");
    }

    private static bool IsBlankOrEnumValue<TEnum>(string? value) where TEnum : struct, Enum
    {
        return string.IsNullOrWhiteSpace(value) || Enum.TryParse<TEnum>(value, true, out _);
    }

    private static bool HasAtLeastOneUpdatableField(UpdateResumeCommand command)
    {
        return command.Title != null
               || command.Salary.HasValue
               || command.ClearSalary
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
