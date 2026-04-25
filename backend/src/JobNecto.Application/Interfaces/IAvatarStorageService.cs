namespace JobNecto.Application.Interfaces;

/// <summary>
/// Abstraction for uploading and deleting user avatar assets in external media storage.
/// </summary>
public interface IAvatarStorageService
{
    /// <summary>
    /// Uploads or replaces a user's avatar and returns metadata required by the application.
    /// </summary>
    /// <param name="userId">User identifier used to scope avatar storage.</param>
    /// <param name="content">Avatar binary content stream.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="contentType">Uploaded content type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Uploaded avatar metadata.</returns>
    Task<AvatarUploadResult> UploadUserAvatarAsync(
        Guid userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct);

    /// <summary>
    /// Deletes a user's avatar asset from media storage.
    /// </summary>
    /// <param name="userId">User identifier used to scope avatar storage.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteUserAvatarAsync(Guid userId, CancellationToken ct);
}

/// <summary>
/// Avatar upload metadata returned from media storage.
/// </summary>
public sealed class AvatarUploadResult
{
    /// <summary>
    /// Secure delivery URL for the uploaded avatar.
    /// </summary>
    public required string SecureUrl { get; init; }

    /// <summary>
    /// Provider public identifier for the uploaded avatar.
    /// </summary>
    public required string PublicId { get; init; }
}
