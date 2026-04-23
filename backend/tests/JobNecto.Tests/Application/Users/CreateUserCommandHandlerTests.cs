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
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);
        _handler = new CreateUserCommandHandler(_uowMock.Object, _passwordHasherMock.Object);
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

        var createdUser = default(User);
        const string hashedPassword = "hashed-password";

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns(hashedPassword);

        _userRepoMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync(command.LoginName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => createdUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.LoginName.Should().Be(command.LoginName);
        createdUser.Should().NotBeNull();
        createdUser!.Password.Should().Be(hashedPassword);
        createdUser.Password.Should().NotBe(command.Password);
        _passwordHasherMock.Verify(x => x.HashPassword(command.Password), Times.Once);
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
        _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
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
        _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CreateUserResult_DoesNotExposePasswordField()
    {
        var passwordProperty = typeof(CreateUserResult).GetProperty("Password");

        passwordProperty.Should().BeNull();
    }
}