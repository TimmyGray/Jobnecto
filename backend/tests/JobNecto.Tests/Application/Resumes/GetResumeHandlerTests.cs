using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.Resumes;

public class GetResumeHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IEditableRepository<Resume>> _resumeRepoMock;
    private readonly GetResumeQueryHandler _handler;

    public GetResumeHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _resumeRepoMock = new Mock<IEditableRepository<Resume>>();
        _uowMock.Setup(x => x.ResumeRepository).Returns(_resumeRepoMock.Object);
        _handler = new GetResumeQueryHandler(_uowMock.Object);
    }

    private static Resume MakeResume(Guid userId) => new()
    {
        UserId = userId,
        Title = "Software Engineer",
        Skills = ["C#", "SQL"],
    };

    [Fact]
    public async Task Handle_OwnedResume_ReturnsCorrectResumeResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resume = MakeResume(userId);
        var query = new GetResumeQuery { ResumeId = resume.Id, UserId = userId };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(resume.Id);
        result.UserId.Should().Be(userId);
        result.Title.Should().Be(resume.Title);
    }

    [Fact]
    public async Task Handle_NotFound_PropagatesNotFoundException()
    {
        // Arrange
        var resumeId = Guid.NewGuid();
        var query = new GetResumeQuery { ResumeId = resumeId, UserId = Guid.NewGuid() };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resumeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Resume", resumeId));

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WrongUser_ThrowsNotFoundException()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var resume = MakeResume(ownerUserId);
        var query = new GetResumeQuery { ResumeId = resume.Id, UserId = differentUserId };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert — must throw NotFoundException (not ForbiddenException) to prevent info leak
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
