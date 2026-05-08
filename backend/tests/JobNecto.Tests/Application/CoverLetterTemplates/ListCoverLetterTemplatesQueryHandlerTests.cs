using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;
using Moq;

namespace JobNecto.Tests.Application.CoverLetterTemplates;

public class ListCoverLetterTemplatesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<CoverLetterTemplate>> _repoMock;
    private readonly ListCoverLetterTemplatesQueryHandler _handler;

    public ListCoverLetterTemplatesQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repoMock = new Mock<IMutableRepository<CoverLetterTemplate>>();
        _uowMock.Setup(x => x.CoverLetterTemplateRepository).Returns(_repoMock.Object);
        _handler = new ListCoverLetterTemplatesQueryHandler(_uowMock.Object);
    }

    private static PagedResult<CoverLetterTemplate> MakePagedResult(
        IReadOnlyList<CoverLetterTemplate> items,
        bool hasNext = false) =>
        new(items, items.Count, items.LastOrDefault()?.Id, items.LastOrDefault()?.UpdatedAt, 20, hasNext);

    private static CoverLetterTemplate MakeTemplate(
        Guid userId,
        string name = "Template",
        string? content = null) =>
        new()
        {
            UserId = userId,
            Name = name,
            Content = content ?? new string('a', 50),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task Handle_NoTemplates_ReturnsEmptyPagedResult()
    {
        _repoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult([]));

        var result = await _handler.Handle(new ListCoverLetterTemplatesQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithTemplates_ReturnsProjectedListItems()
    {
        var userId = Guid.NewGuid();
        var template = MakeTemplate(userId, "Senior Dev Cover Letter", new string('x', 50));

        _repoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult([template]));

        var result = await _handler.Handle(new ListCoverLetterTemplatesQuery { UserId = userId }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(template.Id);
        item.Name.Should().Be(template.Name);
        item.ContentPreview.Should().Be(template.Content);
        item.CreatedAt.Should().Be(template.CreatedAt);
        item.UpdatedAt.Should().Be(template.UpdatedAt);
    }

    [Fact]
    public async Task Handle_ContentOver200Chars_ContentPreviewTruncatedTo200()
    {
        var userId = Guid.NewGuid();
        var longContent = new string('z', 300);
        var template = MakeTemplate(userId, content: longContent);

        _repoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult([template]));

        var result = await _handler.Handle(new ListCoverLetterTemplatesQuery { UserId = userId }, CancellationToken.None);

        result.Items[0].ContentPreview.Length.Should().Be(200);
        result.Items[0].ContentPreview.Should().Be(longContent[..200]);
    }

    [Fact]
    public async Task Handle_ContentExactly200Chars_ContentPreviewNotTruncated()
    {
        var userId = Guid.NewGuid();
        var exactContent = new string('y', 200);
        var template = MakeTemplate(userId, content: exactContent);

        _repoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult([template]));

        var result = await _handler.Handle(new ListCoverLetterTemplatesQuery { UserId = userId }, CancellationToken.None);

        result.Items[0].ContentPreview.Should().Be(exactContent);
        result.Items[0].ContentPreview.Length.Should().Be(200);
    }

    [Fact]
    public async Task Handle_PageSizeCappedAt100()
    {
        PagedQuery? captured = null;
        _repoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(MakePagedResult([]));

        await _handler.Handle(new ListCoverLetterTemplatesQuery { UserId = Guid.NewGuid(), PageSize = 200 }, CancellationToken.None);

        captured!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Handle_PageSizeLessThan1_DefaultsTo20()
    {
        PagedQuery? captured = null;
        _repoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(MakePagedResult([]));

        await _handler.Handle(new ListCoverLetterTemplatesQuery { UserId = Guid.NewGuid(), PageSize = 0 }, CancellationToken.None);

        captured!.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_ForwardsSearchToPagedQuery()
    {
        PagedQuery? captured = null;
        _repoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .Callback<PagedQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(MakePagedResult([]));

        await _handler.Handle(
            new ListCoverLetterTemplatesQuery { UserId = Guid.NewGuid(), Search = "senior" },
            CancellationToken.None);

        captured!.Search.Should().Be("senior");
    }

    [Fact]
    public async Task Handle_PreservesPagedResultMetadata()
    {
        var lastSeenId = Guid.NewGuid();
        var lastSeenUpdatedAt = DateTime.UtcNow;

        var pagedResult = new PagedResult<CoverLetterTemplate>([], 42, lastSeenId, lastSeenUpdatedAt, 20, true);

        _repoMock
            .Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _handler.Handle(new ListCoverLetterTemplatesQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.TotalCount.Should().Be(42);
        result.LastSeenId.Should().Be(lastSeenId);
        result.LastSeenUpdatedAt.Should().Be(lastSeenUpdatedAt);
        result.HasNext.Should().BeTrue();
    }
}
