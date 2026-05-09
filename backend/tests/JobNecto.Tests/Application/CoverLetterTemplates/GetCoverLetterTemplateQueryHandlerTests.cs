using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.CoverLetterTemplates;

public class GetCoverLetterTemplateQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<CoverLetterTemplate>> _repositoryMock;
    private readonly GetCoverLetterTemplateQueryHandler _handler;

    public GetCoverLetterTemplateQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repositoryMock = new Mock<IMutableRepository<CoverLetterTemplate>>();
        _uowMock.Setup(x => x.CoverLetterTemplateRepository).Returns(_repositoryMock.Object);
        _handler = new GetCoverLetterTemplateQueryHandler(_uowMock.Object);
    }

    private static CoverLetterTemplate MakeTemplate(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = "My Template",
        Content = new string('a', 50),
    };

    [Fact]
    public async Task Handle_OwnedTemplate_ReturnsFullCoverLetterTemplateResult()
    {
        var userId = Guid.NewGuid();
        var template = MakeTemplate(userId);
        var query = new GetCoverLetterTemplateQuery { CoverLetterTemplateId = template.Id, UserId = userId };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(template.Id);
        result.UserId.Should().Be(userId);
        result.Name.Should().Be(template.Name);
        result.Content.Should().Be(template.Content);
    }

    [Fact]
    public async Task Handle_NonExistentId_PropagatesNotFoundException()
    {
        var templateId = Guid.NewGuid();
        var query = new GetCoverLetterTemplateQuery { CoverLetterTemplateId = templateId, UserId = Guid.NewGuid() };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("CoverLetterTemplate", templateId));

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CrossUserAccess_ThrowsNotFoundException()
    {
        var ownerUserId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var template = MakeTemplate(ownerUserId);
        var query = new GetCoverLetterTemplateQuery { CoverLetterTemplateId = template.Id, UserId = anotherUserId };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var act = () => _handler.Handle(query, CancellationToken.None);

        // Must throw NotFoundException (not ForbiddenException) to prevent existence leakage
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
