using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Users;

/// <summary>
/// Command for uploading or replacing current-user avatar.
/// </summary>
public class UploadCurrentUserAvatarCommand : IRequest<GetCurrentUserResult>
{
    /// <summary>
    /// Authenticated user identifier set by API layer.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Uploaded file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Uploaded file content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Uploaded file payload.
    /// </summary>
    public byte[] Content { get; set; } = [];
}
