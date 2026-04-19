namespace JobNecto.Application.Users;

using MediatR;
using System;

/// <summary>
/// Command to create a new user account.
/// Public API contract: loginName, email, password, optional phone, location, about, avatar.
/// </summary>
public class CreateUserCommand : IRequest<CreateUserResult>
{
    /// <summary>
    /// The public login name. Maps to `User.Login`.
    /// </summary>
    public string LoginName { get; set; } = null!;

    /// <summary>
    /// User email address. Maps to `User.Email`.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Plain-text password. (Password hardening is out of scope for this story.)
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Optional phone in E.164 format. Maps to `User.Phone`.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Optional location serialized as the `Location` enum string. Maps to `User.Location`.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Optional about text. Maps to `User.AboutMe`.
    /// </summary>
    public string? About { get; set; }

    /// <summary>
    /// Optional avatar URL or identifier.
    /// </summary>
    public string? Avatar { get; set; }
}

/// <summary>
/// Result returned after successful creation. Contains the created user representation (without password).
/// JWT token is set in an HTTP-Only secure cookie (Set-Cookie header) with SameSite=Strict and Secure flag,
/// not in the response body, for better XSS/CSRF protection.
/// </summary>
public class CreateUserResult
{
    public Guid Id { get; set; }
    public string LoginName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? About { get; set; }
    public string? Avatar { get; set; }
}
