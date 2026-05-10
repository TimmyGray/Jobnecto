using FluentAssertions;
using JobNecto.Application.CoverLetters;
using JobNecto.Application.CoverLetters.Validators;

namespace JobNecto.Tests.Application.CoverLetters;

public class UpdateCoverLetterCommandValidatorTests
{
    private readonly UpdateCoverLetterCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidPayload_Passes()
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = new string('a', 50),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCoverLetterId_Fails()
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.Empty,
            UserId = Guid.NewGuid(),
            Content = new string('a', 50),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "CoverLetterId");
    }

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.Empty,
            Content = new string('a', 50),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "UserId");
    }

    [Fact]
    public void Validate_ContentLessThan50Chars_Fails()
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = new string('a', 49),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Content");
    }

    [Fact]
    public void Validate_ContentExactly50Chars_Passes()
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = new string('a', 50),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ContentExactly10000Chars_Passes()
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = new string('a', 10000),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ContentMoreThan10000Chars_Fails()
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = new string('a', 10001),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Content");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_NullOrEmptyContent_Fails(string? content)
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = content!,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Content");
    }

    [Fact]
    public void Validate_WhitespaceOnlyContent_Fails()
    {
        var command = new UpdateCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = "   ",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Content");
    }
}