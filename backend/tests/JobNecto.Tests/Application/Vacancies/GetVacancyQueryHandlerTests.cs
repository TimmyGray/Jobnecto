using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Vacancies;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;
using Moq;

namespace JobNecto.Tests.Application.Vacancies;

public class GetVacancyQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IVacancyRepository> _vacancyRepositoryMock;
    private readonly GetVacancyQueryHandler _handler;

    public GetVacancyQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _vacancyRepositoryMock = new Mock<IVacancyRepository>();
        _uowMock.Setup(x => x.VacancyRepository).Returns(_vacancyRepositoryMock.Object);
        _handler = new GetVacancyQueryHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_VacancyOwnedByUser_ReturnsMappedDetailResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

        var vacancy = new Vacancy
        {
            Id = vacancyId,
            UserId = userId,
            Title = "Senior Backend Engineer (.NET / Azure)",
            Description = "Design APIs and event-driven services.",
            Company = "MedStack Analytics",
            Skills = [".NET", "C#", "Azure"],
            WorkLocationType = WorkLocationType.Remote,
            Location = Location.Poland,
            SalaryMin = 95_000m,
            SalaryMax = 118_000m,
            Currency = Currency.EUR,
            MatchScore = 0.91,
            JobSource = new JobSource { Name = "LinkedIn", Url = "https://linkedin.com/jobs" },
            JobCategories = ["Engineering", "Backend"],
            ExperienceLevel = "Senior",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

        _vacancyRepositoryMock
            .Setup(x => x.GetByIdAsync(vacancyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vacancy);

        // Act
        var result = await _handler.Handle(
            new GetVacancyQuery { VacancyId = vacancyId, UserId = userId },
            CancellationToken.None);

        // Assert
        result.Id.Should().Be(vacancyId);
        result.Title.Should().Be(vacancy.Title);
        result.Description.Should().Be(vacancy.Description);
        result.Company.Should().Be(vacancy.Company);
        result.Skills.Should().BeEquivalentTo(vacancy.Skills);
        result.WorkLocationType.Should().Be(WorkLocationType.Remote.ToString());
        result.Location.Should().Be(Location.Poland.ToString());
        result.Salary.Should().NotBeNull();
        result.Salary!.Min.Should().Be(vacancy.SalaryMin);
        result.Salary.Max.Should().Be(vacancy.SalaryMax);
        result.Currency.Should().Be(Currency.EUR.ToString());
        result.MatchScore.Should().Be(vacancy.MatchScore);
        result.JobSource.Should().NotBeNull();
        result.JobSource!.Name.Should().Be("LinkedIn");
        result.JobSource.Url.Should().Be("https://linkedin.com/jobs");
        result.Categories.Should().BeEquivalentTo(vacancy.JobCategories);
        result.ExperienceLevel.Should().Be(vacancy.ExperienceLevel);
        result.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public async Task Handle_VacancyNotFound_PropagatesNotFoundException()
    {
        // Arrange
        var vacancyId = Guid.NewGuid();

        _vacancyRepositoryMock
            .Setup(x => x.GetByIdAsync(vacancyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Vacancy", vacancyId));

        // Act
        var act = () => _handler.Handle(
            new GetVacancyQuery { VacancyId = vacancyId, UserId = Guid.NewGuid() },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_VacancyOwnedByDifferentUser_ThrowsNotFoundException()
    {
        // Arrange
        var requestingUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var vacancy = new Vacancy
        {
            Id = vacancyId,
            UserId = ownerUserId,
            Title = "Some Vacancy",
            JobSource = new JobSource { Name = "LinkedIn", Url = "https://linkedin.com/jobs" },
            CreatedAt = now,
            UpdatedAt = now,
        };

        _vacancyRepositoryMock
            .Setup(x => x.GetByIdAsync(vacancyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vacancy);

        // Act
        var act = () => _handler.Handle(
            new GetVacancyQuery { VacancyId = vacancyId, UserId = requestingUserId },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
