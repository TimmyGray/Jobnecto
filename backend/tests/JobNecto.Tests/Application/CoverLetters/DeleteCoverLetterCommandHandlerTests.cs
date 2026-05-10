using FluentAssertions;
using JobNecto.Application.CoverLetters;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using MediatR;
using Moq;

namespace JobNecto.Tests.Application.CoverLetters;

public class DeleteCoverLetterCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICoverLetterRepository> _repositoryMock;
    private readonly DeleteCoverLetterCommandHandler _handler;

    public DeleteCoverLetterCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repositoryMock = new Mock<ICoverLetterRepository>();
        _uowMock.Setup(x => x.CoverLetterRepository).Returns(_repositoryMock.Object);
        _handler = new DeleteCoverLetterCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedCoverLetter_CallsSoftDeleteAndPersists()
    {
        var userId = Guid.NewGuid();
        var coverLetter = new CoverLetter
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VacancyId = Guid.NewGuid(),
            Content = new string('a', 70),
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(coverLetter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coverLetter);

        _repositoryMock
            .Setup(x => x.SoftDeleteAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()))
            .Callback<CoverLetter, CancellationToken>((entity, _) =>
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
            })
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new DeleteCoverLetterCommand { CoverLetterId = coverLetter.Id, UserId = userId },
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        coverLetter.IsDeleted.Should().BeTrue();
        coverLetter.DeletedAt.Should().NotBeNull();

        _repositoryMock.Verify(x => x.SoftDeleteAsync(coverLetter, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_PropagatesNotFoundException()
    {
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("CoverLetter", id));

        var act = () => _handler.Handle(
            new DeleteCoverLetterCommand { CoverLetterId = id, UserId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(x => x.SoftDeleteAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CrossUserDelete_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var coverLetter = new CoverLetter
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            VacancyId = Guid.NewGuid(),
            Content = new string('a', 70),
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(coverLetter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coverLetter);

        var act = () => _handler.Handle(
            new DeleteCoverLetterCommand { CoverLetterId = coverLetter.Id, UserId = attackerId },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _repositoryMock.Verify(x => x.SoftDeleteAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}