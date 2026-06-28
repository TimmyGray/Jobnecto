using FluentAssertions;
using JobNecto.API.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API;

public class AuthenticationCollectionExtensionsTests
{
    private const string ValidSecret = "this-is-a-sufficiently-long-secret-key!!";

    private static IConfiguration Config(string? secret, string? issuer, string? audience)
    {
        var values = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = secret,
            ["JwtSettings:Issuer"] = issuer,
            ["JwtSettings:Audience"] = audience,
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void AddJwtAuthentication_WithValidConfig_RegistersAuthentication()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddJwtAuthentication(Config(ValidSecret, "jobnecto", "jobnecto-clients"));

        act.Should().NotThrow();
        services.Should().Contain(d => d.ServiceType.Name.Contains("IAuthenticationService"));
    }

    [Theory]
    [InlineData(null, "SecretKey")]
    [InlineData("", "SecretKey")]
    [InlineData("too-short", "at least 32")]
    public void AddJwtAuthentication_WithMissingOrShortSecret_Throws(string? secret, string expectedFragment)
    {
        var services = new ServiceCollection();

        var act = () => services.AddJwtAuthentication(Config(secret, "issuer", "audience"));

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(expectedFragment);
    }

    [Fact]
    public void AddJwtAuthentication_WithMissingIssuer_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddJwtAuthentication(Config(ValidSecret, null, "audience"));

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Issuer");
    }

    [Fact]
    public void AddJwtAuthentication_WithMissingAudience_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddJwtAuthentication(Config(ValidSecret, "issuer", null));

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Audience");
    }
}
