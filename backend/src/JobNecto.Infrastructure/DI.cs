using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

public static class InfrastructureCollectionExtensions
{
    /// <summary>
    /// Registers EF Core (<see cref="AppDbContext"/>) and infrastructure services.
    /// </summary>
    /// <param name="services">Application DI collection.</param>
    /// <param name="configuration">Must define a usable <c>ConnectionStrings:Postgres</c> value (non-empty, parseable, with a host).</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">When the Postgres connection string is missing, blank, unparsable, or has no host (common misconfiguration).</exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        EnsureValidPostgresConnectionString(connectionString);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                o => o.MigrationsAssembly("JobNecto.Infrastructure")
            )
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Purpose: fail fast at composition time so operators see a clear configuration error instead of an Npgsql error on first query.
    /// Assumes: standard Npgsql keyword connection strings (see <see cref="NpgsqlConnectionStringBuilder"/>).
    /// Contract: returns normally only when <paramref name="connectionString"/> is non-blank and supplies a non-empty host.
    /// Failure modes: <see cref="InvalidOperationException"/> for null/empty, parse errors, or empty host (e.g. template <c>Host=;</c> in appsettings).
    /// </summary>
    private static void EnsureValidPostgresConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Postgres is missing or empty. Set it in configuration, environment variables, or user secrets (see appsettings.Development.json for a local development example)."
            );
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Postgres is not a valid PostgreSQL connection string.",
                ex
            );
        }

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Postgres must include a non-empty Host (current value looks like a template; override with real credentials for the target environment)."
            );
        }
    }
}
