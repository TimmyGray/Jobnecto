using FluentValidation;
using JobNecto.Domain.Enums;

namespace JobNecto.Application.Resumes.Validators;

/// <summary>
/// Validator for the <see cref="CreateResumeCommand"/>.
/// Ensures all mandatory fields are present and valid, and optional fields match enum definitions.
/// </summary>
public class CreateResumeCommandValidator : AbstractValidator<CreateResumeCommand>
{
    private bool IsExisingEnumValue<TEnum>(string value) where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, true, out _);
    }
    public CreateResumeCommandValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Title), () =>
        {
            RuleFor(x => x.Title)
                .MaximumLength(200).WithMessage("title must be at most 200 characters long.");
        });

        When(x => x.Skills != null && x.Skills.Length > 0, () =>
        {
            RuleFor(x => x.Skills)
                .Must(s => s.All(skill => skill.Length <= 30)).WithMessage("each skill must be at most 30 characters long.");
        });

        When(x => !string.IsNullOrEmpty(x.WorkLocationType), () =>
        {
            RuleFor(x => x.WorkLocationType)
                .Must(type => IsExisingEnumValue<WorkLocationType>(type))
                .WithMessage("workLocationType must be a valid WorkLocationType enum value (remote, office, hybrid).");
        });

        When(x => !string.IsNullOrEmpty(x.Currency), () =>
        {
            RuleFor(x => x.Currency!)
                .Must(currency => IsExisingEnumValue<Currency>(currency))
                .WithMessage("currency must be a valid Currency enum value (e.g. USD, EUR, UAH).");
        });

        When(x => !string.IsNullOrEmpty(x.Experience), () =>
        {
            RuleFor(x => x.Experience!)
                .Must(experience => IsExisingEnumValue<Experience>(experience))
                .WithMessage("experience must be a valid Experience enum value (e.g. LessThanOneYear, OneToThreeYears, ThreeToFiveYears, MoreThanFiveYears).");
        });

        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).When(x => x.Salary.HasValue)
            .WithMessage("salary cannot be negative.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
