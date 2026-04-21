using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace JobNecto.API.Infrastructure;

/// <summary>
/// Extension methods for configuring JWT authentication services.
/// </summary>
public static class AuthenticationCollectionExtensions
{
    /// <summary>
    /// Adds JWT Bearer token authentication to the service collection.
    /// Configures token validation with issuer, audience, and signing key from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration containing JwtSettings.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if required JWT settings are missing or invalid.</exception>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        EnsureValidJwtConfiguration(secretKey, issuer, audience);

        var key = Encoding.UTF8.GetBytes(secretKey!);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Validates that required JWT configuration values are present and valid.
    /// </summary>
    /// <param name="secretKey">The JWT signing secret key.</param>
    /// <param name="issuer">The JWT issuer claim value.</param>
    /// <param name="audience">The JWT audience claim value.</param>
    /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
    private static void EnsureValidJwtConfiguration(string? secretKey, string? issuer, string? audience)
    {
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey is not configured. " +
                "Set JwtSettings:SecretKey in appsettings.json, appsettings.{Environment}.json, or via environment variable JwtSettings__SecretKey. " +
                "The secret must be at least 32 characters for HS256 (256-bit key).");
        }

        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"JWT SecretKey must be at least 32 characters for HS256 signing (256-bit key). Current length: {secretKey.Length}.");
        }

        if (string.IsNullOrEmpty(issuer))
        {
            throw new InvalidOperationException(
                "JWT Issuer is not configured. " +
                "Set JwtSettings:Issuer in appsettings.json or via environment variable JwtSettings__Issuer.");
        }

        if (string.IsNullOrEmpty(audience))
        {
            throw new InvalidOperationException(
                "JWT Audience is not configured. " +
                "Set JwtSettings:Audience in appsettings.json or via environment variable JwtSettings__Audience.");
        }
    }
}
