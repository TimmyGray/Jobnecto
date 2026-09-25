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
    public void NoLengthOrFormatRulesOnIdentifier_ShortOrUnusualIdentifierPasses()
    {
        // The validator enforces non-empty only, so it must never leak "that isn't a valid login shape".
        var cmd = new SignInCommand { Identifier = "x", Password = "y" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}
