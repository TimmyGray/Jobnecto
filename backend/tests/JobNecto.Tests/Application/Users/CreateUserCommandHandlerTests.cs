using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using MediatR;
using Moq;

namespace JobNecto.Tests.Application.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _publisherMock = new Mock<IPublisher>();
        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);
        _handler = new CreateUserCommandHandler(_uowMock.Object, _passwordHasherMock.Object, _publisherMock.Object);
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
    public async Task Handle_DuplicatePhone_ThrowsConflictException()
    {
        var command = new CreateUserCommand
        {
            Email = "user@example.com",
            LoginName = "newuser",
            Password = "Password123!",
            Phone = "+15555550123"
        };

        _userRepoMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync(command.LoginName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByPhoneAsync(command.Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Login = "other", Email = "other@example.com", Password = "pwd", Phone = command.Phone });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"User with phone '{command.Phone}' already exists.");
        _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_PublishesUserRegisteredOnceAfterSave()
    {
        var command = new CreateUserCommand
        {
            LoginName = "newcomer",
            Email = "newcomer@example.com",
            Password = "Password123!"
        };
        var calls = new List<string>();
        User? createdUser = null;

        _passwordHasherMock.Setup(x => x.HashPassword(command.Password)).Returns("hashed");
        _userRepoMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync(command.LoginName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => createdUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("save"))
            .ReturnsAsync(1);
        _publisherMock
            .Setup(x => x.Publish(It.IsAny<UserRegisteredNotification>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("publish"))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        calls.Should().Equal("save", "publish");
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<UserRegisteredNotification>(n => n.UserId == createdUser!.Id && n.UserId == result.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("login")]
    [InlineData("phone")]
    public async Task Handle_Conflict_DoesNotPublishUserRegistered(string conflictOn)
    {
        var command = new CreateUserCommand
        {
            Email = "taken@example.com",
            LoginName = "taken",
            Password = "Password123!",
            Phone = "+15555550199"
        };
        var existing = new User { Login = "other", Email = "other@example.com", Password = "pwd" };

        _userRepoMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictOn == "email" ? existing : null);
        _userRepoMock.Setup(x => x.GetByLoginAsync(command.LoginName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictOn == "login" ? existing : null);
        _userRepoMock.Setup(x => x.GetByPhoneAsync(command.Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictOn == "phone" ? existing : null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<UserRegisteredNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void CreateUserResult_DoesNotExposePasswordField()
    {
        var passwordProperty = typeof(CreateUserResult).GetProperty("Password");

        passwordProperty.Should().BeNull();
    }
}