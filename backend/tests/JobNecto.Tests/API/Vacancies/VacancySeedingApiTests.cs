using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.JobSources;
using JobNecto.Application.Users;
using JobNecto.Infrastructure.JobSources.Seeding;
using JobNecto.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using JobNecto.Infrastructure.JobSources.Static;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace JobNecto.Tests.API.Vacancies;

/// <summary>
/// HTTP-level coverage for the demo vacancy catalogue: registration seeds the new user's board,
/// paging and ownership hold for seeded rows, the flag gates everything, and a failing source
/// never breaks sign-up.
/// </summary>
public class VacancySeedingApiTests
{
    private const int CatalogueSize = 40;
    private const int DefaultPageSize = 20;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Register_WithSeedingEnabled_FirstBrowsePageHas20Of40AndHasNext()
    {
        await using var factory = new SeedingEnabledApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await RegisterAsync(client);

        var firstPage = await BrowseAsync(client, authCookie, new { });

        firstPage.TotalCount.Should().Be(CatalogueSize);
        firstPage.Items.Should().HaveCount(DefaultPageSize);
        firstPage.HasNext.Should().BeTrue();
        firstPage.Items.Should().BeInDescendingOrder(i => i.CreatedAt);
    }

    [Fact]
    public async Task Register_WithSeedingEnabled_SecondBrowsePageHasRemaining20AndNoNext()
    {
        await using var factory = new SeedingEnabledApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await RegisterAsync(client);
        var firstPage = await BrowseAsync(client, authCookie, new { });

        var secondPage = await BrowseAsync(
            client,
            authCookie,
            new { lastSeenId = firstPage.LastSeenId, lastSeenUpdatedAt = firstPage.LastSeenUpdatedAt }
        );

        secondPage.TotalCount.Should().Be(CatalogueSize);
        secondPage.Items.Should().HaveCount(CatalogueSize - DefaultPageSize);
        secondPage.HasNext.Should().BeFalse();
        firstPage.Items.Select(i => i.Id)
            .Concat(secondPage.Items.Select(i => i.Id))
            .Should().OnlyHaveUniqueItems()
            .And.HaveCount(CatalogueSize);
    }

    [Fact]
    public async Task SeededVacancy_IsReadableByOwner_AndNotFoundForAnotherUser()
    {
        await using var factory = new SeedingEnabledApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (ownerCookie, _) = await RegisterAsync(client);
        var (otherCookie, _) = await RegisterAsync(client);
        var ownerPage = await BrowseAsync(client, ownerCookie, new { });
        var vacancyId = ownerPage.Items[0].Id;

        var ownerResponse = await GetDetailAsync(client, ownerCookie, vacancyId);
        var otherResponse = await GetDetailAsync(client, otherCookie, vacancyId);

        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await ownerResponse.Content.ReadFromJsonAsync<VacancyDetailDto>(JsonOptions);
        detail!.JobSource!.Name.Should().Be("Jobnecto Demo Catalogue");
        detail.JobSource.Url.Should().StartWith("https://jobs.example.test/");
        otherResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SecondUser_GetsTheirOwn40Vacancies_WithDifferentIds()
    {
        await using var factory = new SeedingEnabledApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (firstCookie, _) = await RegisterAsync(client);
        var (secondCookie, _) = await RegisterAsync(client);

        var firstIds = await CollectAllIdsAsync(client, firstCookie);
        var secondIds = await CollectAllIdsAsync(client, secondCookie);

        firstIds.Should().HaveCount(CatalogueSize);
        secondIds.Should().HaveCount(CatalogueSize);
        firstIds.Should().NotIntersectWith(secondIds);
    }

    [Fact]
    public async Task Register_WithDefaultTestConfiguration_SeedsNothing()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await RegisterAsync(client);
        var page = await BrowseAsync(client, authCookie, new { });

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_WhenJobSourceThrows_StillReturns201WithCookie_AndSeedsNothing()
    {
        await using var factory = new SeedingEnabledApiFactory(useFailingSource: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await RegisterAsync(client);
        var page = await BrowseAsync(client, authCookie, new { });

        page.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task EverySeededVacancy_IsReadableByItsOwner_WithDistinctCreatedAt()
    {
        await using var factory = new SeedingEnabledApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await RegisterAsync(client);
        var page = await BrowseAsync(client, authCookie, new { pageSize = 100 });

        page.Items.Select(i => i.CreatedAt).Should().OnlyHaveUniqueItems();
        foreach (var item in page.Items)
        {
            var response = await GetDetailAsync(client, authCookie, item.Id);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"vacancy {item.Id} belongs to the caller");
        }
    }

    [Fact]
    public async Task Backfill_SeedsExistingUserWithoutCatalogue_AndIsIdempotentOnRerun()
    {
        await using var factory = new SeedingEnabledApiFactory();
        var existingUser = VacancyTestData.CreateOwnerUser();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(existingUser);
            await db.SaveChangesAsync();
        }
        var backfill = factory.Services.GetServices<IHostedService>().OfType<VacancySeedBackfillService>().Single();

        await backfill.StartAsync(CancellationToken.None);
        var afterFirstRun = await CountVacanciesAsync(factory, existingUser.Id);
        await backfill.StartAsync(CancellationToken.None);
        var afterSecondRun = await CountVacanciesAsync(factory, existingUser.Id);

        afterFirstRun.Should().Be(CatalogueSize);
        afterSecondRun.Should().Be(CatalogueSize);
    }

    [Fact]
    public async Task Backfill_AfterRegistration_AddsNothing_AndNeverResurrectsADeletedVacancy()
    {
        await using var factory = new SeedingEnabledApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (authCookie, userId) = await RegisterAsync(client);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var seeded = await db.Vacancies.FirstAsync(v => v.UserId == userId);
            seeded.IsDeleted = true;
            seeded.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        var backfill = factory.Services.GetServices<IHostedService>().OfType<VacancySeedBackfillService>().Single();

        await backfill.StartAsync(CancellationToken.None);

        var page = await BrowseAsync(client, authCookie, new { });
        page.TotalCount.Should().Be(CatalogueSize - 1);
        (await CountVacanciesAsync(factory, userId, includeDeleted: true)).Should().Be(CatalogueSize);
    }

    [Fact]
    public async Task CorsFactory_DevelopmentHost_KeepsSeedingOff()
    {
        await using var factory = new CorsFactory();

        factory.Services.GetRequiredService<IOptions<VacancySeedOptions>>().Value.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task OpenApiSchemaFactory_DevelopmentHost_KeepsSeedingOff()
    {
        await using var factory = new OpenApiSchemaFactory();

        factory.Services.GetRequiredService<IOptions<VacancySeedOptions>>().Value.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Container_WiresSeedingHandler_DemoSource_AndBackfill()
    {
        await using var factory = new JobNectoApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        provider.GetServices<INotificationHandler<UserRegisteredNotification>>()
            .Should().ContainSingle(h => h is SeedVacanciesOnUserRegisteredHandler);
        provider.GetRequiredService<IJobSource>().Should().BeOfType<StaticJobSource>();
        provider.GetRequiredService<IVacancyIngestionService>().Should().BeOfType<VacancyIngestionService>();
        factory.Services.GetServices<IHostedService>()
            .Should().ContainSingle(s => s is VacancySeedBackfillService);
        provider.GetRequiredService<IOptions<VacancySeedOptions>>().Value.Enabled.Should().BeFalse();
    }

    private static async Task<(string AuthCookie, Guid UserId)> RegisterAsync(HttpClient client)
    {
        var command = new CreateUserCommand
        {
            LoginName = "seeded_user" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
        };

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateUserResult>(JsonOptions);
        created.Should().NotBeNull();

        var authCookie = response
            .Headers.GetValues("Set-Cookie")
            .Select(x =>
                x.Split(';', StringSplitOptions.TrimEntries)
                    .FirstOrDefault(y => y.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase))
            )
            .First(x => !string.IsNullOrWhiteSpace(x));

        authCookie.Should().NotBeNullOrWhiteSpace();
        return (authCookie!, created!.Id);
    }

    private static async Task<VacancyPagedResultDto> BrowseAsync(HttpClient client, string authCookie, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/vacancies/filter")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        return result!;
    }

    private static async Task<HttpResponseMessage> GetDetailAsync(HttpClient client, string authCookie, Guid vacancyId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/vacancies/{vacancyId}");
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    private static async Task<int> CountVacanciesAsync(JobNectoApiFactory factory, Guid userId, bool includeDeleted = false)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = includeDeleted ? db.Vacancies.IgnoreQueryFilters() : db.Vacancies;
        return await query.CountAsync(v => v.UserId == userId);
    }

    private static async Task<List<Guid>> CollectAllIdsAsync(HttpClient client, string authCookie)
    {
        var page = await BrowseAsync(client, authCookie, new { pageSize = 100 });
        return page.Items.Select(i => i.Id).ToList();
    }

    /// <summary>
    /// Test host with <c>Seed:Vacancies:Enabled = true</c>. Optionally swaps the catalogue for a
    /// source that always throws, to prove sign-up survives a seeding failure.
    /// </summary>
    private sealed class SeedingEnabledApiFactory(bool useFailingSource = false) : JobNectoApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Seed:Vacancies:Enabled"] = "true",
                }));

            if (useFailingSource)
            {
                builder.ConfigureTestServices(services =>
                    services.Replace(ServiceDescriptor.Singleton<IJobSource, FailingJobSource>()));
            }
        }
    }

    private sealed class FailingJobSource : IJobSource
    {
        public string Name => "Failing";

        public Task<IReadOnlyList<JobPosting>> FetchAsync(CancellationToken ct) =>
            throw new InvalidOperationException("Simulated job source outage.");
    }

    private sealed class VacancyPagedResultDto
    {
        public List<VacancyListItemDto> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public Guid? LastSeenId { get; set; }
        public DateTime? LastSeenUpdatedAt { get; set; }
        public int PageSize { get; set; }
        public bool HasNext { get; set; }
    }

    private sealed class VacancyListItemDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class VacancyDetailDto
    {
        public Guid Id { get; set; }
        public VacancyJobSourceDto? JobSource { get; set; }
    }

    private sealed class VacancyJobSourceDto
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
    }
}
