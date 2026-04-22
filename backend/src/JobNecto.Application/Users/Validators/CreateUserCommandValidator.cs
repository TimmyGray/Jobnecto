using FluentValidation;
using JobNecto.Domain.Enums;

namespace JobNecto.Application.Users.Validators;

/// <summary>
/// FluentValidation validator for <see cref="CreateUserCommand"/>.
/// Enforces loginName rules, email format, password length, and optional phone/location formats.
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.LoginName)
            .NotEmpty().WithMessage("loginName is required.")
            .Length(3, 50).WithMessage("loginName must be between 3 and 50 characters.")
            .Matches("^[A-Za-z0-9_]+$").WithMessage("loginName must contain only letters, numbers, or underscore.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("email is required.")
            .EmailAddress().WithMessage("email must be a valid email address.")
            .MaximumLength(50).WithMessage("email must be at most 50 characters long.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("password is required.")
            .MinimumLength(8).WithMessage("password must be at least 8 characters long.");

        RuleFor(x => x.Password)
            .MaximumLength(50).WithMessage("password must be at most 50 characters long.");

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () =>
        {
            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("phone must be at most 20 characters long.")
                .Matches(@"^\+[1-9]\d{1,14}$")
                .WithMessage("phone must be a valid E.164 string (e.g. +15555550100).");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Location), () =>
        {
            RuleFor(x => x.Location)
                .MaximumLength(50).WithMessage("location must be at most 50 characters long.")
                .Must(location => Enum.TryParse<Location>(location, true, out _))
                .WithMessage("location must be a valid Location enum value.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.About), () =>
        {
            RuleFor(x => x.About)
                .MaximumLength(5000).WithMessage("about must be at most 5000 characters long.");
        });
    }
}
