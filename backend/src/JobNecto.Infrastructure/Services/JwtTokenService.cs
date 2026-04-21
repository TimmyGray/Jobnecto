using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobNecto.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace JobNecto.Infrastructure.Services;

/// <summary>
/// Generates signed JWT tokens for authenticated users using the JwtSettings configuration section.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    /// <summary>
    /// Initialises the service using <c>JwtSettings</c> from application configuration.
    /// </summary>
    /// <param name="configuration">Application configuration that must contain a valid <c>JwtSettings</c> section.</param>
    public JwtTokenService(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        _secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is required.");
        _issuer = jwtSettings["Issuer"]
            ?? throw new InvalidOperationException("JwtSettings:Issuer is required.");
        _audience = jwtSettings["Audience"]
            ?? throw new InvalidOperationException("JwtSettings:Audience is required.");
        _expirationMinutes = int.TryParse(jwtSettings["ExpirationMinutes"], out var exp) ? exp : 60;
    }

    /// <inheritdoc />
    public string GenerateToken(User user)
    {
        var userIdString = user.Id.ToString();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userIdString),
            new Claim(JwtRegisteredClaimNames.Sub, userIdString),
            new Claim("userId", userIdString),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
