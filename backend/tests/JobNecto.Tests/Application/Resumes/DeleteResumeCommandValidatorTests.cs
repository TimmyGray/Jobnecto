using FluentAssertions;
using JobNecto.Application.Resumes;
using JobNecto.Application.Resumes.Validators;

namespace JobNecto.Tests.Application.Resumes;

public class DeleteResumeCommandValidatorTests
{
    private readonly DeleteResumeCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyResumeId_Fails()
    {
        var command = new DeleteResumeCommand
        {
            ResumeId = Guid.Empty,
            UserId = Guid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResumeId");
    }

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        var command = new DeleteResumeCommand
        {
            ResumeId = Guid.NewGuid(),
            UserId = Guid.Empty,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = new DeleteResumeCommand
        {
            ResumeId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
