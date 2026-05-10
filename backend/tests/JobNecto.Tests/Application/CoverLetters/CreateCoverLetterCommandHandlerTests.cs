using FluentAssertions;
using JobNecto.Application.CoverLetters;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace JobNecto.Tests.Application.CoverLetters;

public class CreateCoverLetterCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICoverLetterRepository> _coverLetterRepositoryMock;
    private readonly Mock<IVacancyRepository> _vacancyRepositoryMock;
    private readonly CreateCoverLetterCommandHandler _handler;

    public CreateCoverLetterCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _coverLetterRepositoryMock = new Mock<ICoverLetterRepository>();
        _vacancyRepositoryMock = new Mock<IVacancyRepository>();

        _unitOfWorkMock
            .Setup(x => x.CoverLetterRepository)
            .Returns(_coverLetterRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.VacancyRepository)
            .Returns(_vacancyRepositoryMock.Object);

        _handler = new CreateCoverLetterCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsExactlyOnceAndReturnsResultWithNonDefaultTimestamps()
    {
        var userId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        var command = new CreateCoverLetterCommand
        {
            UserId = userId,
            VacancyId = vacancyId,
            Content = new string('a', 50),
        };

        var vacancy = new Vacancy
        {
            Id = vacancyId,
            UserId = userId,
            JobSource = new JobSource { Name = "LinkedIn", Url = "https://linkedin.com/jobs" },
        };

        _vacancyRepositoryMock
            .Setup(x => x.GetByIdAsync(vacancyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vacancy);

        CoverLetter? capturedEntity = null;
        _coverLetterRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()))
            .Callback<CoverLetter, CancellationToken>((entity, _) =>
            {
                if (entity.Id == Guid.Empty)
                    entity.Id = Guid.NewGuid();
                capturedEntity = entity;
            })
            .ReturnsAsync((CoverLetter entity, CancellationToken _) => entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.VacancyId.Should().Be(vacancyId);
        result.Content.Should().Be(command.Content);
        result.CreatedAt.Should().NotBe(default(DateTime));
        result.UpdatedAt.Should().NotBe(default(DateTime));
        result.CreatedAt.Should().Be(result.UpdatedAt);

        capturedEntity.Should().NotBeNull();
        capturedEntity!.UserId.Should().Be(userId);
        capturedEntity.VacancyId.Should().Be(vacancyId);
        capturedEntity.Content.Should().Be(command.Content);
        capturedEntity.CreatedAt.Should().NotBe(default(DateTime));
        capturedEntity.UpdatedAt.Should().NotBe(default(DateTime));

        _vacancyRepositoryMock.Verify(
            x => x.GetByIdAsync(vacancyId, It.IsAny<CancellationToken>()),
            Times.Once);
        _coverLetterRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_VacancyNotFound_ThrowsNotFoundException()
    {
        var vacancyId = Guid.NewGuid();

        _vacancyRepositoryMock
            .Setup(x => x.GetByIdAsync(vacancyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Vacancy", vacancyId));

        var act = () => _handler.Handle(
            new CreateCoverLetterCommand
            {
                UserId = Guid.NewGuid(),
                VacancyId = vacancyId,
                Content = new string('a', 50),
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        _coverLetterRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_VacancyOwnedByDifferentUser_ThrowsNotFoundException()
    {
        var requestUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        var vacancy = new Vacancy
        {
            Id = vacancyId,
            UserId = ownerUserId,
            JobSource = new JobSource { Name = "LinkedIn", Url = "https://linkedin.com/jobs" },
        };

        _vacancyRepositoryMock
            .Setup(x => x.GetByIdAsync(vacancyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vacancy);

        var act = () => _handler.Handle(
            new CreateCoverLetterCommand
            {
                UserId = requestUserId,
                VacancyId = vacancyId,
                Content = new string('a', 50),
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        _coverLetterRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UniqueConstraintViolation_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        var vacancy = new Vacancy
        {
            Id = vacancyId,
            UserId = userId,
            JobSource = new JobSource { Name = "LinkedIn", Url = "https://linkedin.com/jobs" },
        };

        _vacancyRepositoryMock
            .Setup(x => x.GetByIdAsync(vacancyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vacancy);

        _coverLetterRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoverLetter entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException(
                "duplicate key value violates unique constraint",
                new Exception("duplicate key value violates unique constraint")));

        var act = () => _handler.Handle(
            new CreateCoverLetterCommand
            {
                UserId = userId,
                VacancyId = vacancyId,
                Content = new string('a', 50),
            },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Message.Should().Contain("already exists");
    }
}