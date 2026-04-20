using System.Net;
using FluentAssertions;
using JobNecto.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JobNecto.Tests.API;

/// <summary>
/// Integration tests for the CORS "Frontend" policy.
/// Exercises OPTIONS preflight and actual-request origin handling without a real database.
/// </summary>
public class CorsFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    internal const string TestOrigin = "http://localhost:5173";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override the allowed origins so the test is self-contained and reproducible.
        builder.UseSetting("Cors:AllowedOrigins:0", TestOrigin);
        builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=testing;Username=test;Password=test");
        builder.UseEnvironment("Production");
    }
}

/// <summary>
/// Verifies issue #37: CORS "Frontend" policy returns correct preflight headers for allowed and disallowed origins.
/// </summary>
public class CorsTests : IClassFixture<CorsFactory>
{
    private const string AllowedOrigin = CorsFactory.TestOrigin;
    private const string DisallowedOrigin = "http://evil.example.com";

    private readonly HttpClient _client;

    /// <summary>
    /// Initialises the test class with a pre-built <see cref="CorsFactory"/> host.
    /// </summary>
    public CorsTests(CorsFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// An OPTIONS preflight from the configured allowed origin must respond with 204 and the
    /// <c>Access-Control-Allow-Origin</c> header set to that origin.
    /// </summary>
    [Fact]
    public async Task Options_Preflight_AllowedOrigin_Returns204WithCorsHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/openapi/v1.json");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(AllowedOrigin);
    }

    /// <summary>
    /// A simple GET from an allowed origin must carry the <c>Access-Control-Allow-Origin</c> response header.
    /// </summary>
    [Fact]
    public async Task Get_AllowedOrigin_ResponseContainsCorsHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await _client.SendAsync(request);

        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(AllowedOrigin);
    }

    /// <summary>
    /// A request from a disallowed origin must NOT receive the <c>Access-Control-Allow-Origin</c> header,
    /// i.e. the browser will block the response.
    /// </summary>
    [Fact]
    public async Task Get_DisallowedOrigin_ResponseDoesNotContainCorsHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add("Origin", DisallowedOrigin);

        var response = await _client.SendAsync(request);

        response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }
}
