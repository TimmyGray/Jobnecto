using FluentAssertions;
using JobNecto.API;
using JobNecto.Application.Interfaces;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobNecto.Tests.API;

/// <summary>
/// Verifies issue #16: infrastructure is wired so the host builds and <see cref="AppDbContext"/> resolves from DI (Development settings).
/// </summary>
public sealed class InfrastructureHostingTests
{
    /// <summary>
    /// Purpose: regression guard that the API host composes with Infrastructure DI.
    /// Assumes: default API configuration includes a Postgres connection string (Development appsettings).
    /// Contract: scoped <see cref="AppDbContext"/> resolves (issue #16); no DB round-trip required.
    /// Side effects: builds the test host only.
    /// Failure modes: <see cref="IUnitOfWork"/> is not resolved here because <c>UnitOfWork.DisposeAsync</c> is not implemented yet (#14).
    /// </summary>
    [Fact]
    public async Task WebApplicationFactory_in_Development_resolves_AppDbContext()
    {
        await using var factory = new WebApplicationFactory<ApiAssemblyMarker>().WithWebHostBuilder(
            builder => builder.UseEnvironment("Test")
        );

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Should().NotBeNull();
        db.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    /// <summary>
    /// Purpose: prove <see cref="InfrastructureCollectionExtensions.AddInfrastructure"/> wires Npgsql <see cref="AppDbContext"/> when <c>ConnectionStrings:Postgres</c> is set.
    /// Assumes: in-memory configuration uses the same key shape as appsettings.
    /// Contract: scoped <see cref="AppDbContext"/> resolves with Npgsql provider metadata.
    /// Side effects: builds and disposes a <see cref="ServiceProvider"/>.
    /// </summary>
    [Fact]
    public void AddInfrastructure_with_Postgres_connection_registers_scoped_AppDbContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] =
                        "Host=localhost;Port=5432;Database=JobNecto;Username=test;Password=test",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    /// <summary>
    /// Purpose: document that UoW is registered alongside EF without constructing <see cref="UnitOfWork"/> (avoids <c>DisposeAsync</c> until #14).
    /// Assumes: <see cref="InfrastructureCollectionExtensions.AddInfrastructure"/> unchanged.
    /// Contract: exactly one scoped <see cref="IUnitOfWork"/> → <see cref="UnitOfWork"/> registration exists.
    /// Side effects: none (descriptor inspection only).
    /// </summary>
    [Fact]
    public void AddInfrastructure_registers_scoped_IUnitOfWork()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] =
                        "Host=localhost;Port=5432;Database=JobNecto;Username=test;Password=test",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddInfrastructure(configuration);

        services
            .Should()
            .ContainSingle(d =>
                d.ServiceType == typeof(IUnitOfWork)
                && d.ImplementationType == typeof(UnitOfWork)
                && d.Lifetime == ServiceLifetime.Scoped
            );
    }

    /// <summary>
    /// Purpose: guard <c>EnsureValidPostgresConnectionString</c> when the key is absent.
    /// Contract: <see cref="InvalidOperationException"/> mentions Postgres configuration.
    /// </summary>
    [Fact]
    public void AddInfrastructure_throws_when_Postgres_connection_string_is_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddInfrastructure(configuration);

        act.Should()
            .Throw<InvalidOperationException>()
            .Which.Message.Should()
            .Contain("Postgres")
            .And.Contain("missing")
            .And.Contain("empty");
    }

    /// <summary>
    /// Purpose: reject blank connection string values (whitespace only).
    /// Contract: <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void AddInfrastructure_throws_when_Postgres_connection_string_is_whitespace()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = "   " })
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Purpose: catch template appsettings where <c>Host=;</c> leaves host empty after parse.
    /// Contract: message explains host requirement.
    /// </summary>
    [Fact]
    public void AddInfrastructure_throws_when_Postgres_host_is_empty()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] =
                        "Host=;Port=5432;Database=JobNecto;Username=x;Password=y;Pooling=true;",
                }
            )
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddInfrastructure(configuration);

        act.Should()
            .Throw<InvalidOperationException>()
            .Which.Message.Should()
            .Contain("Host")
            .And.Contain("non-empty");
    }

    /// <summary>
    /// Purpose: cover the "Cloudinary is configured" branch so the startup warning is skipped.
    /// Contract: registration completes and <see cref="IAvatarStorageService"/> is registered.
    /// </summary>
    [Fact]
    public void AddInfrastructure_with_Cloudinary_configured_skips_warning()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] =
                        "Host=localhost;Port=5432;Database=JobNecto;Username=test;Password=test",
                    ["Cloudinary:CloudName"] = "demo",
                    ["Cloudinary:ApiKey"] = "key",
                    ["Cloudinary:ApiSecret"] = "secret",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        var act = () => services.AddInfrastructure(configuration);

        act.Should().NotThrow();
        services.Should().Contain(d => d.ServiceType == typeof(IAvatarStorageService));
    }

    /// <summary>
    /// Purpose: cover the warning fallback when no <see cref="ILoggerFactory"/> is registered.
    /// Contract: registration still completes (warning written to console instead of the log).
    /// </summary>
    [Fact]
    public void AddInfrastructure_without_logging_and_Cloudinary_does_not_throw()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] =
                        "Host=localhost;Port=5432;Database=JobNecto;Username=test;Password=test",
                }
            )
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddInfrastructure(configuration);

        act.Should().NotThrow();
    }
}
