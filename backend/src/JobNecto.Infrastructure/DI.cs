using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                o => o.MigrationsAssembly("JobNecto.Infrastructure")
            )
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
