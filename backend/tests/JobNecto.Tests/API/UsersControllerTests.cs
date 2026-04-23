using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JobNecto.API.Contracts.Auth;
using JobNecto.Application.Users;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JobNecto.Tests.API;

public class UsersControllerTests : IClassFixture<JobNectoApiFactory>
{
    private readonly JobNectoApiFactory _factory;

    public UsersControllerTests(JobNectoApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ValidUser_Returns201AndSetsCookie()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var command = new CreateUserCommand
        {
            LoginName = "test" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!"
        };

        var response = await client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location?.ToString().Should().Be("/api/v1/users/me");

        var result = await response.Content.ReadFromJsonAsync<CreateUserResult>();
        result.Should().NotBeNull();
        result!.LoginName.Should().Be(command.LoginName);

        response.Headers.Should().ContainKey("Set-Cookie");
        var cookieHeader = response.Headers.GetValues("Set-Cookie").First();
        cookieHeader.Should().Contain("auth-token=");
        cookieHeader.ToLower().Should().Contain("httponly");
        cookieHeader.ToLower().Should().Contain("samesite=strict");
        cookieHeader.ToLower().Should().Contain("secure");
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400()
    {
        var client = _factory.CreateClient();
        var command = new CreateUserCommand
        {
            LoginName = "sh",
            Email = "invalid-email",
            Password = "short"
        };

        var response = await client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_WithBearerTransport_Returns200AndRenewsCookieAndBodyToken()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var command = new CreateUserCommand
        {
            LoginName = "refresh" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", command);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var originalCookie = createResponse.Headers.GetValues("Set-Cookie").First();
        var originalToken = originalCookie
            .Split(';', StringSplitOptions.TrimEntries)
            .First(x => x.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase))
            .Substring("auth-token=".Length);

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/token/refresh");
        refreshRequest.Headers.Authorization = new("Bearer", originalToken);

        var refreshResponse = await client.SendAsync(refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshResponse.Headers.Should().ContainKey("Set-Cookie");

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshAccessTokenResult>();
        refreshResult.Should().NotBeNull();
        refreshResult!.TokenType.Should().Be("Bearer");
        refreshResult.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshResult.RenewalPolicy.Should().Contain("token/refresh");
    }

    [Fact]
    public async Task RefreshToken_WithCookieTransport_Returns200AndRenewsCookieWithoutBodyToken()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var command = new CreateUserCommand
        {
            LoginName = "refreshcookie" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", command);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var cookieToken = createResponse.Headers
            .GetValues("Set-Cookie")
            .First()
            .Split(';', StringSplitOptions.TrimEntries)
            .First(x => x.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase));

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/token/refresh");
        refreshRequest.Headers.TryAddWithoutValidation("Cookie", cookieToken);

        var refreshResponse = await client.SendAsync(refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshResponse.Headers.Should().ContainKey("Set-Cookie");

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshAccessTokenResult>();
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().BeEmpty();
        refreshResult.TokenType.Should().Be("Bearer");
        refreshResult.RenewalPolicy.Should().Contain("token/refresh");
    }

    [Fact]
    public async Task RefreshToken_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/users/token/refresh", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}