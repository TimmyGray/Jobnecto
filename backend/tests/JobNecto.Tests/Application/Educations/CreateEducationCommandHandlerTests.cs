using FluentAssertions;
using JobNecto.Application.Educations;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.Educations;

public class CreateEducationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMutableRepository<Education>> _educationRepositoryMock;
    private readonly CreateEducationCommandHandler _handler;

    public CreateEducationCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _educationRepositoryMock = new Mock<IMutableRepository<Education>>();

        _unitOfWorkMock
            .Setup(x => x.EducationRepository)
            .Returns(_educationRepositoryMock.Object);

        _handler = new CreateEducationCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsAndReturnsEducation()
    {
        var command = new CreateEducationCommand
        {
            UserId = Guid.NewGuid(),
            Title = "Bachelor of Science",
            Specialization = "Computer Science",
            Degree = "bachelor",
        };

        Education? capturedEntity = null;
        _educationRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()))
            .Callback<Education, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((Education entity, CancellationToken _) => entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(command.UserId);
        result.Title.Should().Be(command.Title);
        result.Specialization.Should().Be(command.Specialization);
        result.Degree.Should().Be("bachelor");

        capturedEntity.Should().NotBeNull();
        capturedEntity!.UserId.Should().Be(command.UserId);
        capturedEntity.Title.Should().Be(command.Title);
        capturedEntity.Specialization.Should().Be(command.Specialization);

        _educationRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}