using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using Moq;

namespace JobNecto.Tests.Application.Resumes;

public class UpdateResumeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IEditableRepository<Resume>> _resumeRepoMock;
    private readonly UpdateResumeCommandHandler _handler;

    public UpdateResumeCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _resumeRepoMock = new Mock<IEditableRepository<Resume>>();
        _uowMock.Setup(x => x.ResumeRepository).Returns(_resumeRepoMock.Object);
        _handler = new UpdateResumeCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedResumeAndPartialPayload_UpdatesOnlyProvidedFields()
    {
        var userId = Guid.NewGuid();
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Original title",
            Salary = 1000,
            Currency = Currency.USD,
            Skills = ["C#"],
            WorkLocationType = WorkLocationType.Remote,
            Experience = Experience.OneToThreeYears,
            Projects = ["Project A"],
            Certifications = ["Cert A"],
            ExcludedWords = ["Legacy"],
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        var command = new UpdateResumeCommand
        {
            ResumeId = resume.Id,
            UserId = userId,
            Title = "Updated title",
            Skills = ["C#", "SQL"],
        };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        _resumeRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resume entity, CancellationToken _) => entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(resume.Id);
        result.Title.Should().Be("Updated title");
        result.Skills.Should().BeEquivalentTo(["C#", "SQL"]);
        result.Salary.Should().Be(1000);
        result.Currency.Should().Be("USD");
        result.WorkLocationType.Should().Be("Remote");
        result.Experience.Should().Be("OneToThreeYears");
        result.Projects.Should().BeEquivalentTo(["Project A"]);
        result.Certifications.Should().BeEquivalentTo(["Cert A"]);

        _resumeRepoMock.Verify(x => x.UpdateAsync(resume, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatedAt_IsRefreshedWithUtcNow()
    {
        var userId = Guid.NewGuid();
        var previousUpdatedAt = DateTime.UtcNow.AddMinutes(-5);

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Original",
            Skills = ["C#"],
            UpdatedAt = previousUpdatedAt,
        };

        var command = new UpdateResumeCommand
        {
            ResumeId = resume.Id,
            UserId = userId,
            Title = "New title",
        };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        _resumeRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resume entity, CancellationToken _) => entity);

        await _handler.Handle(command, CancellationToken.None);

        resume.UpdatedAt.Should().BeAfter(previousUpdatedAt);
        resume.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Handle_ResumeNotFound_PropagatesNotFoundException()
    {
        var resumeId = Guid.NewGuid();

        var command = new UpdateResumeCommand
        {
            ResumeId = resumeId,
            UserId = Guid.NewGuid(),
            Title = "Updated",
        };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resumeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Resume", resumeId));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CrossUserUpdate_ThrowsForbiddenException()
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

        var command = new UpdateResumeCommand
        {
            ResumeId = resume.Id,
            UserId = attackerId,
            Title = "Malicious update",
        };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _resumeRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
