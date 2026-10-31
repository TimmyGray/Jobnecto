using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace JobNecto.Tests.API;

/// <summary>
/// Configuration pins shared by the API test hosts.
/// </summary>
internal static class TestHostConfigurationExtensions
{
    internal const string SeedVacanciesEnabledKey = "Seed:Vacancies:Enabled";

    /// <summary>
    /// Forces demo vacancy seeding off. Registered as an app-configuration source, which
    /// <c>WebApplicationFactory</c> applies after Program.cs's own sources (including
    /// <c>appsettings.Local.json</c>), so neither a Development appsettings file nor a developer's local
    /// file can turn the startup backfill on against whatever database the host points at.
    /// Call it after any callback that clears configuration sources.
    /// </summary>
    internal static IWebHostBuilder DisableVacancySeeding(this IWebHostBuilder builder) =>
        builder.SetVacancySeeding(enabled: false);

    /// <summary>Pins <c>Seed:Vacancies:Enabled</c>; the last call on a builder wins.</summary>
    internal static IWebHostBuilder SetVacancySeeding(this IWebHostBuilder builder, bool enabled) =>
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [SeedVacanciesEnabledKey] = enabled ? "true" : "false",
            }));
}
