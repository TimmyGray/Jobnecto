using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Users;

/// <summary>
/// Command for deleting current-user avatar reference and media asset.
/// </summary>
public class DeleteCurrentUserAvatarCommand : IRequest<GetCurrentUserResult>
{
    /// <summary>
    /// Authenticated user identifier set by API layer.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }
}
