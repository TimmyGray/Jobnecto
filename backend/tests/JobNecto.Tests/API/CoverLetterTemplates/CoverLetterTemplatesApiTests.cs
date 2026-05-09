using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API.CoverLetterTemplates;

public class CoverLetterTemplatesApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static string ValidContent() => new string('a', 50);

    private static CreateUserCommand NewUserCommand(string prefix = "clt_user") =>
        new()
        {
            LoginName = prefix + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
        };

    internal static async Task<string> CreateUserAndGetCookieHelperAsync(
        HttpClient client,
        CreateUserCommand? command = null)
    {
        command ??= NewUserCommand();

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var authCookie = response
            .Headers.GetValues("Set-Cookie")
            .Select(x =>
                x.Split(';', StringSplitOptions.TrimEntries)
                    .FirstOrDefault(y =>
                        y.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase)))
            .First(x => !string.IsNullOrWhiteSpace(x));

        authCookie.Should().NotBeNullOrWhiteSpace();
        return authCookie!;
    }

    internal static async Task<HttpResponseMessage> PostTemplateAsync(
        HttpClient client,
        string authCookie,
        object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cover-letter-templates")
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

        var response = await client.PostAsJsonAsync(
            "/api/v1/cover-letter-templates",
            new CreateCoverLetterTemplateCommand { Name = "Test", Content = ValidContent() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidPayload_Returns201WithLocationAndPayload()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var response = await PostTemplateAsync(client, authCookie, new
        {
            name = "My Cover Letter",
            content = ValidContent(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var locationStr = response.Headers.Location.ToString();
        locationStr.Should().StartWith("/api/v1/cover-letter-templates/");
        var idSegment = locationStr.Split('/').Last();
        Guid.TryParse(idSegment, out _).Should().BeTrue("Location header must end with a valid GUID");

        var result = await response.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Name.Should().Be("My Cover Letter");
        result.Content.Should().Be(ValidContent());
        result.CreatedAt.Should().NotBe(default(DateTime));
        result.UpdatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task Create_ContentLessThan50Chars_Returns400WithFieldLevelError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var response = await PostTemplateAsync(client, authCookie, new
        {
            name = "My Template",
            content = new string('a', 49),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Content");
    }

    [Fact]
    public async Task Create_ContentMoreThan10000Chars_Returns400WithFieldLevelError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var response = await PostTemplateAsync(client, authCookie, new
        {
            name = "My Template",
            content = new string('a', 10001),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Content");
    }

    internal class CoverLetterTemplateResultDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // --- GET /api/v1/cover-letter-templates ---

    private static async Task<HttpResponseMessage> GetTemplatesAsync(
        HttpClient client,
        string authCookie,
        string queryString = "")
    {
        var url = "/api/v1/cover-letter-templates" +
                  (string.IsNullOrEmpty(queryString) ? "" : "?" + queryString);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/cover-letter-templates");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_EmptyLibrary_Returns200WithEmptyResult()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var response = await GetTemplatesAsync(client, authCookie);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(0);
        result.HasNext.Should().BeFalse();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsOnlyCurrentUsersTemplates()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var cookieA = await CreateUserAndGetCookieHelperAsync(client, NewUserCommand("clt_a_"));
        var cookieB = await CreateUserAndGetCookieHelperAsync(client, NewUserCommand("clt_b_"));

        await PostTemplateAsync(client, cookieA, new { name = "User A Template", content = ValidContent() });

        var response = await GetTemplatesAsync(client, cookieB);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task List_ReturnsItemsWithContentPreview()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);
        var longContent = new string('x', 300);

        await PostTemplateAsync(client, authCookie, new { name = "Long Template", content = longContent });

        var response = await GetTemplatesAsync(client, authCookie);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result!.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Name.Should().Be("Long Template");
        item.ContentPreview.Should().NotBeNull();
        item.ContentPreview.Length.Should().Be(200);
        item.ContentPreview.Should().Be(longContent[..200]);
    }

    [Fact]
    public async Task List_SearchMatchesName_ReturnsFilteredResults()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        await PostTemplateAsync(client, authCookie, new { name = "Senior Developer", content = ValidContent() });
        await PostTemplateAsync(client, authCookie, new { name = "Junior Analyst", content = ValidContent() });

        var response = await GetTemplatesAsync(client, authCookie, "search=senior");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result!.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Senior Developer");
    }

    [Fact]
    public async Task List_SearchIsCaseInsensitive()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        await PostTemplateAsync(client, authCookie, new { name = "Senior Developer", content = ValidContent() });

        var response = await GetTemplatesAsync(client, authCookie, "search=SENIOR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result!.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Senior Developer");
    }

    [Fact]
    public async Task List_SearchNoMatch_ReturnsEmpty()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        await PostTemplateAsync(client, authCookie, new { name = "Senior Developer", content = ValidContent() });

        var response = await GetTemplatesAsync(client, authCookie, "search=zzznomatch");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result!.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_SoftDeletedTemplatesNotVisible()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        // Create a template via API
        var createResponse = await PostTemplateAsync(client, authCookie, new
        {
            name = "Deleted Template",
            content = ValidContent(),
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);

        // Soft-delete the template directly via DbContext (story 3.5 DELETE endpoint not yet implemented)
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobNecto.Infrastructure.Persistance.AppDbContext>();
            var template = await db.CoverLetterTemplates
                .IgnoreQueryFilters()
                .SingleAsync(t => t.Id == created!.Id);
            template.IsDeleted = true;
            template.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // List should now return 0 — EF Core global query filter (!IsDeleted) excludes it
        var response = await GetTemplatesAsync(client, authCookie);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto>(JsonOptions);
        result!.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    private class PagedResultDto
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public bool HasNext { get; set; }
        public Guid? LastSeenId { get; set; }
        public DateTime? LastSeenUpdatedAt { get; set; }
        public List<CoverLetterTemplateListItemDto> Items { get; set; } = [];
    }

    private class CoverLetterTemplateListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string ContentPreview { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
