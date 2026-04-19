using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Application.Users.Validators;
using Xunit;

namespace JobNecto.Tests.Application.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void ValidModel_PassesValidation()
    {
        var cmd = new CreateUserCommand
        {
            LoginName = "valid_user",
            Email = "timmy@example.com",
            Password = "strongpassword",
            Phone = null,
            Location = null,
            About = "About me",
            Avatar = null
        };

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("this_login_name_is_way_too_long_for_the_limit")]
    [InlineData("bad-char!")]
    public void InvalidLoginName_Fails(string login)
    {
        var cmd = new CreateUserCommand { LoginName = login, Email = "a@b.com", Password = "strongpassword" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LoginName");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("noatsign.com")]
    public void InvalidEmail_Fails(string email)
    {
        var cmd = new CreateUserCommand { LoginName = "user123", Email = email, Password = "strongpassword" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void ShortPassword_Fails()
    {
        var cmd = new CreateUserCommand { LoginName = "user123", Email = "a@b.com", Password = "short" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("+1")]
    [InlineData("++1234567890")]
    public void InvalidPhone_Fails(string phone)
    {
        var cmd = new CreateUserCommand { LoginName = "user123", Email = "a@b.com", Password = "strongpassword", Phone = phone };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public void EmptyPhone_Passes()
    {
        var cmd = new CreateUserCommand { LoginName = "user1", Email = "a@b.com", Password = "strongpassword", Phone = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
