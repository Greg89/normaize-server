using System.Security.Claims;
using Normaize.Core.DTOs;

namespace Normaize.Core.Extensions;

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
                    ?? throw new UnauthorizedAccessException("User ID not found in token");

        return userId;
    }

    /// <summary>
    /// Gets the user ID from the JWT token claims with a fallback for client credentials
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <param name="fallbackUserId">Optional fallback user ID for client credentials tokens</param>
    /// <returns>The user ID from the token or fallback</returns>
    public static string? GetUserIdWithFallback(this ClaimsPrincipal user, string? fallbackUserId = null)
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
                return fallbackUserId ?? "client-credentials-user";
            }

            // For test scenarios or unauthenticated contexts, return null instead of throwing
            return null;
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
               ?? user.FindFirst("name")?.Value;
    }

    /// <summary>
    /// Gets all user information from the JWT token claims in a single call
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the current user context</param>
    /// <returns>ProfileInfoDto containing all available user information</returns>
    public static ProfileInfoDto GetUserInfo(this ClaimsPrincipal user)
    {
        return new ProfileInfoDto
        {
            UserId = user.GetUserId(),
            Email = user.GetUserEmail(),
            Name = user.GetUserName(),
            Picture = user.FindFirst(ClaimTypes.Uri)?.Value 
                     ?? user.FindFirst("picture")?.Value,
            EmailVerified = bool.TryParse(
                user.FindFirst("email_verified")?.Value, out var verified) && verified
        };
    }
}