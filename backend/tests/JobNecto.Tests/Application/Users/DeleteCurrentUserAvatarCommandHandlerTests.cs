using FluentAssertions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.Users;

public class DeleteCurrentUserAvatarCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAvatarStorageService> _avatarStorageMock;
    private readonly DeleteCurrentUserAvatarCommandHandler _handler;

    public DeleteCurrentUserAvatarCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _avatarStorageMock = new Mock<IAvatarStorageService>();

        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);

        _handler = new DeleteCurrentUserAvatarCommandHandler(_uowMock.Object, _avatarStorageMock.Object);
    }

    [Fact]
    public async Task Handle_UserHasAvatar_DeletesAssetAndClearsReference()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Login = "user",
            Email = "user@example.com",
            Password = "hash",
            Avatar = "https://res.cloudinary.com/demo/image/upload/users/avatar.jpg"
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var result = await _handler.Handle(new DeleteCurrentUserAvatarCommand { UserId = userId }, CancellationToken.None);

        result.Avatar.Should().BeNull();
        _avatarStorageMock.Verify(x => x.DeleteUserAvatarAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserHasNoAvatar_DoesNotCallDelete()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Login = "user",
            Email = "user@example.com",
            Password = "hash",
            Avatar = null
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        await _handler.Handle(new DeleteCurrentUserAvatarCommand { UserId = userId }, CancellationToken.None);

        _avatarStorageMock.Verify(x => x.DeleteUserAvatarAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
