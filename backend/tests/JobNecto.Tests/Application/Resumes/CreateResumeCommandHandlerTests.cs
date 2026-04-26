using FluentAssertions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.Resumes;

public class CreateResumeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IEditableRepository<Resume>> _resumeRepoMock;
    private readonly CreateResumeCommandHandler _handler;

    public CreateResumeCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _resumeRepoMock = new Mock<IEditableRepository<Resume>>();
        _uowMock.Setup(x => x.ResumeRepository).Returns(_resumeRepoMock.Object);
        _handler = new CreateResumeCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesResume()
    {
        // Arrange
        var request = new CreateResumeCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Dev",
            Skills = new[] { "C#" },
            WorkLocationType = "remote"
        };

        Resume? capturedEntity = null;
        _resumeRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
            .Callback<Resume, CancellationToken>((r, _) => capturedEntity = r)
            .ReturnsAsync((Resume r, CancellationToken _) => r);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);
        capturedEntity.Should().NotBeNull();
        capturedEntity!.UserId.Should().Be(request.UserId);
        capturedEntity.Title.Should().Be(request.Title);
        
        _resumeRepoMock.Verify(x => x.CreateAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
