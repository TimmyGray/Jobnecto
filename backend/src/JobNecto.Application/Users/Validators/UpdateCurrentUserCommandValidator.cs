using FluentValidation;
using JobNecto.Domain.Enums;
using System.Text.RegularExpressions;

namespace JobNecto.Application.Users.Validators;

/// <summary>
/// FluentValidation rules for updating current-user profile fields.
/// </summary>
public class UpdateCurrentUserCommandValidator : AbstractValidator<UpdateCurrentUserCommand>
{
    private static readonly Regex StorageKeyRegex = new(
        "^[A-Za-z0-9_./-]+$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public UpdateCurrentUserCommandValidator()
    {
        RuleFor(x => x.LoginName)
            .NotEmpty().When(x => x.LoginName != null)
            .Length(3, 50).When(x => x.LoginName != null)
            .Matches("^[A-Za-z0-9_]+$").When(x => x.LoginName != null)
            .WithMessage("loginName must contain only letters, numbers, or underscore.");

        RuleFor(x => x.Email)
            .NotEmpty().When(x => x.Email != null)
            .EmailAddress().When(x => x.Email != null)
            .MaximumLength(50).When(x => x.Email != null);

        RuleFor(x => x.Phone)
            .MaximumLength(20).When(x => x.Phone != null)
            .Matches(@"^\+[1-9]\d{1,14}$").When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("phone must be a valid E.164 string (e.g. +15555550100).");

        RuleFor(x => x.Location)
            .MaximumLength(50).When(x => x.Location != null)
            .Must(location => string.IsNullOrWhiteSpace(location) || Enum.TryParse<Location>(location, true, out _))
            .When(x => x.Location != null)
            .WithMessage("location must be a valid Location enum value.");

        RuleFor(x => x.About)
            .MaximumLength(5000).When(x => x.About != null);

        RuleFor(x => x.Avatar)
            .MaximumLength(2048).When(x => x.Avatar != null)
            .Must(IsValidAvatarReference)
            .When(x => !string.IsNullOrWhiteSpace(x.Avatar))
            .WithMessage("avatar must be a valid https URL or storage key.");
    }

    private static bool IsValidAvatarReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme == Uri.UriSchemeHttps;
        }

        return StorageKeyRegex.IsMatch(value);
    }
}
