using FluentAssertions;
using JobNecto.Application.Educations;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using Moq;

namespace JobNecto.Tests.Application.Educations;

public class UpdateEducationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<Education>> _educationRepoMock;
    private readonly UpdateEducationCommandHandler _handler;

    public UpdateEducationCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _educationRepoMock = new Mock<IMutableRepository<Education>>();
        _uowMock.Setup(x => x.EducationRepository).Returns(_educationRepoMock.Object);
        _handler = new UpdateEducationCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedRecordPartialPayload_UpdatesOnlyProvidedFields()
    {
        var userId = Guid.NewGuid();
        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Original title",
            Specialization = "Original spec",
            Degree = Degree.Bachelor,
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        var command = new UpdateEducationCommand
        {
            EducationId = education.Id,
            UserId = userId,
            Title = "Updated title",
            // Specialization and Degree intentionally omitted — should remain unchanged
        };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(education.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(education);

        _educationRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Education entity, CancellationToken _) => entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Updated title");
        result.Specialization.Should().Be("Original spec");
        result.Degree.Should().Be("bachelor");

        _educationRepoMock.Verify(x => x.UpdateAsync(education, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatedAt_IsRefreshedWithUtcNow()
    {
        var userId = Guid.NewGuid();
        var previousUpdatedAt = DateTime.UtcNow.AddMinutes(-5);

        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Original",
            Specialization = "CS",
            Degree = Degree.Bachelor,
            UpdatedAt = previousUpdatedAt,
        };

        var command = new UpdateEducationCommand
        {
            EducationId = education.Id,
            UserId = userId,
            Title = "New title",
        };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(education.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(education);

        _educationRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Education entity, CancellationToken _) => entity);

        await _handler.Handle(command, CancellationToken.None);

        education.UpdatedAt.Should().BeAfter(previousUpdatedAt);
        education.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Handle_DegreeUpdate_ParsesEnumCorrectly()
    {
        var userId = Guid.NewGuid();
        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Title",
            Specialization = "Spec",
            Degree = Degree.Bachelor,
        };

        var command = new UpdateEducationCommand
        {
            EducationId = education.Id,
            UserId = userId,
            Degree = "master",
        };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(education.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(education);

        _educationRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Education entity, CancellationToken _) => entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Degree.Should().Be("master");
    }

    [Fact]
    public async Task Handle_CrossUserUpdate_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Title = "Owner education",
            Specialization = "Math",
            Degree = Degree.PhD,
        };

        var command = new UpdateEducationCommand
        {
            EducationId = education.Id,
            UserId = attackerId,
            Title = "Malicious update",
        };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(education.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(education);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _educationRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordNotFound_PropagatesNotFoundException()
    {
        var educationId = Guid.NewGuid();

        var command = new UpdateEducationCommand
        {
            EducationId = educationId,
            UserId = Guid.NewGuid(),
            Title = "Updated",
        };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(educationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Education", educationId));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
