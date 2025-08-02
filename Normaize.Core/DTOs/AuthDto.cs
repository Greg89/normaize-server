using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

/// <summary>
/// Data Transfer Object for login requests
/// </summary>
/// <remarks>
/// This DTO represents a login request containing username and password credentials.
/// Used by the AuthController for authentication with Auth0. The credentials are
/// used to obtain an access token through Auth0's OAuth2 flow.
/// </remarks>
public class LoginRequestDto
{
    /// <summary>
    /// Gets or sets the username for authentication
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for Auth0 token responses
/// </summary>
/// <remarks>
/// This DTO represents the response from Auth0's OAuth2 token endpoint.
/// It contains the access token, expiration time, and token type returned
/// by Auth0 after successful authentication.
/// </remarks>
public class Auth0TokenResponseDto
{
    /// <summary>
    /// Gets or sets the access token from Auth0
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration time in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the token type (typically "Bearer")
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for token responses
/// </summary>
/// <remarks>
/// This DTO represents the standardized token response returned by the API.
/// It provides a consistent format for token information including the access
/// token, expiration time, and token type for client applications.
/// </remarks>
public class TokenResponseDto
{
    /// <summary>
    /// Gets or sets the access token for API authentication
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration time in seconds
    /// </summary>
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the token type (typically "Bearer")
    /// </summary>
    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for authentication test responses
/// </summary>
/// <remarks>
/// This DTO represents the response from authentication test endpoints.
/// It provides detailed information about the current authentication state,
/// including user ID, grant type, claims, and authentication status.
/// </remarks>
public class AuthTestResponseDto
{
    /// <summary>
    /// Gets or sets the authentication status message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID extracted from claims
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 grant type used for authentication
    /// </summary>
    [JsonPropertyName("grantType")]
    public string? GrantType { get; set; }

    /// <summary>
    /// Gets or sets whether the token is a client credentials token
    /// </summary>
    [JsonPropertyName("isClientCredentials")]
    public bool IsClientCredentials { get; set; }

    /// <summary>
    /// Gets or sets the user claims from the JWT token
    /// </summary>
    [JsonPropertyName("claims")]
    public List<ClaimDto> Claims { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp of the authentication test
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Data Transfer Object for individual claims
/// </summary>
/// <remarks>
/// This DTO represents a single claim from a JWT token.
/// Used to provide structured access to claim information
/// in authentication test responses.
/// </remarks>
public class ClaimDto
{
    /// <summary>
    /// Gets or sets the claim type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the claim value
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
} 