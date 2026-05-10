using FluentAssertions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Vacancies;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;
using Moq;

namespace JobNecto.Tests.Application.Vacancies;

public class FilterVacanciesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IVacancyRepository> _vacancyRepositoryMock;
    private readonly FilterVacanciesQueryHandler _handler;

    public FilterVacanciesQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _vacancyRepositoryMock = new Mock<IVacancyRepository>();
        _uowMock.Setup(x => x.VacancyRepository).Returns(_vacancyRepositoryMock.Object);
        _handler = new FilterVacanciesQueryHandler(_uowMock.Object);
    }

    private static PagedResult<Vacancy> MakePagedResult(
        IReadOnlyList<Vacancy> items,
        int totalCount,
        Guid? lastSeenId,
        DateTime? lastSeenUpdatedAt,
        int pageSize,
        bool hasNext
    ) => new(items, totalCount, lastSeenId, lastSeenUpdatedAt, pageSize, hasNext);

    [Fact]
    public async Task Handle_PageSizeAbove100_IsCappedAt100()
    {
        // Arrange
        PagedQuery? capturedQuery = null;
        _vacancyRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<PagedQuery>(), It.IsAny<VacancyFilter?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, VacancyFilter?, CancellationToken>((q, _, _) => capturedQuery = q)
            .ReturnsAsync(MakePagedResult([], 0, null, null, 100, false));

        // Act
        await _handler.Handle(new FilterVacanciesQuery { PageSize = 500 }, CancellationToken.None);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Handle_PageSizeBelowOne_DefaultsToTwenty()
    {
        // Arrange
        PagedQuery? capturedQuery = null;
        _vacancyRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<PagedQuery>(), It.IsAny<VacancyFilter?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, VacancyFilter?, CancellationToken>((q, _, _) => capturedQuery = q)
            .ReturnsAsync(MakePagedResult([], 0, null, null, 20, false));

        // Act
        await _handler.Handle(new FilterVacanciesQuery { PageSize = 0 }, CancellationToken.None);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_ForwardsSortByToPagedQuery()
    {
        // Arrange
        PagedQuery? capturedQuery = null;
        _vacancyRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<PagedQuery>(), It.IsAny<VacancyFilter?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, VacancyFilter?, CancellationToken>((q, _, _) => capturedQuery = q)
            .ReturnsAsync(MakePagedResult([], 0, null, null, 20, false));

        // Act
        await _handler.Handle(new FilterVacanciesQuery { SortBy = "updatedAt" }, CancellationToken.None);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.SortBy.Should().Be("updatedAt");
    }

    [Fact]
    public async Task Handle_EmptyCriteria_ForwardsNullFilterAndCursorValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lastSeenId = Guid.NewGuid();
        var lastSeenUpdatedAt = DateTime.UtcNow.AddMinutes(-3);

        PagedQuery? capturedQuery = null;
        VacancyFilter? capturedFilter = null;

        _vacancyRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<PagedQuery>(), It.IsAny<VacancyFilter?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, VacancyFilter?, CancellationToken>((q, f, _) =>
            {
                capturedQuery = q;
                capturedFilter = f;
            })
            .ReturnsAsync(MakePagedResult([], 0, null, null, 20, false));

        var query = new FilterVacanciesQuery
        {
            UserId = userId,
            LastSeenId = lastSeenId,
            LastSeenUpdatedAt = lastSeenUpdatedAt,
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.UserId.Should().Be(userId);
        capturedQuery!.LastSeenId.Should().Be(lastSeenId);
        capturedQuery.LastSeenUpdatedAt.Should().Be(lastSeenUpdatedAt);
        capturedFilter.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonEmptyCriteria_BuildsVacancyFilter()
    {
        // Arrange
        VacancyFilter? capturedFilter = null;

        _vacancyRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<PagedQuery>(), It.IsAny<VacancyFilter?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, VacancyFilter?, CancellationToken>((_, f, _) => capturedFilter = f)
            .ReturnsAsync(MakePagedResult([], 0, null, null, 20, false));

        var query = new FilterVacanciesQuery
        {
            Company = "  MedStack  ",
            WorkLocationType = [WorkLocationType.Remote],
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Company.Should().Be("MedStack");
        capturedFilter.WorkLocationType.Should().BeEquivalentTo([WorkLocationType.Remote]);
    }

    [Fact]
    public async Task Handle_MapsVacancyWithNoSalary_ToNullSalaryInResult()
    {
        // Arrange
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Engineering role — details on apply",
            SalaryMin = null,
            SalaryMax = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            JobSource = new JobSource { Name = "RSSFeed", Url = "https://jobs.example/feed.xml" },
        };

        _vacancyRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<PagedQuery>(), It.IsAny<VacancyFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult([vacancy], 1, vacancy.Id, vacancy.CreatedAt, 20, false));

        // Act
        var result = await _handler.Handle(new FilterVacanciesQuery(), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Salary.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MapsEntitiesAndPreservesEnvelopeMetadata()
    {
        // Arrange
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Senior Backend Engineer",
            Company = "MedStack",
            Location = Location.Poland,
            WorkLocationType = WorkLocationType.Remote,
            SalaryMin = 95_000m,
            SalaryMax = 118_000m,
            Currency = Currency.EUR,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            JobSource = new JobSource { Name = "LinkedIn", Url = "https://www.linkedin.com/jobs" },
        };

        var expectedLastSeenId = vacancy.Id;
        var expectedLastSeenUpdatedAt = vacancy.CreatedAt;

        _vacancyRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<PagedQuery>(), It.IsAny<VacancyFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                MakePagedResult(
                    [vacancy],
                    totalCount: 42,
                    lastSeenId: expectedLastSeenId,
                    lastSeenUpdatedAt: expectedLastSeenUpdatedAt,
                    pageSize: 20,
                    hasNext: true
                )
            );

        // Act
        var result = await _handler.Handle(new FilterVacanciesQuery(), CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(42);
        result.PageSize.Should().Be(20);
        result.HasNext.Should().BeTrue();
        result.LastSeenId.Should().Be(expectedLastSeenId);
        result.LastSeenUpdatedAt.Should().Be(expectedLastSeenUpdatedAt);

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(vacancy.Id);
        item.Title.Should().Be(vacancy.Title);
        item.Company.Should().Be(vacancy.Company);
        item.Location.Should().Be(Location.Poland.ToString());
        item.WorkLocationType.Should().Be(WorkLocationType.Remote.ToString());
        item.Currency.Should().Be(Currency.EUR.ToString());
        item.CreatedAt.Should().Be(vacancy.CreatedAt);
        item.Salary.Should().NotBeNull();
        item.Salary!.Min.Should().Be(vacancy.SalaryMin);
        item.Salary.Max.Should().Be(vacancy.SalaryMax);
    }
}