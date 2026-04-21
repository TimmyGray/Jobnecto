using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using JobNecto.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace JobNecto.Tests.API;

/// <summary>
/// Startup filter to add test endpoints with authorization requirements.
/// </summary>
public class AuthenticationTestEndpointsStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            next(app);

            // Add test endpoints using the routing that's already configured in Program.cs
            app.UseEndpoints(endpoints =>
            {
                // Protected endpoint that requires authentication
                endpoints.MapGet("/test-protected", () => "Success")
                    .WithName("ProtectedTest")
                    .RequireAuthorization();

                // Unprotected endpoint for comparison
                endpoints.MapGet("/test-public", () => "Success")
                    .WithName("PublicTest");
            });
        };
    }
}

/// <summary>
/// Factory for creating test web application instances with JWT authentication configured.
/// </summary>
public class AuthenticationTestFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    // Use a 64-character key to ensure it's long enough for HMAC-SHA256
    public const string TestSecretKey = "Thisisatestsecretkeyforjwttokentesting1234567890abcdefghijklmn";
    public const string TestIssuer = "JobNecto";
    public const string TestAudience = "JobNecto-API";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        
        // Override configuration 
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var sources = config.Sources.ToList();
            foreach (var source in sources)
            {
                config.Sources.Remove(source);
            }
            
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey", TestSecretKey },
                { "JwtSettings:Issuer", TestIssuer },
                { "JwtSettings:Audience", TestAudience },
                { "JwtSettings:ExpirationMinutes", "60" },
                { "ConnectionStrings:Postgres", "Host=localhost;Database=testing;Username=test;Password=test" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing JWT bearer options and replace with test-configured ones
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = Encoding.ASCII.GetBytes(TestSecretKey);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = true,
                    ValidAudience = TestAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddTransient<IStartupFilter, AuthenticationTestEndpointsStartupFilter>();
        });
    }
}

/// <summary>
/// Tests for JWT bearer token authentication middleware.
/// Verifies that tokens are validated correctly and requests without valid tokens return 401 Unauthorized.
/// </summary>
public class AuthenticationTests : IClassFixture<AuthenticationTestFactory>
{
    private readonly HttpClient _client;

    public AuthenticationTests(AuthenticationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Generates a valid JWT token for testing purposes.
    /// </summary>
    private static string GenerateValidToken(string userId, DateTime? expiresAt = null)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(AuthenticationTestFactory.TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new("userId", userId)
        };

        var token = new JwtSecurityToken(
            issuer: AuthenticationTestFactory.TestIssuer,
            audience: AuthenticationTestFactory.TestAudience,
            claims: claims,
            expires: expiresAt ?? DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates an expired JWT token for testing rejection of expired tokens.
    /// </summary>
    private static string GenerateExpiredToken(string userId)
    {
        return GenerateValidToken(userId, DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ValidJwtToken_Should_Succeed()
    {
        // Arrange
        var userId = "test-user-123";
        var token = GenerateValidToken(userId);
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        request.Headers.Authorization = new("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Valid token should allow access to protected endpoint
        // If this fails with 401, check that token claims match what the endpoint expects
        // The endpoint requires a valid JWT bearer token with issuer/audience validation
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MissingJwtToken_Should_Return_401_On_Protected_Endpoint()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        // No Authorization header

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Without token, protected endpoint should return 401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidJwtToken_Should_Return_401()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        request.Headers.Authorization = new("Bearer", invalidToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Invalid token format should be rejected
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredJwtToken_Should_Return_401()
    {
        // Arrange
        var userId = "test-user-123";
        var expiredToken = GenerateExpiredToken(userId);
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        request.Headers.Authorization = new("Bearer", expiredToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Expired token should be rejected
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void UserIdClaim_Should_Be_Extracted_From_Token()
    {
        // Arrange
        var userId = "test-user-456";
        var token = GenerateValidToken(userId);

        // Act
        var handler = new JwtSecurityTokenHandler();
        var decodedToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        var uidClaim = decodedToken!.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        uidClaim!.Value.Should().Be(userId);

        var subClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == "sub");
        subClaim!.Value.Should().Be(userId);

        var userIdClaimValue = decodedToken.Claims.FirstOrDefault(c => c.Type == "userId");
        userIdClaimValue!.Value.Should().Be(userId);
    }

    [Fact]
    public async Task TokenWithWrongIssuer_Should_Return_401()
    {
        // Arrange
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(AuthenticationTestFactory.TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "WrongIssuer",  // Wrong issuer
            audience: AuthenticationTestFactory.TestAudience,
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") },
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        request.Headers.Authorization = new("Bearer", tokenString);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenWithWrongAudience_Should_Return_401()
    {
        // Arrange
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(AuthenticationTestFactory.TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: AuthenticationTestFactory.TestIssuer,
            audience: "WrongAudience",  // Wrong audience
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") },
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        request.Headers.Authorization = new("Bearer", tokenString);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenSignedWithWrongKey_Should_Return_401()
    {
        // Arrange
        var wrongKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("WrongKeyThatIsDifferentFromTestKey1234567890"));
        var credentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: AuthenticationTestFactory.TestIssuer,
            audience: AuthenticationTestFactory.TestAudience,
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") },
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-protected");
        request.Headers.Authorization = new("Bearer", tokenString);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
