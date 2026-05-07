using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Application.Users;
using Microsoft.AspNetCore.Mvc.Testing;

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
}
