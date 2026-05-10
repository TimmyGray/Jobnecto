using FluentAssertions;
using JobNecto.Application.CoverLetters;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.ValueObjects;
using Moq;

namespace JobNecto.Tests.Application.CoverLetters;

public class ListCoverLettersQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICoverLetterRepository> _coverLetterRepositoryMock;
    private readonly ListCoverLettersQueryHandler _handler;

    public ListCoverLettersQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _coverLetterRepositoryMock = new Mock<ICoverLetterRepository>();
        _uowMock.Setup(x => x.CoverLetterRepository).Returns(_coverLetterRepositoryMock.Object);
        _handler = new ListCoverLettersQueryHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_NoCoverLetters_ReturnsEmptyPagedResult()
    {
        _coverLetterRepositoryMock
            .Setup(x => x.GetPagedListAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CoverLetterListItem>([], 0, null, null, 20, false));

        var result = await _handler.Handle(new ListCoverLettersQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ForwardsPagedQueryWithPageSizeCap()
    {
        var userId = Guid.NewGuid();
        var lastSeenId = Guid.NewGuid();
        var lastSeenUpdatedAt = DateTime.UtcNow;

        PagedQuery? captured = null;
        _coverLetterRepositoryMock
            .Setup(x => x.GetPagedListAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(new PagedResult<CoverLetterListItem>([], 0, null, null, 100, false));

        await _handler.Handle(
            new ListCoverLettersQuery
            {
                UserId = userId,
                PageSize = 999,
                LastSeenId = lastSeenId,
                LastSeenUpdatedAt = lastSeenUpdatedAt,
            },
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(userId);
        captured.PageSize.Should().Be(100);
        captured.LastSeenId.Should().Be(lastSeenId);
        captured.LastSeenUpdatedAt.Should().Be(lastSeenUpdatedAt);
    }

    [Fact]
    public async Task Handle_PageSizeLessThan1_DefaultsTo20()
    {
        PagedQuery? captured = null;
        _coverLetterRepositoryMock
            .Setup(x => x.GetPagedListAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(new PagedResult<CoverLetterListItem>([], 0, null, null, 20, false));

        await _handler.Handle(
            new ListCoverLettersQuery
            {
                UserId = Guid.NewGuid(),
                PageSize = 0,
            },
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WithItems_PreservesProjectedValuesAndMetadata()
    {
        var item = new CoverLetterListItem
        {
            Id = Guid.NewGuid(),
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Backend Engineer",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
        };

        var expected = new PagedResult<CoverLetterListItem>(
            [item],
            1,
            item.Id,
            item.CreatedAt,
            20,
            false);

        _coverLetterRepositoryMock
            .Setup(x => x.GetPagedListAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _handler.Handle(new ListCoverLettersQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.LastSeenId.Should().Be(item.Id);
        result.LastSeenUpdatedAt.Should().Be(item.CreatedAt);
        result.Items.Should().HaveCount(1);
        result.Items[0].VacancyTitle.Should().Be("Backend Engineer");
    }
}