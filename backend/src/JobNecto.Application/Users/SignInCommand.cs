using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Command to sign in a returning user with an email or login identifier and a password.
/// </summary>
public class SignInCommand : IRequest<SignInResult>
{
    /// <summary>
    /// The user-supplied identifier: an email address or a login name.
    /// </summary>
    public string Identifier { get; set; } = null!;

    /// <summary>
    /// Plain-text password supplied by the client; never returned in API responses.
    /// </summary>
    public string Password { get; set; } = null!;
}

/// <summary>
/// Result returned after a successful sign-in. Contains the authenticated user's representation
/// (without password). JWT token issuance and cookie transport are handled by the controller.
/// </summary>
public class SignInResult
{
    public Guid Id { get; set; }
    public string LoginName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? About { get; set; }
    public string? Avatar { get; set; }
}
