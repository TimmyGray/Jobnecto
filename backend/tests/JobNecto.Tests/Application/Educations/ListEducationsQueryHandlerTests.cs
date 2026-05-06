using FluentAssertions;
using JobNecto.Application.Educations;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;
using Moq;

namespace JobNecto.Tests.Application.Educations;

public class ListEducationsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<Education>> _educationRepoMock;
    private readonly ListEducationsQueryHandler _handler;

    public ListEducationsQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _educationRepoMock = new Mock<IMutableRepository<Education>>();
        _uowMock.Setup(x => x.EducationRepository).Returns(_educationRepoMock.Object);
        _handler = new ListEducationsQueryHandler(_uowMock.Object);
    }

    private static PagedResult<Education> MakePagedResult(
        IReadOnlyList<Education> items,
        bool hasNext = false
    ) =>
        new(
            items,
            items.Count,
            items.LastOrDefault()?.Id,
            items.LastOrDefault()?.UpdatedAt,
            20,
            hasNext
        );

    [Fact]
    public async Task Handle_ForwardsCorrectPagedQueryToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lastSeenId = Guid.NewGuid();
        var lastSeenUpdatedAt = DateTime.UtcNow.AddMinutes(-5);

        var query = new ListEducationsQuery
        {
            UserId = userId,
            PageSize = 10,
            LastSeenId = lastSeenId,
            LastSeenUpdatedAt = lastSeenUpdatedAt,
        };

        PagedQuery? capturedQuery = null;
        _educationRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(MakePagedResult([]));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.UserId.Should().Be(userId);
        capturedQuery.PageSize.Should().Be(10);
        capturedQuery.LastSeenId.Should().Be(lastSeenId);
        capturedQuery.LastSeenUpdatedAt.Should().Be(lastSeenUpdatedAt);
    }

    [Fact]
    public async Task Handle_PageSizeAbove100_IsCappedAt100()
    {
        // Arrange
        var query = new ListEducationsQuery { UserId = Guid.NewGuid(), PageSize = 999 };

        PagedQuery? capturedQuery = null;
        _educationRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(MakePagedResult([]));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedQuery!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Handle_PageSizeBelowOne_DefaultsToTwenty()
    {
        // Arrange
        var query = new ListEducationsQuery { UserId = Guid.NewGuid(), PageSize = 0 };

        PagedQuery? capturedQuery = null;
        _educationRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(MakePagedResult([]));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedQuery!.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPagedResult()
    {
        // Arrange
        var query = new ListEducationsQuery { UserId = Guid.NewGuid() };

        _educationRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult([]));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MapsEntitiesToEducationResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Bachelor of Science",
            Specialization = "Computer Science",
            Degree = Domain.Enums.Degree.Bachelor,
        };

        _educationRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult([education]));

        var query = new ListEducationsQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(education.Id);
        item.UserId.Should().Be(education.UserId);
        item.Title.Should().Be(education.Title);
        item.Specialization.Should().Be(education.Specialization);
        item.Degree.Should().Be("bachelor");
    }

    [Fact]
    public async Task Handle_PreservesPagedResultMetadata()
    {
        // Arrange
        var lastSeenId = Guid.NewGuid();
        var lastSeenUpdatedAt = DateTime.UtcNow;

        var pagedResult = new PagedResult<Education>(
            [],
            42,
            lastSeenId,
            lastSeenUpdatedAt,
            20,
            true
        );

        _educationRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new ListEducationsQuery { UserId = Guid.NewGuid() };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(42);
        result.LastSeenId.Should().Be(lastSeenId);
        result.LastSeenUpdatedAt.Should().Be(lastSeenUpdatedAt);
        result.PageSize.Should().Be(20);
        result.HasNext.Should().BeTrue();
    }
}
