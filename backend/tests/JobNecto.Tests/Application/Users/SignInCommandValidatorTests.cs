using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Application.Users.Validators;

namespace JobNecto.Tests.Application.Users;

public class SignInCommandValidatorTests
{
    private readonly SignInCommandValidator _validator = new();

    [Fact]
    public void ValidModel_PassesValidation()
    {
        var cmd = new SignInCommand { Identifier = "daria@example.com", Password = "anything" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyIdentifier_Fails(string? identifier)
    {
        var cmd = new SignInCommand { Identifier = identifier!, Password = "anything" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Identifier");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyPassword_Fails(string? password)
    {
        var cmd = new SignInCommand { Identifier = "daria_dev", Password = password! };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void NoFormatOrMinimumLengthRulesOnIdentifier_ShortOrUnusualIdentifierPasses()
    {
        // No format/regex/minimum-length rules, so this must never leak "that isn't a valid login shape".
        var cmd = new SignInCommand { Identifier = "x", Password = "y" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IdentifierAtMaxLength_Passes()
    {
        var cmd = new SignInCommand { Identifier = new string('a', 1000), Password = "anything" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IdentifierOverMaxLength_Fails()
    {
        // Bounds PBKDF2/payload cost per request, not "valid shape" — 1000 chars is far above any real identifier.
        var cmd = new SignInCommand { Identifier = new string('a', 1001), Password = "anything" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Identifier");
    }

    [Fact]
    public void PasswordAtMaxLength_Passes()
    {
        var cmd = new SignInCommand { Identifier = "daria_dev", Password = new string('a', 1000) };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PasswordOverMaxLength_Fails()
    {
        // Bounds PBKDF2 hashing cost per request — the not-found path always hashes too.
        var cmd = new SignInCommand { Identifier = "daria_dev", Password = new string('a', 1001) };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
