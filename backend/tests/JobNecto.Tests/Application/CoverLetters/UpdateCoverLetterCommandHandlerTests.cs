using FluentAssertions;
using JobNecto.Application.CoverLetters;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.CoverLetters;

public class UpdateCoverLetterCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICoverLetterRepository> _repositoryMock;
    private readonly UpdateCoverLetterCommandHandler _handler;

    public UpdateCoverLetterCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repositoryMock = new Mock<ICoverLetterRepository>();
        _uowMock.Setup(x => x.CoverLetterRepository).Returns(_repositoryMock.Object);
        _handler = new UpdateCoverLetterCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedCoverLetter_UpdatesContentAndReturnsResult()
    {
        var userId = Guid.NewGuid();
        var coverLetter = new CoverLetter
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VacancyId = Guid.NewGuid(),
            Content = new string('a', 60),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5),
        };

        var previousUpdatedAt = coverLetter.UpdatedAt;

        _repositoryMock
            .Setup(x => x.GetByIdAsync(coverLetter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coverLetter);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoverLetter entity, CancellationToken _) => entity);

        var result = await _handler.Handle(
            new UpdateCoverLetterCommand
            {
                CoverLetterId = coverLetter.Id,
                UserId = userId,
                Content = new string('b', 90),
            },
            CancellationToken.None);

        result.Id.Should().Be(coverLetter.Id);
        result.VacancyId.Should().Be(coverLetter.VacancyId);
        result.Content.Should().Be(new string('b', 90));
        result.UpdatedAt.Should().BeAfter(previousUpdatedAt);

        _repositoryMock.Verify(x => x.UpdateAsync(coverLetter, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CrossUserUpdate_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var coverLetter = new CoverLetter
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            VacancyId = Guid.NewGuid(),
            Content = new string('a', 80),
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(coverLetter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coverLetter);

        var act = () => _handler.Handle(
            new UpdateCoverLetterCommand
            {
                CoverLetterId = coverLetter.Id,
                UserId = attackerId,
                Content = new string('x', 80),
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotFound_PropagatesNotFoundException()
    {
        var coverLetterId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(coverLetterId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("CoverLetter", coverLetterId));

        var act = () => _handler.Handle(
            new UpdateCoverLetterCommand
            {
                CoverLetterId = coverLetterId,
                UserId = Guid.NewGuid(),
                Content = new string('z', 90),
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}