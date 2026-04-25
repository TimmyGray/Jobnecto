using FluentAssertions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using Moq;

namespace JobNecto.Tests.Application.Users;

public class UploadCurrentUserAvatarCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAvatarStorageService> _avatarStorageMock;
    private readonly UploadCurrentUserAvatarCommandHandler _handler;

    public UploadCurrentUserAvatarCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _avatarStorageMock = new Mock<IAvatarStorageService>();

        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);

        _handler = new UploadCurrentUserAvatarCommandHandler(_uowMock.Object, _avatarStorageMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUpload_UpdatesAvatarAndSaves()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Login = "user",
            Email = "user@example.com",
            Password = "hash"
        };

        var command = new UploadCurrentUserAvatarCommand
        {
            UserId = userId,
            FileName = "avatar.png",
            ContentType = "image/png",
            Content = [1, 2, 3]
        };

        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _avatarStorageMock
            .Setup(x => x.UploadUserAvatarAsync(
                userId,
                It.IsAny<Stream>(),
                command.FileName,
                command.ContentType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvatarUploadResult
            {
                SecureUrl = "https://res.cloudinary.com/demo/image/upload/users/avatar.jpg",
                PublicId = $"users/{userId:N}/avatar"
            });
        _userRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Avatar.Should().Be("https://res.cloudinary.com/demo/image/upload/users/avatar.jpg");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
