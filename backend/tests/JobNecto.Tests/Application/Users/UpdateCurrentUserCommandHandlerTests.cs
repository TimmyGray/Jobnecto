using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using Moq;

namespace JobNecto.Tests.Application.Users;

public class UpdateCurrentUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly UpdateCurrentUserCommandHandler _handler;

    public UpdateCurrentUserCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();

        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);

        _handler = new UpdateCurrentUserCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidPartialUpdate_UpdatesOnlyProvidedFields()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Login = "old_login",
            Email = "old@example.com",
            Password = "hash",
            Phone = "+15555550100",
            Location = Location.Germany,
            AboutMe = "old about",
            Avatar = "https://old.example.com/avatar.jpg",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var command = new UpdateCurrentUserCommand
        {
            UserId = userId,
            Email = "new@example.com",
            About = "new about"
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock
            .Setup(x => x.GetByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Email.Should().Be("new@example.com");
        result.About.Should().Be("new about");
        result.LoginName.Should().Be("old_login");
        result.Phone.Should().Be("+15555550100");
        result.Avatar.Should().Be("https://old.example.com/avatar.jpg");

        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateLogin_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Login = "current_login",
            Email = "current@example.com",
            Password = "hash"
        };

        var command = new UpdateCurrentUserCommand
        {
            UserId = userId,
            LoginName = "duplicate_login"
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock
            .Setup(x => x.GetByLoginAsync("duplicate_login", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Login = "duplicate_login",
                Email = "other@example.com",
                Password = "hash"
            });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*login*");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Login = "current_login",
            Email = "current@example.com",
            Password = "hash"
        };

        var command = new UpdateCurrentUserCommand
        {
            UserId = userId,
            Email = "duplicate@example.com"
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock
            .Setup(x => x.GetByEmailAsync("duplicate@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Login = "other",
                Email = "duplicate@example.com",
                Password = "hash"
            });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*email*");
    }

    [Fact]
    public async Task Handle_DuplicatePhone_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Login = "current_login",
            Email = "current@example.com",
            Phone = "+15555550100",
            Password = "hash"
        };

        var command = new UpdateCurrentUserCommand
        {
            UserId = userId,
            Phone = "+15555550222"
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock
            .Setup(x => x.GetByPhoneAsync("+15555550222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Login = "other",
                Email = "other@example.com",
                Phone = "+15555550222",
                Password = "hash"
            });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*phone*");
    }
}
