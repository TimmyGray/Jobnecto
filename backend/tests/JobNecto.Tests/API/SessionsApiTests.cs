using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using JobNecto.API.Contracts.Auth;
using JobNecto.Application.Users;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API;

public class SessionsApiTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task SignIn_ValidCredentials_Returns200AndSetsCookieAndUserFields()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (created, _) = await RegisterUserAsync(client, "signin-happy");

        var response = await client.PostAsJsonAsync("/api/v1/users/sessions", new { identifier = created.Email, password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.Should().ContainKey("Set-Cookie");
        var cookieHeader = response.Headers.GetValues("Set-Cookie").First();
        cookieHeader.Should().Contain("auth-token=");
        cookieHeader.ToLowerInvariant().Should().Contain("httponly");
        cookieHeader.ToLowerInvariant().Should().Contain("samesite=strict");

        var body = await response.Content.ReadFromJsonAsync<SignInResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.LoginName.Should().Be(created.LoginName);
        body.Email.Should().Be(created.Email);
    }

    [Fact]
    public async Task SignIn_ByLoginName_Succeeds()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (created, _) = await RegisterUserAsync(client, "signin-login");

        var response = await client.PostAsJsonAsync("/api/v1/users/sessions", new { identifier = created.LoginName, password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SignIn_BearerTransport_AccessTokenNonEmpty()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (created, _) = await RegisterUserAsync(client, "signin-bearer");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/sessions")
        {
            Content = JsonContent.Create(new { identifier = created.Email, password = Password })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "irrelevant-for-sign-in");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SignInResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignIn_CookieTransport_AccessTokenEmpty()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (created, _) = await RegisterUserAsync(client, "signin-cookie");

        var response = await client.PostAsJsonAsync("/api/v1/users/sessions", new { identifier = created.Email, password = Password });

        var body = await response.Content.ReadFromJsonAsync<SignInResponse>();
        body!.AccessToken.Should().BeEmpty();
    }

    [Fact]
    public async Task SignIn_UnknownIdentifierAndWrongPassword_ProduceByteIdenticalResponses()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (created, _) = await RegisterUserAsync(client, "signin-antienum");

        var unknownResponse = await client.PostAsJsonAsync(
            "/api/v1/users/sessions",
            new { identifier = "no-such-user-" + Guid.NewGuid().ToString("N"), password = Password });
        var wrongPasswordResponse = await client.PostAsJsonAsync(
            "/api/v1/users/sessions",
            new { identifier = created.Email, password = "TotallyWrongPassword!" });

        unknownResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrongPasswordResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var unknownBody = await unknownResponse.Content.ReadAsStringAsync();
        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadAsStringAsync();

        unknownBody.Should().Be(wrongPasswordBody);
    }

    [Fact]
    public async Task SignIn_SoftDeletedUser_Returns401WithSameBodyAsUnknownIdentifier()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (created, _) = await RegisterUserAsync(client, "signin-softdeleted");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == created.Id);
            user.Should().NotBeNull();
            user!.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        var softDeletedResponse = await client.PostAsJsonAsync(
            "/api/v1/users/sessions",
            new { identifier = created.Email, password = Password });
        var unknownResponse = await client.PostAsJsonAsync(
            "/api/v1/users/sessions",
            new { identifier = "no-such-user-" + Guid.NewGuid().ToString("N"), password = Password });

        softDeletedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var softDeletedBody = await softDeletedResponse.Content.ReadAsStringAsync();
        var unknownBody = await unknownResponse.Content.ReadAsStringAsync();
        softDeletedBody.Should().Be(unknownBody);
    }

    [Fact]
    public async Task SignIn_EmptyIdentifier_Returns400WithErrors()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/sessions", new { identifier = "", password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("errors");
    }

    [Fact]
    public async Task SignIn_EmptyPassword_Returns400WithErrors()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/sessions", new { identifier = "someone", password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("errors");
    }

    [Fact]
    public async Task SignIn_IdentifierFieldOmittedEntirely_Returns400WithErrors()
    {
        // Distinct from the empty-string case: model binding leaves Identifier null here,
        // exercising the `command.Identifier ?? string.Empty` fallback used for the lockout key.
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/sessions", new { password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("errors");
    }

    [Fact]
    public async Task SignIn_FiveFailuresThenSixth_Returns429WithRetryAfterHeader()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (created, _) = await RegisterUserAsync(client, "signin-lockout");

        for (var i = 0; i < 5; i++)
        {
            var failResponse = await client.PostAsJsonAsync(
                "/api/v1/users/sessions",
                new { identifier = created.Email, password = "WrongPassword!" });
            failResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var lockedResponse = await client.PostAsJsonAsync(
            "/api/v1/users/sessions",
            new { identifier = created.Email, password = "WrongPassword!" });

        lockedResponse.StatusCode.Should().Be((HttpStatusCode)429);
        lockedResponse.Headers.Should().ContainKey("Retry-After");
        var retryAfterSeconds = int.Parse(lockedResponse.Headers.GetValues("Retry-After").First());
        retryAfterSeconds.Should().BePositive();
    }

    [Fact]
    public async Task SignIn_LockedOut_WithCorrectPassword_StillReturns429()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (created, _) = await RegisterUserAsync(client, "signin-lockout-correct");

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync(
                "/api/v1/users/sessions",
                new { identifier = created.Email, password = "WrongPassword!" });
        }

        var response = await client.PostAsJsonAsync(
            "/api/v1/users/sessions",
            new { identifier = created.Email, password = Password });

        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task SignIn_SuccessResetsFailureCounter()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (created, _) = await RegisterUserAsync(client, "signin-reset");

        for (var i = 0; i < 4; i++)
        {
            await client.PostAsJsonAsync(
                "/api/v1/users/sessions",
                new { identifier = created.Email, password = "WrongPassword!" });
        }

        var successResponse = await client.PostAsJsonAsync(
            "/api/v1/users/sessions",
            new { identifier = created.Email, password = Password });
        successResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        for (var i = 0; i < 4; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/users/sessions",
                new { identifier = created.Email, password = "WrongPassword!" });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task SignIn_ProducesResponseType_DeclaresAllReachableStatuses()
    {
        var actionType = typeof(JobNecto.API.Controllers.UsersController);
        var method = actionType.GetMethod(
            "SignIn",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        method.Should().NotBeNull();

        var attributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute), false)
            .Cast<Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();

        attributes.Should().Contain(200);
        attributes.Should().Contain(400);
        attributes.Should().Contain(401);
        attributes.Should().Contain(429);
    }

    private static async Task<(CreateUserResult user, string cookie)> RegisterUserAsync(HttpClient client, string prefix)
    {
        var command = new CreateUserCommand
        {
            LoginName = prefix.Replace('-', '_') + "_" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = Password
        };

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateUserResult>();
        created.Should().NotBeNull();

        var cookie = response.Headers
            .GetValues("Set-Cookie")
            .Select(header => header
                .Split(';', StringSplitOptions.TrimEntries)
                .FirstOrDefault(part => part.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        cookie.Should().NotBeNullOrWhiteSpace();

        return (created!, cookie!);
    }
}
