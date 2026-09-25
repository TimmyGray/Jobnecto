namespace JobNecto.API.Contracts.Auth;

/// <summary>
/// Response contract for a successful sign-in.
/// </summary>
public sealed class SignInResponse
{
    public Guid Id { get; init; }
    public string LoginName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? Phone { get; init; }
    public string? Location { get; init; }
    public string? About { get; init; }
    public string? Avatar { get; init; }

    /// <summary>
    /// Newly issued JWT access token for non-browser bearer transport.
    /// Empty for browser cookie transport.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;
}
