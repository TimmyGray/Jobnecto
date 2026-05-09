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

    internal static async Task<HttpResponseMessage> PatchTemplateAsync(
        HttpClient client,
        string authCookie,
        Guid id,
        object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/cover-letter-templates/{id}")
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

    // --- GET /api/v1/cover-letter-templates/{id} ---

    private static async Task<HttpResponseMessage> GetTemplateByIdAsync(
        HttpClient client,
        string authCookie,
        Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/cover-letter-templates/{id}");
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/cover-letter-templates/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_OwnedTemplate_Returns200WithFullContent()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);
        var longContent = new string('b', 300);

        var createResponse = await PostTemplateAsync(client, authCookie, new
        {
            name = "Detail Template",
            content = longContent,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);

        var response = await GetTemplateByIdAsync(client, authCookie, created!.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("Detail Template");
        result.Content.Should().Be(longContent);
        result.Content.Length.Should().Be(300);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var response = await GetTemplateByIdAsync(client, authCookie, Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_AnotherUsersTemplate_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var cookieA = await CreateUserAndGetCookieHelperAsync(client, NewUserCommand("clt_detail_a_"));
        var cookieB = await CreateUserAndGetCookieHelperAsync(client, NewUserCommand("clt_detail_b_"));

        var createResponse = await PostTemplateAsync(client, cookieA, new
        {
            name = "User A Template",
            content = ValidContent(),
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);

        var response = await GetTemplateByIdAsync(client, cookieB, created!.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- PATCH /api/v1/cover-letter-templates/{id} ---

    [Fact]
    public async Task Patch_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/cover-letter-templates/{Guid.NewGuid()}",
            new { name = "Updated Name" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Patch_OwnedTemplate_NameOnly_Returns200AndPreservesContent()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var createResponse = await PostTemplateAsync(client, authCookie, new
        {
            name = "Original Name",
            content = new string('a', 70),
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);

        var response = await PatchTemplateAsync(client, authCookie, created!.Id, new
        {
            name = "Updated Name",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Content.Should().Be(new string('a', 70));
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task Patch_OwnedTemplate_ContentOnly_Returns200AndPreservesName()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var createResponse = await PostTemplateAsync(client, authCookie, new
        {
            name = "Original Name",
            content = new string('a', 70),
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);
        var updatedContent = new string('b', 80);

        var response = await PatchTemplateAsync(client, authCookie, created!.Id, new
        {
            content = updatedContent,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Original Name");
        result.Content.Should().Be(updatedContent);
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task Patch_AnotherUsersTemplate_Returns403()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var cookieA = await CreateUserAndGetCookieHelperAsync(client, NewUserCommand("clt_patch_a_"));
        var cookieB = await CreateUserAndGetCookieHelperAsync(client, NewUserCommand("clt_patch_b_"));

        var createResponse = await PostTemplateAsync(client, cookieA, new
        {
            name = "Owner Template",
            content = ValidContent(),
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);

        var response = await PatchTemplateAsync(client, cookieB, created!.Id, new
        {
            name = "Attack",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var response = await PatchTemplateAsync(client, authCookie, Guid.NewGuid(), new
        {
            name = "Updated",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_InvalidContentBounds_Returns400WithFieldError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var createResponse = await PostTemplateAsync(client, authCookie, new
        {
            name = "Template",
            content = ValidContent(),
        });

        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);

        var response = await PatchTemplateAsync(client, authCookie, created!.Id, new
        {
            content = new string('x', 49),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Content");
    }

    [Fact]
    public async Task Patch_EmptyBody_Returns400()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var createResponse = await PostTemplateAsync(client, authCookie, new
        {
            name = "Template",
            content = ValidContent(),
        });

        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);

        var response = await PatchTemplateAsync(client, authCookie, created!.Id, new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_SoftDeletedTemplate_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieHelperAsync(client);

        var createResponse = await PostTemplateAsync(client, authCookie, new
        {
            name = "To Be Deleted",
            content = ValidContent(),
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CoverLetterTemplateResultDto>(JsonOptions);

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

        var response = await GetTemplateByIdAsync(client, authCookie, created!.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
