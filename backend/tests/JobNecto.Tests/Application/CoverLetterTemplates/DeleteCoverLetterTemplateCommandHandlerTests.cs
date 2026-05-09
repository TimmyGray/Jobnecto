using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using MediatR;
using Moq;

namespace JobNecto.Tests.Application.CoverLetterTemplates;

public class DeleteCoverLetterTemplateCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<CoverLetterTemplate>> _repositoryMock;
    private readonly DeleteCoverLetterTemplateCommandHandler _handler;

    public DeleteCoverLetterTemplateCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repositoryMock = new Mock<IMutableRepository<CoverLetterTemplate>>();
        _uowMock.Setup(x => x.CoverLetterTemplateRepository).Returns(_repositoryMock.Object);
        _handler = new DeleteCoverLetterTemplateCommandHandler(_uowMock.Object);
    }

    private static CoverLetterTemplate MakeTemplate(Guid userId) => new()
    {
        UserId = userId,
        Name = "My Template",
        Content = new string('a', 50),
    };

    [Fact]
    public async Task Handle_OwnedTemplate_CallsSoftDeleteAndPersists()
    {
        var userId = Guid.NewGuid();
        var template = MakeTemplate(userId);
        var command = new DeleteCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = template.Id,
            UserId = userId,
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _repositoryMock
            .Setup(x => x.SoftDeleteAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()))
            .Callback<CoverLetterTemplate, CancellationToken>((entity, _) =>
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
            })
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        template.IsDeleted.Should().BeTrue();
        template.DeletedAt.Should().NotBeNull();
        template.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);

        _repositoryMock.Verify(x => x.SoftDeleteAsync(template, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TemplateNotFound_PropagatesNotFoundException()
    {
        var templateId = Guid.NewGuid();
        var command = new DeleteCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = templateId,
            UserId = Guid.NewGuid(),
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("CoverLetterTemplate", templateId));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(x => x.SoftDeleteAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CrossUserDelete_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var template = MakeTemplate(ownerId);
        var command = new DeleteCoverLetterTemplateCommand
        {
            CoverLetterTemplateId = template.Id,
            UserId = attackerId,
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _repositoryMock.Verify(x => x.SoftDeleteAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
