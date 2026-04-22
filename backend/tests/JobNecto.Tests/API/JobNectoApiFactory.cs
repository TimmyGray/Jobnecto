using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using JobNecto.Infrastructure.Persistance;

namespace JobNecto.Tests.API;

/// <summary>
/// Custom WebApplicationFactory for API integration tests.
/// Replaces Npgsql with EF Core InMemory database using ConfigureTestServices,
/// which runs AFTER Program.cs service registration so overrides actually stick.
/// </summary>
public class JobNectoApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // ConfigureTestServices (from TestHost) runs after Program.cs, so our override wins over AddInfrastructure()
        builder.ConfigureTestServices(services =>
        {
            // EF Core 9+ registers BOTH DbContextOptions<T> AND IDbContextOptionsConfiguration<T>.
            // Removing only DbContextOptions<T> leaves the Npgsql IDbContextOptionsConfiguration behind,
            // which merges with the new InMemory one and causes EF Core 10's dual-provider exception.
            // We must remove both descriptors.
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                 d.ServiceType.GetGenericArguments().FirstOrDefault() == typeof(AppDbContext))
            ).ToList();

            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Replace with isolated InMemory database
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("JobNectoTest_" + Guid.NewGuid()));
        });
    }
}