using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.Resumes;

public class GetResumeQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMutableRepository<Resume>> _resumeRepoMock;
    private readonly GetResumeQueryHandler _handler;

    public GetResumeQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _resumeRepoMock = new Mock<IMutableRepository<Resume>>();
        _uowMock.Setup(x => x.ResumeRepository).Returns(_resumeRepoMock.Object);
        _handler = new GetResumeQueryHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_CrossUserAccess_ThrowsNotFoundException()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Title = "Owner Resume",
            Skills = ["C#"],
        };

        _resumeRepoMock
            .Setup(x => x.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        var query = new GetResumeQuery { ResumeId = resume.Id, UserId = attackerId };

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}