using FluentAssertions;
using JobNecto.Application.Resumes;
using JobNecto.Application.Resumes.Mappers;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;

namespace JobNecto.Tests.Application.Resumes;

public class ResumeMappersTests
{
    [Fact]
    public void ToEntity_NullCommand_Throws()
    {
        CreateResumeCommand command = null!;

        var act = () => command.ToEntity();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToEntity_WithValidEnumStrings_ParsesEnums()
    {
        var command = new CreateResumeCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Senior .NET",
            Skills = ["C#"],
            WorkLocationType = "Remote",
            Salary = 1000m,
            Currency = "USD",
            Experience = "ThreeToFiveYears",
            Locations = [Location.Ukraine],
        };

        var entity = command.ToEntity();

        entity.UserId.Should().Be(command.UserId);
        entity.Title.Should().Be("Senior .NET");
        entity.Currency.Should().Be(Currency.USD);
        entity.WorkLocationType.Should().Be(WorkLocationType.Remote);
        entity.Experience.Should().Be(Experience.ThreeToFiveYears);
    }

    [Fact]
    public void ToEntity_WithInvalidEnumStrings_MapsToNull()
    {
        var command = new CreateResumeCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Dev",
            Skills = ["C#"],
            WorkLocationType = "not-a-value",
            Currency = "XXX",
            Experience = "huge",
        };

        var entity = command.ToEntity();

        entity.Currency.Should().BeNull();
        entity.WorkLocationType.Should().BeNull();
        entity.Experience.Should().BeNull();
    }

    [Fact]
    public void ToEntity_WithBlankCurrencyAndExperience_MapsToNull()
    {
        var command = new CreateResumeCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Dev",
            Skills = ["C#"],
            WorkLocationType = "Remote",
            Currency = "   ",
            Experience = "   ",
        };

        var entity = command.ToEntity();

        entity.Currency.Should().BeNull();
        entity.Experience.Should().BeNull();
    }

    [Fact]
    public void ToResumeResult_NullResume_Throws()
    {
        Resume resume = null!;

        var act = () => resume.ToResumeResult();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToResumeResult_WithNullOptionalFields_UsesEmptyDefaults()
    {
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = null,
            Currency = null,
            Skills = null,
            WorkLocationType = null,
            Experience = null,
        };

        var result = resume.ToResumeResult();

        result.Title.Should().BeEmpty();
        result.Currency.Should().BeEmpty();
        result.Skills.Should().BeEmpty();
        result.WorkLocationType.Should().BeEmpty();
        result.Experience.Should().BeEmpty();
    }

    [Fact]
    public void ToResumeResult_WithValues_MapsAllFields()
    {
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "QA",
            Salary = 500m,
            Currency = Currency.EUR,
            Skills = ["test"],
            WorkLocationType = WorkLocationType.Hybrid,
            Experience = Experience.OneToThreeYears,
        };

        var result = resume.ToResumeResult();

        result.Title.Should().Be("QA");
        result.Currency.Should().Be("EUR");
        result.WorkLocationType.Should().Be("Hybrid");
        result.Experience.Should().Be("OneToThreeYears");
        result.Skills.Should().BeEquivalentTo(["test"]);
    }

    [Fact]
    public void ApplyUpdates_WithAllFieldsSet_UpdatesEntity()
    {
        var resume = new Resume { Title = "old", UserId = Guid.NewGuid() };
        var command = new UpdateResumeCommand
        {
            Title = "new",
            Salary = 42m,
            Currency = "GBP",
            Skills = ["a", "b"],
            WorkLocationType = "OnSite",
            Experience = "MoreThanFiveYears",
            Projects = ["p"],
            Certifications = ["c"],
            Languages = [],
            Locations = [Location.Poland],
            ExcludedWords = ["x"],
        };

        resume.ApplyUpdates(command);

        resume.Title.Should().Be("new");
        resume.Salary.Should().Be(42m);
        resume.Currency.Should().Be(Currency.GBP);
        resume.Skills.Should().BeEquivalentTo(["a", "b"]);
        resume.WorkLocationType.Should().Be(WorkLocationType.OnSite);
        resume.Experience.Should().Be(Experience.MoreThanFiveYears);
        resume.Projects.Should().BeEquivalentTo(["p"]);
        resume.Certifications.Should().BeEquivalentTo(["c"]);
        resume.Locations.Should().BeEquivalentTo([Location.Poland]);
        resume.ExcludedWords.Should().BeEquivalentTo(["x"]);
    }

    [Fact]
    public void ApplyUpdates_WithBlankOrInvalidEnums_MapsToNull()
    {
        var resume = new Resume
        {
            UserId = Guid.NewGuid(),
            Currency = Currency.USD,
            WorkLocationType = WorkLocationType.Remote,
            Experience = Experience.OneToThreeYears,
        };
        var command = new UpdateResumeCommand
        {
            Currency = "   ",
            WorkLocationType = "nonsense",
            Experience = "   ",
        };

        resume.ApplyUpdates(command);

        resume.Currency.Should().BeNull();
        resume.WorkLocationType.Should().BeNull();
        resume.Experience.Should().BeNull();
    }

    [Fact]
    public void ApplyUpdates_WithNoFieldsSet_LeavesEntityUnchanged()
    {
        var resume = new Resume
        {
            Title = "keep",
            Salary = 10m,
            Currency = Currency.USD,
            Skills = ["s"],
            WorkLocationType = WorkLocationType.Remote,
            Experience = Experience.LessThanOneYear,
            UserId = Guid.NewGuid(),
        };

        resume.ApplyUpdates(new UpdateResumeCommand());

        resume.Title.Should().Be("keep");
        resume.Salary.Should().Be(10m);
        resume.Currency.Should().Be(Currency.USD);
        resume.Skills.Should().BeEquivalentTo(["s"]);
        resume.WorkLocationType.Should().Be(WorkLocationType.Remote);
        resume.Experience.Should().Be(Experience.LessThanOneYear);
    }
}
