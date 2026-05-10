using FluentAssertions;
using JobNecto.Application.CoverLetters;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;
using Moq;

namespace JobNecto.Tests.Application.CoverLetters;

public class GetCoverLetterQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICoverLetterRepository> _repositoryMock;
    private readonly GetCoverLetterQueryHandler _handler;

    public GetCoverLetterQueryHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repositoryMock = new Mock<ICoverLetterRepository>();
        _uowMock.Setup(x => x.CoverLetterRepository).Returns(_repositoryMock.Object);
        _handler = new GetCoverLetterQueryHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedCoverLetter_ReturnsDetailWithNestedVacancy()
    {
        var userId = Guid.NewGuid();
        var coverLetterId = Guid.NewGuid();

        var detail = new CoverLetterDetailResult
        {
            Id = coverLetterId,
            UserId = userId,
            VacancyId = Guid.NewGuid(),
            Content = new string('a', 100),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            Vacancy = new VacancyInCoverLetterResult
            {
                Id = Guid.NewGuid(),
                Title = "Backend Engineer",
                Company = "Contoso",
                WorkLocationType = WorkLocationType.Hybrid,
                Location = Location.Turkey,
            },
        };

        _repositoryMock
            .Setup(x => x.GetDetailByIdAsync(coverLetterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await _handler.Handle(
            new GetCoverLetterQuery { CoverLetterId = coverLetterId, UserId = userId },
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(coverLetterId);
        result.Content.Should().Be(detail.Content);
        result.Vacancy.Title.Should().Be("Backend Engineer");
        result.Vacancy.Company.Should().Be("Contoso");
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        var coverLetterId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetDetailByIdAsync(coverLetterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoverLetterDetailResult?)null);

        var act = () => _handler.Handle(
            new GetCoverLetterQuery { CoverLetterId = coverLetterId, UserId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CrossUserAccess_ThrowsNotFoundException()
    {
        var ownerUserId = Guid.NewGuid();
        var requesterUserId = Guid.NewGuid();
        var coverLetterId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetDetailByIdAsync(coverLetterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoverLetterDetailResult
            {
                Id = coverLetterId,
                UserId = ownerUserId,
                VacancyId = Guid.NewGuid(),
                Content = new string('a', 70),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Vacancy = new VacancyInCoverLetterResult { Id = Guid.NewGuid() },
            });

        var act = () => _handler.Handle(
            new GetCoverLetterQuery { CoverLetterId = coverLetterId, UserId = requesterUserId },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}