using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.Educations;
using JobNecto.Application.Users;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API.Educations;

public class EducationsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static CreateUserCommand NewUserCommand(string prefix = "education_user") =>
        new()
        {
            LoginName = prefix + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
        };

    private static CreateEducationCommand NewEducationCommand() =>
        new()
        {
            Title = "Bachelor of Science",
            Specialization = "Computer Science",
            Degree = "bachelor",
        };

    private static async Task<string> CreateUserAndGetCookieAsync(
        HttpClient client,
        CreateUserCommand? command = null
    )
    {
        command ??= NewUserCommand();

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var authCookie = response
            .Headers.GetValues("Set-Cookie")
            .Select(x =>
                x.Split(';', StringSplitOptions.TrimEntries)
                    .FirstOrDefault(y =>
                        y.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase)
                    )
            )
            .First(x => !string.IsNullOrWhiteSpace(x));

        authCookie.Should().NotBeNullOrWhiteSpace();
        return authCookie!;
    }

    private static async Task<HttpResponseMessage> PostEducationAsync(
        HttpClient client,
        string authCookie,
        object payload
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/educations")
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/educations", NewEducationCommand());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidPayload_Returns201WithLocationAndPayload()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var command = NewEducationCommand();

        var response = await PostEducationAsync(client, authCookie, command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/v1/educations/");

        var result = await response.Content.ReadFromJsonAsync<EducationResult>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Title.Should().Be(command.Title);
        result.Specialization.Should().Be(command.Specialization);
        result.Degree.Should().Be("bachelor");
    }

    [Fact]
    public async Task Create_MissingTitle_Returns400WithFieldLevelError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await PostEducationAsync(
            client,
            authCookie,
            new { specialization = "Computer Science", degree = "master" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Title");
    }

    [Fact]
    public async Task Create_InvalidDegree_Returns400WithFieldLevelError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await PostEducationAsync(
            client,
            authCookie,
            new
            {
                title = "Master of Science",
                specialization = "Computer Science",
                degree = "invalid_degree",
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Degree");
    }

    private static async Task<HttpResponseMessage> GetEducationsAsync(
        HttpClient client,
        string authCookie,
        string queryString = ""
    )
    {
        var url =
            "/api/v1/educations" + (string.IsNullOrEmpty(queryString) ? "" : "?" + queryString);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/educations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_UserWithNoEducations_Returns200WithEmptyResult()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await GetEducationsAsync(client, authCookie);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(0);
        result.HasNext.Should().BeFalse();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsOnlyCurrentUsersRecords_NotAnotherUsers()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var cookieA = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_a_"));
        var cookieB = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_b_"));

        // User A creates an education record
        var createResponse = await PostEducationAsync(client, cookieA, NewEducationCommand());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // User B lists their own educations — should be empty
        var response = await GetEducationsAsync(client, cookieB);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task List_ReturnsAllNonDeletedRecordsWithCorrectEnvelope()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);

        // Create two education records
        var cmd1 = new CreateEducationCommand
        {
            Title = "BSc CS",
            Specialization = "Computer Science",
            Degree = "bachelor",
        };
        var cmd2 = new CreateEducationCommand
        {
            Title = "MSc AI",
            Specialization = "Artificial Intelligence",
            Degree = "master",
        };

        var r1 = await PostEducationAsync(client, authCookie, cmd1);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        var r2 = await PostEducationAsync(client, authCookie, cmd2);
        r2.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await GetEducationsAsync(client, authCookie);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(2);
        result.HasNext.Should().BeFalse();
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Title).Should().Contain(["BSc CS", "MSc AI"]);
    }

    private static async Task<HttpResponseMessage> GetEducationAsync(
        HttpClient client,
        string authCookie,
        Guid id
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/educations/{id}");
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PatchEducationAsync(
        HttpClient client,
        string authCookie,
        Guid id,
        object payload
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/educations/{id}")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> DeleteEducationAsync(
        HttpClient client,
        string authCookie,
        Guid id
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/educations/{id}");
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    // --- GET /api/v1/educations/{id} ---

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/educations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_OwnedRecord_Returns200WithAllFields()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var createResponse = await PostEducationAsync(client, authCookie, NewEducationCommand());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        var response = await GetEducationAsync(client, authCookie, created!.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);
        result.Should().NotBeNull();
        // AC2: Verify all seven required fields are returned
        result!.Id.Should().Be(created.Id);
        result.UserId.Should().NotBeEmpty();
        result.Title.Should().Be(NewEducationCommand().Title);
        result.Specialization.Should().Be(NewEducationCommand().Specialization);
        result.Degree.Should().Be("bachelor");
        result.CreatedAt.Should().NotBe(default(DateTime));
        result.UpdatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await GetEducationAsync(client, authCookie, Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_AnotherUsersRecord_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var cookieA = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_a_"));
        var cookieB = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_b_"));

        var createResponse = await PostEducationAsync(client, cookieA, NewEducationCommand());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        // User B attempts to GET user A's record — must get 404, not 403
        var response = await GetEducationAsync(client, cookieB, created!.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- PATCH /api/v1/educations/{id} ---

    [Fact]
    public async Task Patch_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync($"/api/v1/educations/{Guid.NewGuid()}", new { title = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Patch_OwnedRecord_ValidPayload_Returns200WithUpdatedFields()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var createResponse = await PostEducationAsync(client, authCookie, NewEducationCommand());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        var response = await PatchEducationAsync(client, authCookie, created!.Id, new
        {
            title = "Master of Science",
            degree = "master",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Master of Science");
        result.Degree.Should().Be("master");
        // Specialization not updated — must remain unchanged
        result.Specialization.Should().Be(NewEducationCommand().Specialization);
        // AC5: Verify updatedAt is refreshed after patch
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task Patch_AnotherUsersRecord_Returns403()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var cookieA = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_a_"));
        var cookieB = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_b_"));

        var createResponse = await PostEducationAsync(client, cookieA, NewEducationCommand());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        var response = await PatchEducationAsync(client, cookieB, created!.Id, new { title = "Attack" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await PatchEducationAsync(client, authCookie, Guid.NewGuid(), new { title = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_EmptyBody_Returns400()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var createResponse = await PostEducationAsync(client, authCookie, NewEducationCommand());
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        // Send body with no updatable fields
        var response = await PatchEducationAsync(client, authCookie, created!.Id, new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_InvalidDegree_Returns400WithFieldError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var createResponse = await PostEducationAsync(client, authCookie, NewEducationCommand());
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        var response = await PatchEducationAsync(client, authCookie, created!.Id, new { degree = "invalid_degree" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Degree");
    }

    // --- DELETE /api/v1/educations/{id} ---

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/educations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_OwnedRecord_Returns204()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var createResponse = await PostEducationAsync(client, authCookie, NewEducationCommand());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        var response = await DeleteEducationAsync(client, authCookie, created!.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_OwnedRecord_RecordNoLongerInList()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var createResponse = await PostEducationAsync(client, authCookie, NewEducationCommand());
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        await DeleteEducationAsync(client, authCookie, created!.Id);

        var listResponse = await GetEducationsAsync(client, authCookie);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        list!.TotalCount.Should().Be(0);
        list.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_OwnedRecord_GetReturns404After()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);
        var createResponse = await PostEducationAsync(client, authCookie, NewEducationCommand());
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        await DeleteEducationAsync(client, authCookie, created!.Id);

        var getResponse = await GetEducationAsync(client, authCookie, created.Id);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AnotherUsersRecord_Returns403()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var cookieA = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_a_"));
        var cookieB = await CreateUserAndGetCookieAsync(client, NewUserCommand("user_b_"));

        var createResponse = await PostEducationAsync(client, cookieA, NewEducationCommand());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<EducationResultDto>(JsonOptions);

        var response = await DeleteEducationAsync(client, cookieB, created!.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await DeleteEducationAsync(client, authCookie, Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Minimal DTO for deserialising PagedResult envelope from the API
    private class PagedResultDto
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public bool HasNext { get; set; }
        public Guid? LastSeenId { get; set; }
        public DateTime? LastSeenUpdatedAt { get; set; }
        public List<EducationResultDto> Items { get; set; } = [];
    }

    private class EducationResultDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Specialization { get; set; } = null!;
        public string Degree { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
