using FluentValidation;

namespace JobNecto.Application.Educations.Validators;

/// <summary>
/// Reusable FluentValidation helpers for education validators.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Ensures a string value is not whitespace-only.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> NotWhitespaceOnly<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("{PropertyName} must not be whitespace-only.");
    }
}
