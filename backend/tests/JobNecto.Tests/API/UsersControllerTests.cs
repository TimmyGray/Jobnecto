using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.API.Contracts.Auth;
using JobNecto.Application.Users;
using JobNecto.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API;

public class UsersControllerTests
{
    [Fact]
    public async Task Create_ValidUser_Returns201AndSetsCookie()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
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
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
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
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
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
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
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
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/users/token/refresh", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidAuthCookie_Returns200AndNoSensitiveFields()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var command = new CreateUserCommand
        {
            LoginName = "profile" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", command);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResult>();
        createdUser.Should().NotBeNull();

        var authCookie = createResponse.Headers
            .GetValues("Set-Cookie")
            .Select(header => header
                .Split(';', StringSplitOptions.TrimEntries)
                .FirstOrDefault(part => part.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(cookie => !string.IsNullOrWhiteSpace(cookie));

        authCookie.Should().NotBeNullOrWhiteSpace();

        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        getRequest.Headers.TryAddWithoutValidation("Cookie", authCookie);

        var getResponse = await client.SendAsync(getRequest);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadAsStringAsync();
        var lowerBody = body.ToLowerInvariant();
        lowerBody.Should().NotContain("password");
        lowerBody.Should().NotContain("hash");

        var profile = JsonSerializer.Deserialize<GetCurrentUserResult>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        profile.Should().NotBeNull();
        profile!.Id.Should().Be(createdUser!.Id);
        profile.LoginName.Should().Be(command.LoginName);
        profile.Email.Should().Be(command.Email);
        profile.Phone.Should().BeNull();
        profile.Location.Should().BeNull();
        profile.About.Should().BeNull();
        profile.Avatar.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUser_WhenTokenUserDoesNotExist_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var command = new CreateUserCommand
        {
            LoginName = "missingprofile" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", command);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResult>();
        createdUser.Should().NotBeNull();

        var authCookie = createResponse.Headers
            .GetValues("Set-Cookie")
            .Select(header => header
                .Split(';', StringSplitOptions.TrimEntries)
                .FirstOrDefault(part => part.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(cookie => !string.IsNullOrWhiteSpace(cookie));

        authCookie.Should().NotBeNullOrWhiteSpace();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == createdUser!.Id);
            user.Should().NotBeNull();

            dbContext.Users.Remove(user!);
            await dbContext.SaveChangesAsync();
        }

        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        getRequest.Headers.TryAddWithoutValidation("Cookie", authCookie);

        var getResponse = await client.SendAsync(getRequest);

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}