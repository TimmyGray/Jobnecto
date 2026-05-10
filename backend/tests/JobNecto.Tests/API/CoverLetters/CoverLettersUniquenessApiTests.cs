using System.Net;
using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace JobNecto.Tests.API.CoverLetters;

public class CoverLettersUniquenessApiTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private CoverLettersPostgresFactory? _factory;

    public CoverLettersUniquenessApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _factory = new CoverLettersPostgresFactory(_output);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_factory != null)
            await _factory.DisposeAsync();
    }

    private static CreateUserCommand NewUserCommand(string prefix = "clu_") =>
        new()
        {
            LoginName = prefix + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
        };

    private async Task<bool> EnsureDatabaseAvailableAsync()
    {
        var isDatabaseReady = await _factory!.TryInitializeSchemaAsync();
        if (isDatabaseReady)
            return true;

        const string message = "PostgreSQL test database was unavailable for cover letter uniqueness assertions.";
        _output.WriteLine("Skipped: " + message);

        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
            isDatabaseReady.Should().BeTrue(message);

        return false;
    }

    [Fact]
    public async Task Create_DuplicateVacancyForSameUser_Returns409()
    {
        if (!await EnsureDatabaseAvailableAsync())
            return;

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (authCookie, userId) = await CoverLettersApiTests.CreateUserAndGetCookieAsync(client);
        var vacancyId = await CoverLettersApiTests.SeedVacancyAsync(_factory, userId);

        var first = await CoverLettersApiTests.PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId,
            content = new string('a', 50),
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await CoverLettersApiTests.PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId,
            content = new string('b', 50),
        });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_SameUserDifferentVacancies_Returns201ForBoth()
    {
        if (!await EnsureDatabaseAvailableAsync())
            return;

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (authCookie, userId) = await CoverLettersApiTests.CreateUserAndGetCookieAsync(client, NewUserCommand());

        var vacancyA = await CoverLettersApiTests.SeedVacancyAsync(_factory, userId);
        var vacancyB = await CoverLettersApiTests.SeedVacancyAsync(_factory, userId);

        var first = await CoverLettersApiTests.PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId = vacancyA,
            content = new string('a', 50),
        });

        var second = await CoverLettersApiTests.PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId = vacancyB,
            content = new string('b', 50),
        });

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ConcurrentDuplicateVacancyForSameUser_OneCreatedOneConflict()
    {
        if (!await EnsureDatabaseAvailableAsync())
            return;

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (authCookie, userId) = await CoverLettersApiTests.CreateUserAndGetCookieAsync(client, NewUserCommand("clu_race_"));
        var vacancyId = await CoverLettersApiTests.SeedVacancyAsync(_factory, userId);

        var firstTask = CoverLettersApiTests.PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId,
            content = new string('a', 50),
        });
        var secondTask = CoverLettersApiTests.PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId,
            content = new string('b', 50),
        });

        var responses = await Task.WhenAll(firstTask, secondTask);

        responses.Count(x => x.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
    }
}

public sealed class CoverLettersPostgresFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=JobNectoTest;Username=test;Password=test";

    private readonly string _baseConnectionString;
    private readonly string _schemaName;
    private string? _scopedConnectionString;
    private bool _schemaInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ITestOutputHelper? _output;

    public CoverLettersPostgresFactory(ITestOutputHelper? output = null)
    {
        _output = output;
        _baseConnectionString = Environment.GetEnvironmentVariable("JOBNECTO_TEST_POSTGRES")
            ?? DefaultConnectionString;
        _schemaName = "cl_uniqueness_" + Guid.NewGuid().ToString("N");
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
                SearchPath = _schemaName,
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