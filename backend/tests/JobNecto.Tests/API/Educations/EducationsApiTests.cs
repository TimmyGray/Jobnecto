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

    private static CreateUserCommand NewUserCommand(string prefix = "education_user") => new()
    {
        LoginName = prefix + Guid.NewGuid().ToString("N")[..8],
        Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
        Password = "Password123!",
    };

    private static CreateEducationCommand NewEducationCommand() => new()
    {
        Title = "Bachelor of Science",
        Specialization = "Computer Science",
        Degree = "bachelor",
    };

    private static async Task<string> CreateUserAndGetCookieAsync(HttpClient client, CreateUserCommand? command = null)
    {
        command ??= NewUserCommand();

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var authCookie = response.Headers
            .GetValues("Set-Cookie")
            .Select(x => x.Split(';', StringSplitOptions.TrimEntries)
                .FirstOrDefault(y => y.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase)))
            .First(x => !string.IsNullOrWhiteSpace(x));

        authCookie.Should().NotBeNullOrWhiteSpace();
        return authCookie!;
    }

    private static async Task<HttpResponseMessage> PostEducationAsync(HttpClient client, string authCookie, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/educations")
        {
            Content = JsonContent.Create(payload)
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
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

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
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await PostEducationAsync(client, authCookie, new
        {
            specialization = "Computer Science",
            degree = "master"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Title");
    }

    [Fact]
    public async Task Create_InvalidDegree_Returns400WithFieldLevelError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var authCookie = await CreateUserAndGetCookieAsync(client);

        var response = await PostEducationAsync(client, authCookie, new
        {
            title = "Master of Science",
            specialization = "Computer Science",
            degree = "invalid_degree"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Degree");
    }
}