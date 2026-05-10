using FluentAssertions;
using JobNecto.Application.CoverLetters;
using JobNecto.Application.CoverLetters.Validators;

namespace JobNecto.Tests.Application.CoverLetters;

public class DeleteCoverLetterCommandValidatorTests
{
    private readonly DeleteCoverLetterCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidPayload_Passes()
    {
        var command = new DeleteCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCoverLetterId_Fails()
    {
        var command = new DeleteCoverLetterCommand
        {
            CoverLetterId = Guid.Empty,
            UserId = Guid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "CoverLetterId");
    }

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        var command = new DeleteCoverLetterCommand
        {
            CoverLetterId = Guid.NewGuid(),
            UserId = Guid.Empty,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "UserId");
    }
}