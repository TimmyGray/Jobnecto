using FluentAssertions;
using JobNecto.Application.Resumes;
using JobNecto.Application.Resumes.Validators;

namespace JobNecto.Tests.Application.Resumes;

public class UpdateResumeCommandValidatorTests
{
    private readonly UpdateResumeCommandValidator _validator = new();

    [Fact]
    public void Validate_NoUpdatableFieldsProvided_Fails()
    {
        var command = new UpdateResumeCommand
        {
            UserId = Guid.NewGuid(),
            ResumeId = Guid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptySkillsArray_FailsWithSkillsError()
    {
        var command = new UpdateResumeCommand
        {
            UserId = Guid.NewGuid(),
            ResumeId = Guid.NewGuid(),
            Skills = [],
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Skills");
    }

    [Fact]
    public void Validate_ValidPartialPayload_Passes()
    {
        var command = new UpdateResumeCommand
        {
            UserId = Guid.NewGuid(),
            ResumeId = Guid.NewGuid(),
            Title = "Updated resume title",
            Skills = ["C#", "EF Core"],
            WorkLocationType = "remote",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid-work-location", null, null, "WorkLocationType")]
    [InlineData(null, "invalid-currency", null, "Currency")]
    [InlineData(null, null, "invalid-experience", "Experience")]
    public void Validate_InvalidEnums_Fail(
        string? workLocationType,
        string? currency,
        string? experience,
        string expectedProperty)
    {
        var command = new UpdateResumeCommand
        {
            UserId = Guid.NewGuid(),
            ResumeId = Guid.NewGuid(),
            Title = "Has at least one field",
            WorkLocationType = workLocationType,
            Currency = currency,
            Experience = experience,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == expectedProperty);
    }
}
