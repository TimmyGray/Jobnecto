namespace JobNecto.Application.Interfaces;

/// <summary>
/// Produces signed JWT tokens from a user ID.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT string for the given user ID.
    /// The token contains at minimum:
    /// <list type="bullet">
    ///   <item><see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/> — <c>userId</c> as a GUID string</item>
    ///   <item><c>sub</c> — <c>userId</c> as a GUID string</item>
    ///   <item><c>userId</c> — <c>userId</c> as a GUID string (compatibility with <c>AuthContext.GetCurrentUserId()</c>)</item>
    /// </list>
    /// </summary>
    /// <param name="userId">The user ID as a string.</param>
    /// <returns>A signed JWT string.</returns>
    Task<string> GenerateTokenAsync(string userId);
}
