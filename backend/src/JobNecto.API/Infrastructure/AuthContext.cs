using System.Security.Claims;

namespace JobNecto.API.Infrastructure;

/// <summary>
/// Provides extension methods for extracting authentication context from HTTP requests.
/// </summary>
public static class AuthContext
{
    /// <summary>
    /// Extracts the current user ID from the JWT claims in the HTTP context.
    /// Checks for 'sub' claim first (standard JWT subject), falls back to 'userId' claim.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>The user ID from claims, or null if no user ID claim is found.</returns>
    public static string? GetCurrentUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) 
            ?? context.User.FindFirst("sub") 
            ?? context.User.FindFirst("userId");
        
        return userIdClaim?.Value;
    }
}
