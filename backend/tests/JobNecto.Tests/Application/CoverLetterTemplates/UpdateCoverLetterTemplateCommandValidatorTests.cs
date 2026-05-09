using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.CoverLetterTemplates.Validators;

namespace JobNecto.Tests.Application.CoverLetterTemplates;

public class UpdateCoverLetterTemplateCommandValidatorTests
{
    private readonly UpdateCoverLetterTemplateCommandValidator _validator = new();

    private static string ValidContent() => new string('a', 50);

    [Fact]
    public void Validate_NoUpdatableFieldsProvided_Fails()
    {
        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidNameOnly_Passes()
    {
        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Updated Name",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidContentOnly_Passes()
    {
        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = ValidContent(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void Validate_WhitespaceOrEmptyName_Fails(string name)
    {
        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateCoverLetterTemplateCommand.Name));
    }

    [Fact]
    public void Validate_TooLongName_Fails()
    {
        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = new string('x', 101),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateCoverLetterTemplateCommand.Name));
    }

    [Theory]
    [InlineData(49)]
    [InlineData(10001)]
    public void Validate_ContentOutOfBounds_Fails(int length)
    {
        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = new string('a', length),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateCoverLetterTemplateCommand.Content));
    }
}