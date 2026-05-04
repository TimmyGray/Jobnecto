using FluentAssertions;
using JobNecto.Application.Educations;
using JobNecto.Application.Educations.Validators;

namespace JobNecto.Tests.Application.Educations;

public class UpdateEducationCommandValidatorTests
{
    private readonly UpdateEducationCommandValidator _validator = new();

    [Fact]
    public void Validate_NoUpdatableFieldsProvided_Fails()
    {
        var command = new UpdateEducationCommand
        {
            EducationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TitleOnly_Passes()
    {
        var command = new UpdateEducationCommand
        {
            EducationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Updated title",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_SpecializationOnly_Passes()
    {
        var command = new UpdateEducationCommand
        {
            EducationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Specialization = "Data Science",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DegreeOnly_ValidValue_Passes()
    {
        var command = new UpdateEducationCommand
        {
            EducationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Degree = "master",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidDegree_FailsWithDegreeError()
    {
        var command = new UpdateEducationCommand
        {
            EducationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Degree = "invalid_degree",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEducationCommand.Degree));
    }

    [Fact]
    public void Validate_TitleExceeding100Chars_FailsWithTitleError()
    {
        var command = new UpdateEducationCommand
        {
            EducationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = new string('x', 101),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEducationCommand.Title));
    }

    [Fact]
    public void Validate_EmptyTitle_FailsWithTitleError()
    {
        var command = new UpdateEducationCommand
        {
            EducationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEducationCommand.Title));
    }

    [Fact]
    public void Validate_AllValidFields_Passes()
    {
        var command = new UpdateEducationCommand
        {
            EducationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "MSc Computer Science",
            Specialization = "Artificial Intelligence",
            Degree = "phd",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
