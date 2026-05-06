using FluentAssertions;
using JobNecto.Application.Educations;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using Moq;

namespace JobNecto.Tests.Application.Educations;

public class GetEducationQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<Education>> _educationRepoMock;
    private readonly GetEducationQueryHandler _handler;

    public GetEducationQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _educationRepoMock = new Mock<IMutableRepository<Education>>();
        _uowMock.Setup(x => x.EducationRepository).Returns(_educationRepoMock.Object);
        _handler = new GetEducationQueryHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedRecord_ReturnsEducationResult()
    {
        var userId = Guid.NewGuid();
        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Bachelor of Science",
            Specialization = "Computer Science",
            Degree = Degree.Bachelor,
        };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(education.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(education);

        var query = new GetEducationQuery { EducationId = education.Id, UserId = userId };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(education.Id);
        result.UserId.Should().Be(userId);
        result.Title.Should().Be("Bachelor of Science");
        result.Specialization.Should().Be("Computer Science");
        result.Degree.Should().Be("bachelor");
    }

    [Fact]
    public async Task Handle_CrossUserAccess_ThrowsNotFoundException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Title = "Owner's education",
            Specialization = "Physics",
            Degree = Degree.Master,
        };

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(education.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(education);

        var query = new GetEducationQuery { EducationId = education.Id, UserId = attackerId };

        var act = () => _handler.Handle(query, CancellationToken.None);

        // Must be 404, not 403 — no existence leakage
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RecordNotFound_PropagatesNotFoundException()
    {
        var educationId = Guid.NewGuid();

        _educationRepoMock
            .Setup(x => x.GetByIdAsync(educationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Education", educationId));

        var query = new GetEducationQuery { EducationId = educationId, UserId = Guid.NewGuid() };

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
