using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);
        _handler = new CreateUserCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesUser()
    {
        var command = new CreateUserCommand
        {
            LoginName = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };
        _userRepoMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync(command.LoginName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.LoginName.Should().Be(command.LoginName);
        _userRepoMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        var command = new CreateUserCommand { Email = "duplicate@example.com", LoginName = "user" };
        _userRepoMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = command.Email, Login = command.LoginName, Password = "pwd" });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"User with email '{command.Email}' already exists.");
    }

    [Fact]
    public async Task Handle_DuplicateLogin_ThrowsConflictException()
    {
        var command = new CreateUserCommand { Email = "user@example.com", LoginName = "duplicate" };
        _userRepoMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync(command.LoginName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Login = command.LoginName, Email = "other@ex.com", Password = "pwd" });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"User with login '{command.LoginName}' already exists.");
    }
}