using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Application.Users.Validators;

namespace JobNecto.Tests.Application.Users;

public class UpdateCurrentUserCommandValidatorTests
{
    private readonly UpdateCurrentUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidPartialPayload_Passes()
    {
        var command = new UpdateCurrentUserCommand
        {
            Email = "new@example.com",
            Phone = "+15555550101",
            Avatar = "https://res.cloudinary.com/demo/image/upload/sample.jpg"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("bad-phone")]
    [InlineData("12345")]
    public void Validate_InvalidPhone_Fails(string phone)
    {
        var command = new UpdateCurrentUserCommand { Phone = phone };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public void Validate_InvalidEmail_Fails()
    {
        var command = new UpdateCurrentUserCommand { Email = "not-an-email" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_InvalidLoginName_Fails()
    {
        var command = new UpdateCurrentUserCommand { LoginName = "bad login" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LoginName");
    }

    [Fact]
    public void Validate_InvalidAvatarReference_Fails()
    {
        var command = new UpdateCurrentUserCommand { Avatar = "http://insecure.example.com/avatar.jpg" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Avatar");
    }
}
