using FluentValidation;

namespace JobNecto.Application.Users.Validators;

/// <summary>
/// FluentValidation validator for <see cref="SignInCommand"/>.
/// Enforces non-empty plus a generous max length only — no format or regex rules, since those
/// would leak "that isn't a valid login shape" to an unauthenticated caller. The max length is
/// far above any real credential and exists only to bound PBKDF2 hashing cost per request
/// (Password is verified — including against the not-found dummy hash — on every attempt,
/// before any rate limit applies), not to reject unusual-but-real input.
/// </summary>
public class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    private const int MaxFieldLength = 1000;

    public SignInCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("identifier is required.")
            .MaximumLength(MaxFieldLength).WithMessage($"identifier must be at most {MaxFieldLength} characters long.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("password is required.")
            .MaximumLength(MaxFieldLength).WithMessage($"password must be at most {MaxFieldLength} characters long.");
    }
}
