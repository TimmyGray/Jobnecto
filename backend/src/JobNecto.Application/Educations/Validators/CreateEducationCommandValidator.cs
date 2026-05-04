using FluentValidation;
using JobNecto.Domain.Enums;

namespace JobNecto.Application.Educations.Validators;

/// <summary>
/// Validator for <see cref="CreateEducationCommand"/>.
/// </summary>
public class CreateEducationCommandValidator : AbstractValidator<CreateEducationCommand>
{
    private bool IsExistingEnumValue<TEnum>(string value) where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, true, out _);
    }

    /// <summary>
    /// Initializes validation rules for education creation requests.
    /// </summary>
    public CreateEducationCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("title is required.")
            .NotWhitespaceOnly()
            .MaximumLength(100).WithMessage("title must be at most 100 characters long.");

        RuleFor(x => x.Specialization)
            .NotEmpty().WithMessage("specialization is required.")
            .NotWhitespaceOnly()
            .MaximumLength(100).WithMessage("specialization must be at most 100 characters long.");

        RuleFor(x => x.Degree)
            .NotEmpty().WithMessage("degree is required.")
            .Must(degree => IsExistingEnumValue<Degree>(degree))
            .WithMessage("degree must be one of: bachelor, master, phd, postdoc, other.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}