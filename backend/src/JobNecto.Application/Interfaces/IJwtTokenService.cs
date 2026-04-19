namespace JobNecto.Application.Interfaces;

/// <summary>
/// Produces signed JWT tokens from a persisted <see cref="User"/> entity.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT string for the given user.
    /// The token contains at minimum:
    /// <list type="bullet">
    ///   <item><see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/> — <c>user.Id</c> as a GUID string</item>
    ///   <item><c>sub</c> — <c>user.Id</c> as a GUID string</item>
    ///   <item><c>userId</c> — <c>user.Id</c> as a GUID string (compatibility with <c>AuthContext.GetCurrentUserId()</c>)</item>
    /// </list>
    /// </summary>
    /// <param name="user">The user for whom the token is generated.</param>
    /// <returns>A signed JWT string.</returns>
    string GenerateToken(User user);
}
