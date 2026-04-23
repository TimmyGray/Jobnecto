using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Query for retrieving the current authenticated user's profile.
/// </summary>
public class GetCurrentUserQuery : IRequest<GetCurrentUserResult>
{
    /// <summary>
    /// Authenticated user identifier extracted from JWT claims.
    /// </summary>
    public Guid UserId { get; set; }
}

/// <summary>
/// Current-user profile response returned by GET /api/v1/users/me.
/// </summary>
public class GetCurrentUserResult
{
    public Guid Id { get; set; }
    public string LoginName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? About { get; set; }
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
