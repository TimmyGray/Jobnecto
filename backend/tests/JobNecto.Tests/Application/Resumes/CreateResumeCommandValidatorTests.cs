using FluentAssertions;
using JobNecto.Application.Resumes;
using JobNecto.Application.Resumes.Validators;

namespace JobNecto.Tests.Application.Resumes;

public class CreateResumeCommandValidatorTests
{
    private readonly CreateResumeCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var cmd = new CreateResumeCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Senior .NET Developer",
            Skills = new[] { "C#", ".NET 10", "EF Core" },
            WorkLocationType = "remote",
            Salary = 100000,
            Currency = "USD",
            Experience = "senior"
        };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidTitle_Passes_If_Logic_Allows_Optional(string? title)
    {
        var cmd = new CreateResumeCommand 
        { 
            UserId = Guid.NewGuid(),
            Title = title!, 
            Skills = new[] { "C#" }, 
            WorkLocationType = "remote" 
        };
        
        var result = _validator.Validate(cmd);
        
        // Casing to follow current logic: Title is optional in validator due to When(!string.IsNullOrEmpty)
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptySkills_Passes_If_Logic_Allows_Optional()
    {
        var cmd = new CreateResumeCommand 
        { 
            UserId = Guid.NewGuid(),
            Title = "Title", 
            Skills = Array.Empty<string>(), 
            WorkLocationType = "remote" 
        };
        
        var result = _validator.Validate(cmd);
        
        // Skills are optional in validator due to When(x.Skills != null && x.Skills.Length > 0)
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("Remote")] // Capitalized passes because IsExisingEnumValue uses ignoreCase: true
    public void WorkLocationType_Validation_Behavior(string type)
    {
        var cmd = new CreateResumeCommand 
        { 
            UserId = Guid.NewGuid(),
            Title = "Title", 
            Skills = new[] { "C#" }, 
            WorkLocationType = type 
        };
        
        var result = _validator.Validate(cmd);
        
        if (type == "invalid")
        {
            result.IsValid.Should().BeFalse();
        }
        else
        {
            // "Remote" or "" passes due to current logic
            result.IsValid.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("")]
    public void WorkLocationType_Empty_Passes(string type)
    {
        var cmd = new CreateResumeCommand 
        { 
            UserId = Guid.NewGuid(),
            Title = "Title", 
            Skills = new[] { "C#" }, 
            WorkLocationType = type 
        };
        
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidCurrency_Fails()
    {
        var cmd = new CreateResumeCommand 
        { 
            UserId = Guid.NewGuid(),
            Title = "Title", 
            Skills = new[] { "C#" }, 
            WorkLocationType = "remote",
            Currency = "INVALID"
        };
        
        var result = _validator.Validate(cmd);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void NegativeSalary_Fails()
    {
        var cmd = new CreateResumeCommand 
        { 
            UserId = Guid.NewGuid(),
            Title = "Title", 
            Skills = new[] { "C#" }, 
            WorkLocationType = "remote",
            Salary = -1
        };
        
        var result = _validator.Validate(cmd);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Salary");
    }

    [Fact]
    public void MissingUserId_Fails()
    {
        var cmd = new CreateResumeCommand 
        { 
            Title = "Title", 
            Skills = new[] { "C#" }, 
            WorkLocationType = "remote"
        };
        
        var result = _validator.Validate(cmd);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
