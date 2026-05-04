using FluentValidation;
using JobNecto.Domain.Enums;

namespace JobNecto.Application.Educations.Validators;

/// <summary>
/// Validator for <see cref="UpdateEducationCommand"/>.
/// </summary>
public class UpdateEducationCommandValidator : AbstractValidator<UpdateEducationCommand>
{
    /// <summary>
    /// Initializes validation rules for education update requests.
    /// </summary>
    public UpdateEducationCommandValidator()
    {
        RuleFor(x => x.EducationId)
            .NotEmpty().WithMessage("educationId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("userId is required.");

        RuleFor(x => x)
            .Custom((command, context) =>
            {
                if (command.Title == null && command.Specialization == null && command.Degree == null)
                    context.AddFailure(nameof(UpdateEducationCommand.Title),
                        "At least one updatable field must be provided.");
            });

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("title must not be empty when provided.")
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("title must not be whitespace-only.")
            .MaximumLength(100).WithMessage("title must be at most 100 characters long.")
            .When(x => x.Title != null);

        RuleFor(x => x.Specialization)
            .NotEmpty().WithMessage("specialization must not be empty when provided.")
            .Must(spec => !string.IsNullOrWhiteSpace(spec)).WithMessage("specialization must not be whitespace-only.")
            .MaximumLength(100).WithMessage("specialization must be at most 100 characters long.")
            .When(x => x.Specialization != null);

        RuleFor(x => x.Degree)
            .Must(value => value == null || Enum.TryParse<Degree>(value, true, out _))
            .WithMessage("degree must be one of: bachelor, master, phd, postdoc, other.");
    }
}
