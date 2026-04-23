using Microsoft.OpenApi;

namespace JobNecto.API.Infrastructure;

/// <summary>
/// OpenAPI configuration extensions for documenting authentication transport policy.
/// </summary>
public static class OpenApiCollectionExtensions
{
    /// <summary>
    /// Registers OpenAPI document generation and adds explicit security scheme
    /// documentation for cookie and bearer authentication transports.
    /// </summary>
    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["CookieAuth"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Cookie,
                        Name = CookieAuthService.CookieName,
                        Description =
                            "Browser transport. JWT is issued as an HTTP-only cookie. " +
                            "Use POST /api/v1/users/token/refresh to renew while still authenticated."
                    },
                    ["BearerAuth"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description =
                            "Non-browser transport. Send Authorization: Bearer <token>. " +
                            "Renew with POST /api/v1/users/token/refresh before expiry; " +
                            "if already expired and refresh is rejected, re-authenticate."
                    }
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }
}