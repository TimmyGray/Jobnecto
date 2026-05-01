using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.Resumes;
using JobNecto.Application.Users;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    private static async Task<ResumeResult> CreateResumeAsync(HttpClient client, string authCookie, string title)
    {
        var cmd = NewResumeCommand(title);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/resumes")
        {
            Content = JsonContent.Create(cmd)
        };
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResumeResult>(JsonOpts);
        created.Should().NotBeNull();
        return created!;
    }

    private static async Task<HttpResponseMessage> UpdateResumeAsync(
        HttpClient client,
        string authCookie,
        Guid resumeId,
        object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/resumes/{resumeId}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> DeleteResumeAsync(
        HttpClient client,
        string authCookie,
        Guid resumeId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/resumes/{resumeId}");
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);

        return await client.SendAsync(request);
    }

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

    // ──────────────────────────────────────────────────────────────────
    //  Story 2.3: GET /api/v1/resumes/{id}
    // ──────────────────────────────────────────────────────────────────

    /// <summary>Sends an authenticated GET to /api/v1/resumes/{id}.</summary>
    private static async Task<HttpResponseMessage> GetResumeByIdAsync(
        HttpClient client, string authCookie, Guid id)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/resumes/{id}");
        req.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(req);
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/resumes/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_OwnedResume_Returns200WithFullResumeResult()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        // Create a resume
        var cmd = NewResumeCommand("My Test Resume");
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/resumes")
        {
            Content = JsonContent.Create(cmd)
        };
        createReq.Headers.TryAddWithoutValidation("Cookie", authCookie);
        var createResp = await client.SendAsync(createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<ResumeResult>(JsonOpts);
        created.Should().NotBeNull();

        // Get by ID
        var getResp = await GetResumeByIdAsync(client, authCookie, created!.Id);

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResumeResult>(body, JsonOpts);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Title.Should().Be("My Test Resume");
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var resp = await GetResumeByIdAsync(client, authCookie, Guid.NewGuid());

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ResumeBelongingToDifferentUser_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        // User A creates a resume
        var cookieA = await CreateUserAndGetCookieAsync(client, NewUserCommand("owner_a"));
        var cmd = NewResumeCommand("Owner A Resume");
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/resumes")
        {
            Content = JsonContent.Create(cmd)
        };
        createReq.Headers.TryAddWithoutValidation("Cookie", cookieA);
        var createResp = await client.SendAsync(createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<ResumeResult>(JsonOpts);
        created.Should().NotBeNull();

        // User B tries to access User A's resume — should get 404, not 403
        var cookieB = await CreateUserAndGetCookieAsync(client, NewUserCommand("attacker_b"));
        var resp = await GetResumeByIdAsync(client, cookieB, created!.Id);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistentAndCrossUser_ReturnIdenticalNotFoundProblemDetails()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        // User A creates a resume.
        var ownerCookie = await CreateUserAndGetCookieAsync(client, NewUserCommand("owner_equal_404"));
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/resumes")
        {
            Content = JsonContent.Create(NewResumeCommand("Owner Resume"))
        };
        createReq.Headers.TryAddWithoutValidation("Cookie", ownerCookie);

        var createResp = await client.SendAsync(createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<ResumeResult>(JsonOpts);
        created.Should().NotBeNull();

        // Scenario 1: non-existent resume id.
        var nonExistentResp = await GetResumeByIdAsync(client, ownerCookie, Guid.NewGuid());
        nonExistentResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var nonExistentBody = await nonExistentResp.Content.ReadFromJsonAsync<ProblemDetails>(JsonOpts);
        nonExistentBody.Should().NotBeNull();

        // Scenario 2: cross-user access to existing resume id.
        var attackerCookie = await CreateUserAndGetCookieAsync(client, NewUserCommand("attacker_equal_404"));
        var crossUserResp = await GetResumeByIdAsync(client, attackerCookie, created!.Id);
        crossUserResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var crossUserBody = await crossUserResp.Content.ReadFromJsonAsync<ProblemDetails>(JsonOpts);
        crossUserBody.Should().NotBeNull();

        // Both 404 paths must be indistinguishable to avoid existence leakage.
        crossUserBody!.Title.Should().Be(nonExistentBody!.Title);
        crossUserBody.Detail.Should().Be(nonExistentBody.Detail);
    }

    // ──────────────────────────────────────────────────────────────────
    //  Story 2.4: PATCH /api/v1/resumes/{id}
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync($"/api/v1/resumes/{Guid.NewGuid()}", new { title = "Updated" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_OwnedResume_Returns200WithUpdatedFieldsAndUpdatedAt()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var created = await CreateResumeAsync(client, authCookie, "Original Title");

        var response = await UpdateResumeAsync(client, authCookie, created.Id, new
        {
            title = "Updated Title",
            skills = new[] { "C#", "EF Core", "SQL" },
            workLocationType = "hybrid"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResumeResult>(JsonOpts);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Title.Should().Be("Updated Title");
        result.Skills.Should().BeEquivalentTo(new[] { "C#", "EF Core", "SQL" });
        result.WorkLocationType.Should().Be("Hybrid");
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await UpdateResumeAsync(client, authCookie, Guid.NewGuid(), new
        {
            title = "Updated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_SoftDeletedResume_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var created = await CreateResumeAsync(client, authCookie, "Delete Me");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var resume = await dbContext.Resumes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == created.Id);

            resume.Should().NotBeNull();
            resume!.IsDeleted = true;
            resume.DeletedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        var response = await UpdateResumeAsync(client, authCookie, created.Id, new
        {
            title = "Should not update"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ResumeBelongingToDifferentUser_Returns403()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var ownerCookie = await CreateUserAndGetCookieAsync(client, NewUserCommand("owner_update"));
        var created = await CreateResumeAsync(client, ownerCookie, "Owner Resume");

        var attackerCookie = await CreateUserAndGetCookieAsync(client, NewUserCommand("attacker_update"));
        var response = await UpdateResumeAsync(client, attackerCookie, created.Id, new
        {
            title = "Malicious"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_EmptySkills_Returns400WithValidationErrors()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var created = await CreateResumeAsync(client, authCookie, "Original");

        var response = await UpdateResumeAsync(client, authCookie, created.Id, new
        {
            skills = Array.Empty<string>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Skills");
    }

    [Fact]
    public async Task Update_NoFieldsProvided_Returns400WithValidationErrors()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var created = await CreateResumeAsync(client, authCookie, "Original");

        var response = await UpdateResumeAsync(client, authCookie, created.Id, new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("At least one updatable field must be provided.");
    }

    // ──────────────────────────────────────────────────────────────────
    //  Story 2.5: DELETE /api/v1/resumes/{id}
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/resumes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_OwnedResume_Returns204_AndDeletedResumeIsHiddenFromListAndDetail()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var created = await CreateResumeAsync(client, authCookie, "Delete Me");

        var deleteResponse = await DeleteResumeAsync(client, authCookie, created.Id);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await GetResumeByIdAsync(client, authCookie, created.Id);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var listResponse = await GetResumesAsync(client, authCookie);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await listResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ResumeResult>>(body, JsonOpts);

        result.Should().NotBeNull();
        result!.Items.Should().NotContain(x => x.Id == created.Id);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await DeleteResumeAsync(client, authCookie, Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_EmptyResumeId_Returns400WithValidationErrors()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await DeleteResumeAsync(client, authCookie, Guid.Empty);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("resumeId is required.");
    }

    [Fact]
    public async Task Delete_ResumeBelongingToDifferentUser_Returns403()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var ownerCookie = await CreateUserAndGetCookieAsync(client, NewUserCommand("owner_delete"));
        var created = await CreateResumeAsync(client, ownerCookie, "Owner Resume");

        var attackerCookie = await CreateUserAndGetCookieAsync(client, NewUserCommand("attacker_delete"));
        var response = await DeleteResumeAsync(client, attackerCookie, created.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

