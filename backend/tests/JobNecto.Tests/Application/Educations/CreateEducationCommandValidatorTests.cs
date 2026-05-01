using FluentAssertions;
using JobNecto.Application.Educations;
using JobNecto.Application.Educations.Validators;

namespace JobNecto.Tests.Application.Educations;

public class CreateEducationCommandValidatorTests
{
    private readonly CreateEducationCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidPayload_Passes()
    {
        var command = new CreateEducationCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Bachelor of Science",
            Specialization = "Computer Science",
            Degree = "bachelor",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingOrEmptyTitle_Fails(string? title)
    {
        var command = new CreateEducationCommand
        {
            UserId = Guid.NewGuid(),
            Title = title!,
            Specialization = "Computer Science",
            Degree = "master",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Title");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingOrEmptySpecialization_Fails(string? specialization)
    {
        var command = new CreateEducationCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Master of Science",
            Specialization = specialization!,
            Degree = "master",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Specialization");
    }

    [Theory]
    [InlineData("postdoc")]
    [InlineData("other")]
    [InlineData("invalid")]
    public void Validate_InvalidDegree_Fails(string degree)
    {
        var command = new CreateEducationCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Master of Science",
            Specialization = "Computer Science",
            Degree = degree,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Degree");
    }
}