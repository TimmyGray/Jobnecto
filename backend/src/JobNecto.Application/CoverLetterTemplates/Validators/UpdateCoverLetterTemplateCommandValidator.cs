using FluentValidation;

namespace JobNecto.Application.CoverLetterTemplates.Validators;

/// <summary>
/// Validator for <see cref="UpdateCoverLetterTemplateCommand"/>.
/// </summary>
public class UpdateCoverLetterTemplateCommandValidator : AbstractValidator<UpdateCoverLetterTemplateCommand>
{
    /// <summary>
    /// Initializes validation rules for cover letter template update requests.
    /// </summary>
    public UpdateCoverLetterTemplateCommandValidator()
    {
        RuleFor(x => x.CoverLetterTemplateId)
            .NotEmpty().WithMessage("coverLetterTemplateId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("userId is required.");

        RuleFor(x => x)
            .Custom((command, context) =>
            {
                if (command.Name == null && command.Content == null)
                {
                    context.AddFailure(nameof(UpdateCoverLetterTemplateCommand.Name), "At least one updatable field must be provided.");
                }
            });

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("name must not be empty when provided.")
            .MaximumLength(100).WithMessage("name must be at most 100 characters long.")
            .When(x => x.Name != null);

        RuleFor(x => x.Content)
            .MinimumLength(50).WithMessage("content must be at least 50 characters long.")
            .MaximumLength(10000).WithMessage("content must be at most 10000 characters long.")
            .When(x => x.Content != null);
    }
}