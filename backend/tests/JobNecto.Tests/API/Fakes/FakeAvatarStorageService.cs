using JobNecto.Application.Interfaces;

namespace JobNecto.Tests.API.Fakes;

/// <summary>
/// In-memory avatar storage fake for API integration tests.
/// </summary>
public sealed class FakeAvatarStorageService : IAvatarStorageService
{
    private readonly Dictionary<Guid, string> _avatars = new();

    /// <inheritdoc />
    public Task<AvatarUploadResult> UploadUserAvatarAsync(
        Guid userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        var secureUrl = $"https://cdn.test.local/avatars/{userId:N}/{Guid.NewGuid():N}.jpg";
        _avatars[userId] = secureUrl;

        return Task.FromResult(new AvatarUploadResult
        {
            SecureUrl = secureUrl,
            PublicId = $"users/{userId:N}/avatar"
        });
    }

    /// <inheritdoc />
    public Task DeleteUserAvatarAsync(Guid userId, CancellationToken ct)
    {
        _avatars.Remove(userId);
        return Task.CompletedTask;
    }
}
