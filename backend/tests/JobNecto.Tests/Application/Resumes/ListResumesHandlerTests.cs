using FluentAssertions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;
using Moq;

namespace JobNecto.Tests.Application.Resumes;

public class ListResumesHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IEditableRepository<Resume>> _resumeRepoMock;
    private readonly ListResumesQueryHandler _handler;

    public ListResumesHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _resumeRepoMock = new Mock<IEditableRepository<Resume>>();
        _uowMock.Setup(x => x.ResumeRepository).Returns(_resumeRepoMock.Object);
        _handler = new ListResumesQueryHandler(_uowMock.Object);
    }

    private static PagedResult<Resume> MakePagedResult(IReadOnlyList<Resume> items, bool hasNext = false) =>
        new(items, items.Count, items.LastOrDefault()?.Id, items.LastOrDefault()?.UpdatedAt, 20, hasNext);

    [Fact]
    public async Task Handle_ForwardsCorrectPagedQueryToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lastSeenId = Guid.NewGuid();
        var lastSeenUpdatedAt = DateTime.UtcNow.AddMinutes(-5);

        var query = new ListResumesQuery
        {
            UserId = userId,
            PageSize = 10,
            LastSeenId = lastSeenId,
            LastSeenUpdatedAt = lastSeenUpdatedAt,
        };

        PagedQuery? capturedQuery = null;
        _resumeRepoMock
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
        var query = new ListResumesQuery
        {
            UserId = Guid.NewGuid(),
            PageSize = 999,
        };

        PagedQuery? capturedQuery = null;
        _resumeRepoMock
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
        var query = new ListResumesQuery
        {
            UserId = Guid.NewGuid(),
            PageSize = 0,
        };

        PagedQuery? capturedQuery = null;
        _resumeRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(MakePagedResult([]));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert — AC 3: values < 1 treated as default (20)
        capturedQuery!.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPagedResult()
    {
        // Arrange
        var query = new ListResumesQuery { UserId = Guid.NewGuid() };

        _resumeRepoMock
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
    public async Task Handle_MapsEntitiesToResumeResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Backend Dev",
            Skills = ["C#", "SQL"],
            WorkLocationType = Domain.Enums.WorkLocationType.Remote,
        };

        _resumeRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult([resume]));

        var query = new ListResumesQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(resume.Id);
        item.Title.Should().Be(resume.Title);
        item.Skills.Should().BeEquivalentTo(resume.Skills);
    }

    [Fact]
    public async Task Handle_PreservesPagedResultMetadata()
    {
        // Arrange
        var lastSeenId = Guid.NewGuid();
        var lastSeenUpdatedAt = DateTime.UtcNow;

        var pagedResult = new PagedResult<Resume>([], 42, lastSeenId, lastSeenUpdatedAt, 20, true);

        _resumeRepoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new ListResumesQuery { UserId = Guid.NewGuid() };

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
