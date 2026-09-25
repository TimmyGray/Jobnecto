using FluentValidation;

namespace JobNecto.Application.Users.Validators;

/// <summary>
/// FluentValidation validator for <see cref="SignInCommand"/>.
/// Enforces non-empty fields only — no length, format, or regex rules, since those would
/// leak "that isn't a valid login shape" to an unauthenticated caller.
/// </summary>
public class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().WithMessage("identifier is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("password is required.");
    }
}
