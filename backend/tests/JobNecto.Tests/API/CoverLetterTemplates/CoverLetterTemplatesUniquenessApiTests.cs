using System.Net;
using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace JobNecto.Tests.API.CoverLetterTemplates;

public class CoverLetterTemplatesUniquenessApiTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private CoverLetterTemplatesPostgresFactory? _factory;

    public CoverLetterTemplatesUniquenessApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _factory = new CoverLetterTemplatesPostgresFactory(_output);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_factory != null)
            await _factory.DisposeAsync();
    }

    private static string ValidContent() => new string('a', 50);

    private static CreateUserCommand NewUserCommand(string prefix = "clt_u") =>
        new()
        {
            LoginName = prefix + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
        };

    [Fact]
    public async Task Create_DuplicateName_SameUser_Returns409()
    {
        if (!await _factory!.TryInitializeSchemaAsync())
        {
            _output.WriteLine("Skipped: PostgreSQL test database was unavailable.");
            return;
        }

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var authCookie = await CoverLetterTemplatesApiTests.CreateUserAndGetCookieHelperAsync(client, NewUserCommand());

        var first = await CoverLetterTemplatesApiTests.PostTemplateAsync(client, authCookie, new
        {
            name = "Duplicate Name",
            content = ValidContent(),
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await CoverLetterTemplatesApiTests.PostTemplateAsync(client, authCookie, new
        {
            name = "Duplicate Name",
            content = ValidContent(),
        });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_SameName_DifferentUsers_Returns201BothTimes()
    {
        if (!await _factory!.TryInitializeSchemaAsync())
        {
            _output.WriteLine("Skipped: PostgreSQL test database was unavailable.");
            return;
        }

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var cookieA = await CoverLetterTemplatesApiTests.CreateUserAndGetCookieHelperAsync(client, NewUserCommand("clt_a_"));
        var cookieB = await CoverLetterTemplatesApiTests.CreateUserAndGetCookieHelperAsync(client, NewUserCommand("clt_b_"));

        var responseA = await CoverLetterTemplatesApiTests.PostTemplateAsync(client, cookieA, new
        {
            name = "Shared Name",
            content = ValidContent(),
        });
        var responseB = await CoverLetterTemplatesApiTests.PostTemplateAsync(client, cookieB, new
        {
            name = "Shared Name",
            content = ValidContent(),
        });

        responseA.StatusCode.Should().Be(HttpStatusCode.Created);
        responseB.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ConcurrentDuplicateName_AtLeastOneReturns409()
    {
        if (!await _factory!.TryInitializeSchemaAsync())
        {
            _output.WriteLine("Skipped: PostgreSQL test database was unavailable.");
            return;
        }

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var authCookie = await CoverLetterTemplatesApiTests.CreateUserAndGetCookieHelperAsync(client, NewUserCommand());

        var task1 = CoverLetterTemplatesApiTests.PostTemplateAsync(client, authCookie, new { name = "Same Name", content = ValidContent() });
        var task2 = CoverLetterTemplatesApiTests.PostTemplateAsync(client, authCookie, new { name = "Same Name", content = ValidContent() });
        var results = await Task.WhenAll(task1, task2);

        results.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        results.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
    }
}

public sealed class CoverLetterTemplatesPostgresFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=JobNectoTest;Username=test;Password=test";

    private readonly string _baseConnectionString;
    private readonly string _schemaName;
    private string? _scopedConnectionString;
    private bool _schemaInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ITestOutputHelper? _output;

    public CoverLetterTemplatesPostgresFactory(ITestOutputHelper? output = null)
    {
        _output = output;
        _baseConnectionString = Environment.GetEnvironmentVariable("JOBNECTO_TEST_POSTGRES")
            ?? DefaultConnectionString;
        _schemaName = "clt_uniqueness_" + Guid.NewGuid().ToString("N");
    }

    public async Task<bool> TryInitializeSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (_schemaInitialized)
            return true;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_schemaInitialized)
                return true;

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
        catch (Exception ex)
        {
            _output?.WriteLine($"Schema initialization failed: {ex.Message}");
            return false;
        }
        finally
        {
            _initLock.Release();
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
            return;

        try
        {
            await using var connection = new NpgsqlConnection(_baseConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE;";
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _output?.WriteLine($"Schema cleanup failed: {ex.Message}");
        }
    }
}
