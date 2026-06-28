using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.API.Infrastructure.Cors;

/// <summary>
/// Registers the CORS policies used by the API.
/// </summary>
public static class CorsServiceExtensions
{
    /// <summary>
    /// Name of the CORS policy applied to front-end SPA origins.
    /// Use this constant with <see cref="Microsoft.AspNetCore.Builder.CorsMiddlewareExtensions.UseCors(Microsoft.AspNetCore.Builder.IApplicationBuilder, string)"/>
    /// to avoid duplicating the policy name as a magic string.
    /// </summary>
    public const string FrontendPolicy = "Frontend";

    /// <summary>
    /// Adds the <c>Frontend</c> CORS policy.
    /// Origins are read from <c>Cors:AllowedOrigins</c> in configuration.
    /// In Development, <c>appsettings.Development.json</c> lists the Vite dev-server origins.
    /// In Production, supply origins via environment variables using the double-underscore convention:
    /// <c>Cors__AllowedOrigins__0=https://app.example.com</c>.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">Application configuration used to resolve <c>Cors:AllowedOrigins</c>.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCorsPolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var allowedOrigins = Array.FindAll(
            Array.ConvertAll(configuredOrigins, origin => origin?.Trim() ?? string.Empty),
            origin => !string.IsNullOrWhiteSpace(origin));

        services.AddCors(options =>
            options.AddPolicy(FrontendPolicy, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }

                policy
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                    .WithHeaders("Authorization", "Content-Type", "Accept")
                    .AllowCredentials();
            })
        );

        return services;
    }
}
