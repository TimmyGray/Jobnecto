namespace JobNecto.API.Contracts.Auth;

/// <summary>
/// Response contract for issuing a renewed access token.
/// </summary>
public sealed class RefreshAccessTokenResult
{
    /// <summary>
    /// Newly issued JWT access token for non-browser bearer transport.
    /// Empty for browser cookie transport.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Token type for the access token field.
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Renewal policy for API clients.
    /// </summary>
    public string RenewalPolicy { get; init; } =
        "Call POST /api/v1/users/token/refresh while currently authenticated. " +
        "If refresh fails due to expired or invalid credentials, re-authenticate.";
}