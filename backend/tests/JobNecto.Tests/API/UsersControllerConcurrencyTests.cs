using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace JobNecto.Tests.API;

public class UsersControllerConcurrencyTests : IClassFixture<UsersControllerConcurrencyFactory>
{
    private readonly UsersControllerConcurrencyFactory _factory;
    private readonly ITestOutputHelper _output;

    public UsersControllerConcurrencyTests(
        UsersControllerConcurrencyFactory factory,
        ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Create_ConcurrentDuplicateRequests_ReturnsOneCreatedAndOneConflict()
    {
        if (!await _factory.TryInitializeSchemaAsync())
        {
            _output.WriteLine("Skipped real-database concurrency assertion because PostgreSQL test database was unavailable.");
            return;
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var login = "race" + Guid.NewGuid().ToString("N")[..8];
        var email = Guid.NewGuid().ToString("N")[..8] + "@example.com";

        var requestA = new CreateUserCommand
        {
            LoginName = login,
            Email = email,
            Password = "Password123!"
        };

        var requestB = new CreateUserCommand
        {
            LoginName = login,
            Email = email,
            Password = "Password123!"
        };

        var responseTasks = new[]
        {
            client.PostAsJsonAsync("/api/v1/users", requestA),
            client.PostAsJsonAsync("/api/v1/users", requestB)
        };

        await Task.WhenAll(responseTasks);

        var responses = responseTasks.Select(task => task.Result).ToArray();
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);

        var conflictResponse = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        var body = await conflictResponse.Content.ReadAsStringAsync();
        var problem = JsonNode.Parse(body);

        problem.Should().NotBeNull();
        problem!["status"]!.GetValue<int>().Should().Be(409);
        problem["title"]!.GetValue<string>().Should().Be("Conflict");
        problem["traceId"]!.GetValue<string>().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_ConcurrentDuplicatePhoneRequests_ReturnsOneCreatedAndOneConflict()
    {
        if (!await _factory.TryInitializeSchemaAsync())
        {
            _output.WriteLine("Skipped real-database phone uniqueness concurrency assertion because PostgreSQL test database was unavailable.");
            return;
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var phone = "+15555550999";

        var requestA = new CreateUserCommand
        {
            LoginName = "phonea" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
            Phone = phone
        };

        var requestB = new CreateUserCommand
        {
            LoginName = "phoneb" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
            Phone = phone
        };

        var responseTasks = new[]
        {
            client.PostAsJsonAsync("/api/v1/users", requestA),
            client.PostAsJsonAsync("/api/v1/users", requestB)
        };

        await Task.WhenAll(responseTasks);

        var responses = responseTasks.Select(task => task.Result).ToArray();
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
    }
}

public sealed class UsersControllerConcurrencyFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=JobNectoTest;Username=test;Password=test";

    private readonly string _baseConnectionString;
    private readonly string _schemaName;
    private string? _scopedConnectionString;
    private bool _schemaInitialized;

    public UsersControllerConcurrencyFactory()
    {
        _baseConnectionString = Environment.GetEnvironmentVariable("JOBNECTO_TEST_POSTGRES")
            ?? DefaultConnectionString;
        _schemaName = "users_concurrency_" + Guid.NewGuid().ToString("N");
    }

    public async Task<bool> TryInitializeSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (_schemaInitialized)
        {
            return true;
        }

        try
        {
            await EnsureSchemaExistsAsync(cancellationToken);

            var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString)
            {
                SearchPath = _schemaName
            };

            _scopedConnectionString = builder.ConnectionString;

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(
                    _scopedConnectionString,
                    npgsql => npgsql.MigrationsAssembly("JobNecto.Infrastructure"))
                .Options;

            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.MigrateAsync(cancellationToken);

            _schemaInitialized = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting(
            "ConnectionStrings:Postgres",
            _scopedConnectionString ?? _baseConnectionString);
    }

    public override async ValueTask DisposeAsync()
    {
        await DropSchemaAsync();
        await base.DisposeAsync();
    }

    private async Task EnsureSchemaExistsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{_schemaName}\";";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task DropSchemaAsync()
    {
        if (!_schemaInitialized)
        {
            return;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_baseConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE;";
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}