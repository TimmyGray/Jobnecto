using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.CoverLetterTemplates.Validators;

namespace JobNecto.Tests.Application.CoverLetterTemplates;

public class CreateCoverLetterTemplateCommandValidatorTests
{
    private readonly CreateCoverLetterTemplateCommandValidator _validator = new();

    private static string ValidContent() => new string('a', 50);

    [Fact]
    public void Validate_ValidPayload_Passes()
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = "My Template",
            Content = ValidContent(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_NullOrEmptyName_Fails(string? name)
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = name!,
            Content = ValidContent(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WhitespaceOnlyName_Fails()
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = "   ",
            Content = ValidContent(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ContentLessThan50Chars_Fails()
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = "My Template",
            Content = new string('a', 49),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Content");
    }

    [Fact]
    public void Validate_ContentMoreThan10000Chars_Fails()
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = "My Template",
            Content = new string('a', 10001),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Content");
    }

    [Fact]
    public void Validate_NameMoreThan100Chars_Fails()
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = new string('a', 101),
            Content = ValidContent(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_NullOrEmptyContent_Fails(string? content)
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = "My Template",
            Content = content!,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Content");
    }

    [Fact]
    public void Validate_ContentExactly10000Chars_Passes()
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = "My Template",
            Content = new string('a', 10000),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
