using FluentAssertions;
using JobNecto.Application.Educations;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using MediatR;
using Moq;

namespace JobNecto.Tests.Application.Educations;

public class DeleteEducationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IEditableRepository<Education>> _educationRepoMock;
    private readonly DeleteEducationCommandHandler _handler;

    public DeleteEducationCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _educationRepoMock = new Mock<IEditableRepository<Education>>();
        _uowMock.Setup(x => x.EducationRepository).Returns(_educationRepoMock.Object);
        _handler = new DeleteEducationCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedRecord_SetsSoftDeleteFlagsAndPersists()
    {
        var userId = Guid.NewGuid();
        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "BSc",
            Specialization = "CS",
            Degree = Degree.Bachelor,
            IsDeleted = false,
            DeletedAt = null,
        };

        var command = new DeleteEducationCommand { EducationId = education.Id, UserId = userId };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(education.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(education);

        _educationRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Education entity, CancellationToken _) => entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        education.IsDeleted.Should().BeTrue();
        education.DeletedAt.Should().NotBeNull();
        education.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);

        _educationRepoMock.Verify(x => x.UpdateAsync(education, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CrossUserDelete_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Title = "Owner education",
            Specialization = "Math",
            Degree = Degree.Master,
        };

        var command = new DeleteEducationCommand { EducationId = education.Id, UserId = attackerId };

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
        var command = new DeleteEducationCommand { EducationId = educationId, UserId = Guid.NewGuid() };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(educationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Education", educationId));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _educationRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
