using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.CoverLetterTemplates;

public class UpdateCoverLetterTemplateCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<CoverLetterTemplate>> _repositoryMock;
    private readonly UpdateCoverLetterTemplateCommandHandler _handler;

    public UpdateCoverLetterTemplateCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repositoryMock = new Mock<IMutableRepository<CoverLetterTemplate>>();
        _uowMock.Setup(x => x.CoverLetterTemplateRepository).Returns(_repositoryMock.Object);
        _handler = new UpdateCoverLetterTemplateCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedTemplatePartialUpdate_ReturnsUpdatedResult()
    {
        var userId = Guid.NewGuid();
        var template = new CoverLetterTemplate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Original Name",
            Content = new string('a', 50),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = template.Id,
            UserId = userId,
            Name = "Updated Name",
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoverLetterTemplate entity, CancellationToken _) => entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(template.Id);
        result.UserId.Should().Be(userId);
        result.Name.Should().Be("Updated Name");
        result.Content.Should().Be(new string('a', 50));

        _repositoryMock.Verify(x => x.UpdateAsync(template, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatedAt_IsRefreshedWithUtcNow()
    {
        var userId = Guid.NewGuid();
        var previousUpdatedAt = DateTime.UtcNow.AddMinutes(-5);

        var template = new CoverLetterTemplate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Template",
            Content = new string('b', 50),
            UpdatedAt = previousUpdatedAt,
        };

        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = template.Id,
            UserId = userId,
            Content = new string('c', 60),
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoverLetterTemplate entity, CancellationToken _) => entity);

        await _handler.Handle(command, CancellationToken.None);

        template.UpdatedAt.Should().BeAfter(previousUpdatedAt);
        template.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Handle_CrossUserUpdate_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var template = new CoverLetterTemplate
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Name = "Owner Template",
            Content = new string('d', 50),
        };

        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = template.Id,
            UserId = attackerId,
            Name = "Malicious Update",
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotFound_PropagatesNotFoundException()
    {
        var templateId = Guid.NewGuid();

        var command = new UpdateCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = templateId,
            UserId = Guid.NewGuid(),
            Name = "Updated",
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("CoverLetterTemplate", templateId));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}