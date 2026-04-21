namespace JobNecto.API.Infrastructure;

/// <summary>
/// Service to handle setting the authentication cookie in the HTTP response.
/// This encapsulates the security policies and cookie configuration.
/// </summary>
public interface ICookieAuthService
{
    /// <summary>
    /// Sets the authentication token in an HTTP-Only secure cookie.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="token">The JWT token.</param>
    void SetAuthCookie(HttpResponse response, string token);
}

/// <inheritdoc />
public class CookieAuthService : ICookieAuthService
{
    private readonly IHostEnvironment _env;
    private readonly int _expirationMinutes;

    public CookieAuthService(IHostEnvironment env, IConfiguration configuration)
    {
        _env = env;
        _expirationMinutes = int.TryParse(configuration["JwtSettings:ExpirationMinutes"], out var exp) ? exp : 60;
    }

    /// <inheritdoc />
    public void SetAuthCookie(HttpResponse response, string token)
    {
        response.Cookies.Append("auth-token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_expirationMinutes)
        });
    }
}