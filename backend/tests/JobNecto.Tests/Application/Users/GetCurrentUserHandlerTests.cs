using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using Moq;

namespace JobNecto.Tests.Application.Users;

public class GetCurrentUserHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();

        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);

        _handler = new GetCurrentUserQueryHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_UserMissing_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("User", userId));

        var act = () => _handler.Handle(new GetCurrentUserQuery { UserId = userId }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserFound_ReturnsMappedProfileWithoutPasswordExposure()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = userId,
            Login = "timmy_dev",
            Email = "timmy@example.com",
            Password = "secret-hash",
            Phone = "+12345678901",
            Location = Location.Germany,
            AboutMe = "About profile",
            Avatar = "https://example.com/avatar.png",
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new GetCurrentUserQuery { UserId = userId }, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.LoginName.Should().Be(user.Login);
        result.Email.Should().Be(user.Email);
        result.Phone.Should().Be(user.Phone);
        result.Location.Should().Be(user.Location!.ToString());
        result.About.Should().Be(user.AboutMe);
        result.Avatar.Should().Be(user.Avatar);
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.UpdatedAt.Should().Be(user.UpdatedAt);

        typeof(GetCurrentUserResult).GetProperty("Password").Should().BeNull();
        typeof(GetCurrentUserResult).GetProperty("PasswordHash").Should().BeNull();
    }
}
