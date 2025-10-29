using System.Security.Claims;

namespace Normaize.DataNormalization.API.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to work with Auth0 JWT tokens
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the JWT token claims
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <returns>The user ID from the token</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID is not found in token</exception>
    public static string GetUserId(this ClaimsPrincipal user)
    {
        // Get user ID from JWT token (Auth0 sub claim)
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value
                    ?? user.FindFirst("nameid")?.Value
                    ?? user.FindFirst("user_id")?.Value
                    ?? throw new UnauthorizedAccessException("User ID not found in token");

        return userId;
    }

    /// <summary>
    /// Gets the user ID from the JWT token claims with a fallback for client credentials or testing
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <param name="fallbackUserId">Optional fallback user ID for client credentials tokens or testing</param>
    /// <returns>The user ID from the token or fallback</returns>
    public static string GetUserIdOrDefault(this ClaimsPrincipal user, string fallbackUserId = "test-user-id")
    {
        try
        {
            return user.GetUserId();
        }
        catch (UnauthorizedAccessException)
        {
            // Check if this is a client credentials token (ends with @clients)
            var subClaim = user.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim) && subClaim.EndsWith("@clients"))
            {
                return fallbackUserId;
            }

            // For test scenarios or unauthenticated contexts, return fallback
            return fallbackUserId;
        }
    }

    /// <summary>
    /// Checks if the current token is a client credentials token
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <returns>True if the token is a client credentials token</returns>
    public static bool IsClientCredentialsToken(this ClaimsPrincipal user)
    {
        var subClaim = user.FindFirst("sub")?.Value;
        return !string.IsNullOrEmpty(subClaim) && subClaim.EndsWith("@clients");
    }

    /// <summary>
    /// Gets the grant type from the JWT token
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <returns>The grant type or null if not found</returns>
    public static string? GetGrantType(this ClaimsPrincipal user)
    {
        return user.FindFirst("gty")?.Value;
    }

    /// <summary>
    /// Gets the user email from the JWT token claims
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <returns>The user email or null if not found</returns>
    public static string? GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
               ?? user.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the user name from the JWT token claims
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <returns>The user name or null if not found</returns>
    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value
               ?? user.FindFirst("name")?.Value
               ?? user.FindFirst("preferred_username")?.Value;
    }

    /// <summary>
    /// Gets the user's picture/avatar URL from the JWT token claims
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <returns>The picture URL or null if not found</returns>
    public static string? GetUserPicture(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Uri)?.Value
               ?? user.FindFirst("picture")?.Value;
    }

    /// <summary>
    /// Checks if the user's email is verified
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <returns>True if email is verified, false otherwise</returns>
    public static bool IsEmailVerified(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("email_verified")?.Value;
        return bool.TryParse(claim, out var verified) && verified;
    }
}
