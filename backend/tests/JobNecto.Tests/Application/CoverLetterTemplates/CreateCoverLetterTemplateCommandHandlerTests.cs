using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.CoverLetterTemplates;

public class CreateCoverLetterTemplateCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMutableRepository<CoverLetterTemplate>> _repositoryMock;
    private readonly CreateCoverLetterTemplateCommandHandler _handler;

    public CreateCoverLetterTemplateCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _repositoryMock = new Mock<IMutableRepository<CoverLetterTemplate>>();

        _unitOfWorkMock
            .Setup(x => x.CoverLetterTemplateRepository)
            .Returns(_repositoryMock.Object);

        _handler = new CreateCoverLetterTemplateCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsExactlyOnceAndReturnsResultWithNonDefaultTimestamps()
    {
        var command = new CreateCoverLetterTemplateCommand
        {
            UserId = Guid.NewGuid(),
            Name = "My Cover Letter",
            Content = new string('a', 50),
        };

        CoverLetterTemplate? capturedEntity = null;
        _repositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()))
            .Callback<CoverLetterTemplate, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((CoverLetterTemplate entity, CancellationToken _) => entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(command.UserId);
        result.Name.Should().Be(command.Name);
        result.Content.Should().Be(command.Content);
        result.CreatedAt.Should().NotBe(default(DateTime));
        result.UpdatedAt.Should().NotBe(default(DateTime));
        result.CreatedAt.Should().Be(result.UpdatedAt);

        capturedEntity.Should().NotBeNull();
        capturedEntity!.UserId.Should().Be(command.UserId);
        capturedEntity.Name.Should().Be(command.Name);
        capturedEntity.Content.Should().Be(command.Content);
        capturedEntity.CreatedAt.Should().NotBe(default(DateTime));
        capturedEntity.UpdatedAt.Should().NotBe(default(DateTime));

        _repositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<CoverLetterTemplate>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
