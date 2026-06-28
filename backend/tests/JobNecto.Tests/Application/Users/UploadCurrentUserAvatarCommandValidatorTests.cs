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

    public static IEnumerable<object[]> ValidSignatures()
    {
        yield return ["image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }];
        yield return ["image/jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xDB }];
        yield return ["image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }];
        yield return ["image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 }];
        yield return ["image/gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }];
        yield return ["image/gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }];
    }

    [Theory]
    [MemberData(nameof(ValidSignatures))]
    public void Validate_MatchingSignatureForEachType_Passes(string contentType, byte[] content)
    {
        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = "avatar",
            ContentType = contentType,
            Content = content
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyFileName_Fails()
    {
        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = "",
            ContentType = "image/png",
            Content = PngBytes
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public void Validate_FileNameTooLong_Fails()
    {
        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = new string('a', 256),
            ContentType = "image/png",
            Content = PngBytes
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public void Validate_ContentLargerThanFiveMegabytes_Fails()
    {
        var oversized = new byte[5 * 1024 * 1024 + 1];
        // Keep a valid PNG signature so only the size rule fails.
        Array.Copy(PngBytes, oversized, PngBytes.Length);

        var command = new UploadCurrentUserAvatarCommand
        {
            FileName = "avatar.png",
            ContentType = "image/png",
            Content = oversized
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("5 MB"));
    }
}
