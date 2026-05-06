using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes;
using JobNecto.Domain.Entities;
using MediatR;
using Moq;

namespace JobNecto.Tests.Application.Resumes;

public class DeleteResumeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<Resume>> _resumeRepoMock;
    private readonly DeleteResumeCommandHandler _handler;

    public DeleteResumeCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _resumeRepoMock = new Mock<IMutableRepository<Resume>>();
        _uowMock.Setup(x => x.ResumeRepository).Returns(_resumeRepoMock.Object);
        _handler = new DeleteResumeCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedResume_CallsSoftDeleteAndPersists()
    {
        var userId = Guid.NewGuid();
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Resume",
            Skills = ["C#"],
            IsDeleted = false,
            DeletedAt = null,
        };

        var command = new DeleteResumeCommand
        {
            ResumeId = resume.Id,
            UserId = userId,
        };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        _resumeRepoMock
            .Setup(x => x.SoftDeleteAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
            .Callback<Resume, CancellationToken>((entity, _) =>
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
            })
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        resume.IsDeleted.Should().BeTrue();
        resume.DeletedAt.Should().NotBeNull();
        resume.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);

        _resumeRepoMock.Verify(x => x.SoftDeleteAsync(resume, It.IsAny<CancellationToken>()), Times.Once);
        _resumeRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ResumeNotFound_PropagatesNotFoundException()
    {
        var resumeId = Guid.NewGuid();

        var command = new DeleteResumeCommand
        {
            ResumeId = resumeId,
            UserId = Guid.NewGuid(),
        };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resumeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Resume", resumeId));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _resumeRepoMock.Verify(x => x.SoftDeleteAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CrossUserDelete_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Title = "Owner resume",
            Skills = ["C#"],
        };

        var command = new DeleteResumeCommand
        {
            ResumeId = resume.Id,
            UserId = attackerId,
        };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _resumeRepoMock.Verify(x => x.SoftDeleteAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
