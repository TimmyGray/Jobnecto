using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Application.Users.Validators;

namespace JobNecto.Tests.Application.Users;

public class UploadCurrentUserAvatarCommandValidatorTests
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47,
        0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D
    ];

    private readonly UploadCurrentUserAvatarCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidPayload_Passes()
    {
        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = "avatar.png",
            ContentType = "image/png",
            Content = PngBytes
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidContentType_Fails()
    {
        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = "avatar.pdf",
            ContentType = "application/pdf",
            Content = [1, 2, 3]
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }

    [Fact]
    public void Validate_EmptyContent_Fails()
    {
        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = "avatar.png",
            ContentType = "image/png",
            Content = []
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [Fact]
    public void Validate_MismatchedSignature_Fails()
    {
        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = "avatar.png",
            ContentType = "image/png",
            Content = [0xFF, 0xD8, 0xFF, 0xE0]
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("signature", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NullContentType_FailsWithoutThrowing()
    {
        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = "avatar.png",
            ContentType = null!,
            Content = PngBytes
        };

        var result = default(FluentValidation.Results.ValidationResult);
        var act = () => result = _validator.Validate(command);

        act.Should().NotThrow();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }
}
