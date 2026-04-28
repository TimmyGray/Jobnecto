using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.Resumes;
using JobNecto.Application.Users;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Tests.API;

public class ResumesControllerTests
{
    // ──────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────

    private static CreateUserCommand NewUserCommand(string prefix = "user") => new()
    {
        LoginName = prefix + Guid.NewGuid().ToString("N")[..8],
        Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
        Password = "Password123!"
    };

    private static CreateResumeCommand NewResumeCommand(string title = "Test Resume") => new()
    {
        Title = title,
        Skills = ["C#", "SQL"],
        WorkLocationType = "remote",
    };

    /// <summary>Creates a user and returns the auth-token cookie value (e.g. "auth-token=eyJ...").</summary>
    private static async Task<string> CreateUserAndGetCookieAsync(HttpClient client, CreateUserCommand? cmd = null)
    {
        cmd ??= NewUserCommand();
        var resp = await client.PostAsJsonAsync("/api/v1/users", cmd);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var authCookie = resp.Headers
            .GetValues("Set-Cookie")
            .Select(h => h.Split(';', StringSplitOptions.TrimEntries)
                          .FirstOrDefault(p => p.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase)))
            .First(c => !string.IsNullOrWhiteSpace(c));

        authCookie.Should().NotBeNullOrWhiteSpace();
        return authCookie!;
    }

    /// <summary>Sends an authenticated GET to /api/v1/resumes.</summary>
    private static async Task<HttpResponseMessage> GetResumesAsync(
        HttpClient client, string authCookie, string queryString = "")
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/resumes{queryString}");
        req.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(req);
    }

    /// <summary>Seeds n resumes for the authenticated user and returns the last posted title.</summary>
    private static async Task SeedResumesAsync(HttpClient client, string authCookie, int count, string titlePrefix = "Resume")
    {
        for (var i = 1; i <= count; i++)
        {
            var cmd = NewResumeCommand($"{titlePrefix} {i}");
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/resumes")
            {
                Content = JsonContent.Create(cmd)
            };
            req.Headers.TryAddWithoutValidation("Cookie", authCookie);
            var resp = await client.SendAsync(req);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // ──────────────────────────────────────────────────────────────────
    //  AC 1: Authentication required
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/resumes");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────────
    //  AC 5: No resumes → empty list
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_UserHasNoResumes_Returns200WithEmptyItems()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var resp = await GetResumesAsync(client, authCookie);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(body, JsonOpts);

        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────
    //  AC 2: Returns only current user's resumes
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsOnlyCurrentUserResumes_NotOtherUsers()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        // User A creates 2 resumes
        var cookieA = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_a"));
        await SeedResumesAsync(client, cookieA, 2, "UserA Resume");

        // User B creates 1 resume
        var cookieB = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_b"));
        await SeedResumesAsync(client, cookieB, 1, "UserB Resume");

        // User B should only see their own 1 resume
        var resp = await GetResumesAsync(client, cookieB);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(body, JsonOpts);

        result!.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().StartWith("UserB Resume");
    }

    // ──────────────────────────────────────────────────────────────────
    //  AC 3: pageSize query param is respected
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithPageSizeParam_ReturnsCorrectCount()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        await SeedResumesAsync(client, authCookie, 5);

        var resp = await GetResumesAsync(client, authCookie, "?pageSize=3");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(body, JsonOpts);

        result!.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.HasNext.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────
    //  AC 4: cursor params return the next page window
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithCursorParams_ReturnsNextSlice()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        await SeedResumesAsync(client, authCookie, 5);

        var firstPageResponse = await GetResumesAsync(client, authCookie, "?pageSize=2");
        firstPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPageBody = await firstPageResponse.Content.ReadAsStringAsync();
        var firstPage = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(firstPageBody, JsonOpts);

        firstPage.Should().NotBeNull();
        firstPage!.Items.Should().HaveCount(2);
        firstPage.HasNext.Should().BeTrue();
        firstPage.LastSeenId.Should().NotBeNull();
        firstPage.LastSeenUpdatedAt.Should().NotBeNull();

        var cursorQuery =
            $"?pageSize=2&lastSeenId={firstPage.LastSeenId}&lastSeenUpdatedAt={Uri.EscapeDataString(firstPage.LastSeenUpdatedAt!.Value.ToString("o"))}";

        var secondPageResponse = await GetResumesAsync(client, authCookie, cursorQuery);
        secondPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPageBody = await secondPageResponse.Content.ReadAsStringAsync();
        var secondPage = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(secondPageBody, JsonOpts);

        secondPage.Should().NotBeNull();
        secondPage!.Items.Should().HaveCount(2);

        var firstPageIds = firstPage.Items.Select(x => x.Id).ToHashSet();
        var secondPageIds = secondPage.Items.Select(x => x.Id).ToHashSet();
        secondPageIds.Intersect(firstPageIds).Should().BeEmpty();
    }

    [Fact]
    public async Task List_WithCursorTimestampWithoutTimezone_ReturnsNextSlice()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        await SeedResumesAsync(client, authCookie, 5);

        var firstPageResponse = await GetResumesAsync(client, authCookie, "?pageSize=2");
        firstPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPageBody = await firstPageResponse.Content.ReadAsStringAsync();
        var firstPage = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(firstPageBody, JsonOpts);

        firstPage.Should().NotBeNull();
        firstPage!.Items.Should().HaveCount(2);
        firstPage.LastSeenId.Should().NotBeNull();
        firstPage.LastSeenUpdatedAt.Should().NotBeNull();

        var timestampWithoutTimezone = firstPage.LastSeenUpdatedAt!.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
        var cursorQuery =
            $"?pageSize=2&lastSeenId={firstPage.LastSeenId}&lastSeenUpdatedAt={Uri.EscapeDataString(timestampWithoutTimezone)}";

        var secondPageResponse = await GetResumesAsync(client, authCookie, cursorQuery);
        secondPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPageBody = await secondPageResponse.Content.ReadAsStringAsync();
        var secondPage = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(secondPageBody, JsonOpts);

        secondPage.Should().NotBeNull();
        secondPage!.Items.Should().HaveCount(2);

        var firstPageIds = firstPage.Items.Select(x => x.Id).ToHashSet();
        var secondPageIds = secondPage.Items.Select(x => x.Id).ToHashSet();
        secondPageIds.Intersect(firstPageIds).Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────
    //  AC 3: pageSize < 1 treated as default (20)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithPageSizeBelowOne_DefaultsToTwenty()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        await SeedResumesAsync(client, authCookie, 3);

        var resp = await GetResumesAsync(client, authCookie, "?pageSize=0");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(body, JsonOpts);

        result!.PageSize.Should().Be(20);
        result.Items.Should().HaveCount(3); // all 3 fit within the default page
    }

    // ──────────────────────────────────────────────────────────────────
    //  AC 3: non-numeric pageSize returns 400 (ApiController model binding)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithInvalidPageSizeFormat_Returns400()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var resp = await GetResumesAsync(client, authCookie, "?pageSize=abc");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────────
    //  AC 3: pageSize > 100 is capped
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithPageSizeAbove100_IsCappedAt100()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        await SeedResumesAsync(client, authCookie, 3);

        var resp = await GetResumesAsync(client, authCookie, "?pageSize=500");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(body, JsonOpts);

        result!.PageSize.Should().Be(100);
    }
}
