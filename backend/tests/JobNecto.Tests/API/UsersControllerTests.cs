using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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
}