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
    public void Validate_EmptySkillsArray_PassesAsClear()
    {
        var command = new UpdateResumeCommand
        {
            UserId = Guid.NewGuid(),
            ResumeId = Guid.NewGuid(),
            Skills = [],
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceEnums_PassAsClear(string value)
    {
        foreach (var command in new[]
                 {
                     NewCommand(c => c.WorkLocationType = value),
                     NewCommand(c => c.Currency = value),
                     NewCommand(c => c.Experience = value),
                 })
        {
            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }

    [Fact]
    public void Validate_ClearSalaryAlone_PassesAndCountsAsUpdatableField()
    {
        var command = NewCommand(c => c.ClearSalary = true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ClearSalaryWithZeroSalary_FailsOnSalary()
    {
        // Zero proves the rule tests presence, not truthiness.
        var command = NewCommand(c =>
        {
            c.ClearSalary = true;
            c.Salary = 0m;
        });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Salary" && e.ErrorMessage == "salary cannot be provided together with clearSalary.");
    }

    [Fact]
    public void Validate_ClearSalaryFalseWithoutOtherFields_StillFailsAsEmptyUpdate()
    {
        var command = NewCommand(c => c.ClearSalary = false);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "At least one updatable field must be provided.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("0123456789012345678901234567890")]
    public void Validate_InvalidSkillItem_StillFails(string skill)
    {
        var command = NewCommand(c => c.Skills = ["C#", skill]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Skills[1]");
    }

    [Fact]
    public void Validate_InvalidWorkLocationType_ListsRealEnumValues()
    {
        var command = NewCommand(c => c.WorkLocationType = "office");

        var result = _validator.Validate(command);

        result.Errors.Should().ContainSingle(e => e.PropertyName == "WorkLocationType")
            .Which.ErrorMessage.Should().Contain("(OnSite, Remote, Hybrid)");
    }

    private static UpdateResumeCommand NewCommand(Action<UpdateResumeCommand> configure)
    {
        var command = new UpdateResumeCommand
        {
            UserId = Guid.NewGuid(),
            ResumeId = Guid.NewGuid(),
        };
        configure(command);
        return command;
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
