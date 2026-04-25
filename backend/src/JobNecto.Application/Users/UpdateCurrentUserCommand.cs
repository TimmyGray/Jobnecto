using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Users;

/// <summary>
/// Command for updating the current authenticated user's profile fields.
/// </summary>
public class UpdateCurrentUserCommand : IRequest<GetCurrentUserResult>
{
    /// <summary>
    /// Authenticated user identifier set by the API layer.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// New login name.
    /// </summary>
    public string? LoginName { get; set; }

    /// <summary>
    /// New email value.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// New phone value in E.164 format.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// New location string mapped to <c>Location</c> enum.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// New about text.
    /// </summary>
    public string? About { get; set; }

    /// <summary>
    /// Avatar reference (https URL or storage key).
    /// </summary>
    public string? Avatar { get; set; }
}
