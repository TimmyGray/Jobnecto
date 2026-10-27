using JobNecto.Application.JobSources;
using JobNecto.Infrastructure.JobSources.Seeding;
using JobNecto.Infrastructure.JobSources.Static;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Infrastructure.JobSources;

/// <summary>
/// Registers the job-source adapter and the vacancy seeding triggers.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Binds <see cref="VacancySeedOptions"/>, registers the demo catalogue as the <see cref="IJobSource"/>,
    /// and adds the startup backfill. Replacing the demo catalogue with a real source means changing
    /// only the <see cref="IJobSource"/> registration.
    /// </summary>
    /// <param name="services">Application DI collection.</param>
    /// <param name="configuration">Configuration holding the <c>Seed:Vacancies</c> section.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddJobSources(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VacancySeedOptions>(configuration.GetSection(VacancySeedOptions.SectionName));
        services.AddSingleton<IJobSource, StaticJobSource>();
        services.AddHostedService<VacancySeedBackfillService>();

        return services;
    }
}
