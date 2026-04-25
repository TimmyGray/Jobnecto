using Microsoft.AspNetCore.Http;

namespace JobNecto.API.Contracts.Users;

/// <summary>
/// Multipart form contract for avatar upload/update endpoints.
/// </summary>
public class UploadAvatarRequest
{
    /// <summary>
    /// Avatar image file.
    /// </summary>
    public IFormFile? Avatar { get; set; }
}
