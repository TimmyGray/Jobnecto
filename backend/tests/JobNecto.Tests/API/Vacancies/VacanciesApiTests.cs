using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API.Vacancies;

public class VacanciesApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static CreateUserCommand NewUserCommand(string prefix = "vacancy_user") =>
        new()
        {
            LoginName = prefix + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
        };

    private static async Task<(string AuthCookie, Guid UserId)> CreateUserAndGetCookieAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/users", NewUserCommand());
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await response.Content.ReadFromJsonAsync<CreateUserResult>(JsonOptions);
        createdUser.Should().NotBeNull();

        var authCookie = response
            .Headers.GetValues("Set-Cookie")
            .Select(x =>
                x.Split(';', StringSplitOptions.TrimEntries)
                    .FirstOrDefault(y => y.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase))
            )
            .First(x => !string.IsNullOrWhiteSpace(x));

        authCookie.Should().NotBeNullOrWhiteSpace();
        return (authCookie!, createdUser!.Id);
    }

    private static async Task<HttpResponseMessage> PostFilterAsync(
        HttpClient client,
        string authCookie,
        object payload
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/vacancies/filter")
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    private static async Task<IReadOnlyList<Vacancy>> SeedVacanciesAsync(
        JobNectoApiFactory factory,
        Guid userId
    )
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            var owner = VacancyTestData.CreateOwnerUser(id: userId);
            db.Users.Add(owner);
        }

        var seeded = VacancyTestData.AllDistinctUpdatedAt(userId).ToList();
        db.Vacancies.AddRange(seeded);

        await db.SaveChangesAsync();
        return seeded;
    }

    [Fact]
    public async Task Filter_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/vacancies/filter", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Filter_EmptyBody_WithNoVacancies_Returns200WithEmptyResult()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await PostFilterAsync(client, authCookie, new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(0);
        result.PageSize.Should().Be(20);
        result.HasNext.Should().BeFalse();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Filter_EmptyBody_WithSeededVacancies_ReturnsCreatedAtDescending()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var seeded = await SeedVacanciesAsync(factory, userId);
        var expectedOrder = seeded.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).ToList();

        var response = await PostFilterAsync(client, authCookie, new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(expectedOrder.Count);
        result.Items.Should().HaveCount(expectedOrder.Count);

        var returnedIds = result.Items.Select(i => i.Id).ToList();
        var expectedIds = expectedOrder.Select(v => v.Id).ToList();
        returnedIds.Should().Equal(expectedIds);

        result.Items.Should().OnlyContain(i => i.Id != Guid.Empty);
        result.Items.Should().OnlyContain(i => i.CreatedAt != default);
    }

    [Fact]
    public async Task Filter_ReturnsOnlyAuthenticatedUsersVacancies()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var ownSeeded = await SeedVacanciesAsync(factory, userId);
        await SeedVacanciesAsync(factory, Guid.NewGuid());

        var expectedOrder = ownSeeded
            .OrderByDescending(v => v.CreatedAt)
            .ThenByDescending(v => v.Id)
            .Select(v => v.Id)
            .ToList();

        var response = await PostFilterAsync(client, authCookie, new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(ownSeeded.Count);
        result.Items.Select(i => i.Id).Should().Equal(expectedOrder);
    }

    [Fact]
    public async Task Filter_PageSizeAbove100_IsCappedTo100()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        await SeedVacanciesAsync(factory, userId);

        var response = await PostFilterAsync(client, authCookie, new { pageSize = 500 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Filter_WithCursor_ReturnsNextSlice()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var seeded = await SeedVacanciesAsync(factory, userId);
        var expectedOrder = seeded.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).ToList();

        var firstResponse = await PostFilterAsync(client, authCookie, new { pageSize = 2 });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPage = await firstResponse.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        firstPage.Should().NotBeNull();
        firstPage!.Items.Should().HaveCount(2);
        firstPage.HasNext.Should().BeTrue();
        firstPage.LastSeenId.Should().NotBeNull();
        firstPage.LastSeenUpdatedAt.Should().NotBeNull();

        var secondResponse = await PostFilterAsync(
            client,
            authCookie,
            new
            {
                pageSize = 2,
                lastSeenId = firstPage.LastSeenId,
                lastSeenUpdatedAt = firstPage.LastSeenUpdatedAt,
            }
        );

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        secondPage.Should().NotBeNull();
        secondPage!.Items.Should().HaveCount(2);

        secondPage.Items[0].Id.Should().Be(expectedOrder[2].Id);
        secondPage.Items[1].Id.Should().Be(expectedOrder[3].Id);
    }

    [Fact]
    public async Task Filter_WithSortByUpdatedAt_ReturnsUpdatedAtDescending()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var seeded = await SeedVacanciesAsync(factory, userId);
        var expectedOrder = seeded.OrderByDescending(v => v.UpdatedAt).ThenByDescending(v => v.Id).ToList();

        var response = await PostFilterAsync(client, authCookie, new { sortBy = "updatedAt" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Select(i => i.Id).Should().Equal(expectedOrder.Select(v => v.Id));
    }

    [Fact]
    public async Task Filter_WithOnlyLastSeenId_Returns400WithProblemDetails()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await PostFilterAsync(client, authCookie, new { lastSeenId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem!.Title.Should().Be("Validation failed");
        problem.Detail.Should().Contain("lastSeenId");
    }

    [Fact]
    public async Task Filter_WithOnlyLastSeenUpdatedAt_Returns400WithProblemDetails()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await PostFilterAsync(client, authCookie, new { lastSeenUpdatedAt = DateTime.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem!.Title.Should().Be("Validation failed");
        problem.Detail.Should().Contain("lastSeenUpdatedAt");
    }

    [Fact]
    public async Task Filter_WithUnknownSortBy_Returns400WithProblemDetails()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await PostFilterAsync(client, authCookie, new { sortBy = "popularity" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem!.Title.Should().Be("Validation failed");
        problem.Detail.Should().Contain("sortBy");
    }

    [Fact]
    public async Task Filter_WithLocationStringFilter_ReturnsOnlyMatchingVacancies()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var seeded = await SeedVacanciesAsync(factory, userId);

        var polandCount = seeded.Count(v => v.Location == JobNecto.Domain.Enums.Location.Poland);

        var response = await PostFilterAsync(client, authCookie, new { location = new[] { "Poland" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(polandCount);
    }

    // ── Story 4.2 tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_SalaryMinGreaterThanSalaryMax_Returns400WithFieldLevelError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await PostFilterAsync(client, authCookie, new { salaryMin = 100_000, salaryMax = 50_000 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        root.GetProperty("title").GetString().Should().Be("Validation failed");
        root.GetProperty("errors").TryGetProperty("SalaryMin", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Filter_SalaryMinEqualToSalaryMax_Returns200()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await PostFilterAsync(client, authCookie, new { salaryMin = 80_000, salaryMax = 80_000 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Filter_WithExcludeKeywords_ExcludesMatchingVacancies()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var seeded = await SeedVacanciesAsync(factory, userId);

        // "PHP" appears in the title of LegacyFullStackHidden
        var phpCount = seeded.Count(v => v.Title != null && v.Title.Contains("PHP"));
        phpCount.Should().Be(1, "precondition: exactly one seeded vacancy has PHP in title");

        var response = await PostFilterAsync(client, authCookie, new { excludeKeywords = new[] { "PHP" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(seeded.Count - phpCount);
        result.Items.Should().NotContain(i => i.Title != null && i.Title.Contains("PHP"));
    }

    [Fact]
    public async Task Filter_WithMultipleCriteria_ReturnsAndIntersection()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var seeded = await SeedVacanciesAsync(factory, userId);

        // Poland vacancies with salaryMin >= 90_000:
        // SeniorDotNetRemotePoland: location=Poland, salaryMin=95_000 ✓
        // DataAnalystInternshipWarsaw: location=Poland, salaryMin=4_500 ✗
        var expected = seeded
            .Where(v => v.Location == JobNecto.Domain.Enums.Location.Poland && v.SalaryMin >= 90_000m)
            .ToList();
        expected.Should().HaveCount(1, "precondition check");

        var response = await PostFilterAsync(client, authCookie, new
        {
            location = new[] { "Poland" },
            salaryMin = 90_000,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(expected.Count);
        result.Items.Select(i => i.Id).Should().BeEquivalentTo(expected.Select(v => v.Id));
    }

    [Fact]
    public async Task Filter_WithFilteredCriteriaAndCursor_ReturnsCorrectSlice()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var seeded = await SeedVacanciesAsync(factory, userId);

        // Remote vacancies ordered by createdAt desc:
        // SeniorDotNetRemotePoland, ContractDevOpsRemoteUs, PartTimeContentMarketerRemote, BareMinimumScrapedListing
        var remoteVacancies = seeded
            .Where(v => v.WorkLocationType == JobNecto.Domain.Enums.WorkLocationType.Remote)
            .OrderByDescending(v => v.CreatedAt)
            .ThenByDescending(v => v.Id)
            .ToList();
        remoteVacancies.Should().HaveCountGreaterThan(1, "precondition: need at least 2 remote vacancies for cursor test");

        var firstResponse = await PostFilterAsync(client, authCookie, new
        {
            workLocationType = new[] { "Remote" },
            pageSize = 1,
        });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPage = await firstResponse.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        firstPage.Should().NotBeNull();
        firstPage!.Items.Should().HaveCount(1);
        firstPage.HasNext.Should().BeTrue();
        firstPage.Items[0].Id.Should().Be(remoteVacancies[0].Id);

        var secondResponse = await PostFilterAsync(client, authCookie, new
        {
            workLocationType = new[] { "Remote" },
            pageSize = 1,
            lastSeenId = firstPage.LastSeenId,
            lastSeenUpdatedAt = firstPage.LastSeenUpdatedAt,
        });

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        secondPage.Should().NotBeNull();
        secondPage!.Items.Should().HaveCount(1);
        secondPage.Items[0].Id.Should().Be(remoteVacancies[1].Id);
    }

    [Fact]
    public async Task Filter_WithSortByRelevance_UsesUpdatedAtOrderingAlias()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var seeded = await SeedVacanciesAsync(factory, userId);
        var expectedOrder = seeded.OrderByDescending(v => v.UpdatedAt).ThenByDescending(v => v.Id).ToList();

        var response = await PostFilterAsync(client, authCookie, new { sortBy = "relevance" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VacancyPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Select(i => i.Id).Should().Equal(expectedOrder.Select(v => v.Id));
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
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? WorkLocationType { get; set; }
        public string? Location { get; set; }
        public VacancySalaryDto? Salary { get; set; }
        public string? Currency { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class VacancySalaryDto
    {
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }
}