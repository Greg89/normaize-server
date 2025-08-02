using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.Extensions;
using Normaize.Core.DTOs;
using System.Text;
using System.Text.Json;

namespace Normaize.API.Controllers;

/// <summary>
/// Controller for authentication and authorization functionality
/// </summary>
/// <remarks>
/// This controller provides authentication endpoints for the Normaize API, including
/// Auth0 integration for login and token management. It supports both client credentials
/// and password grant flows for obtaining access tokens, with comprehensive error
/// handling and logging for security monitoring.
/// 
/// Key features:
/// - Auth0 OAuth2 integration for token-based authentication
/// - Support for client credentials and password grant flows
/// - Comprehensive error handling and logging
/// - Authentication test endpoints for debugging
/// - Secure token processing and validation
/// 
/// This controller is typically used for:
/// - User authentication and login
/// - Token management and validation
/// - Authentication testing and debugging
/// - Development and testing authentication flows
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    HttpClient httpClient,
    ILogger<AuthController> logger
) : BaseApiController()
{
    /// <summary>
    /// Authenticates a user and returns an Auth0 access token
    /// </summary>
    /// <param name="request">The login request containing username and password</param>
    /// <returns>
    /// Authentication result with access token, expiration time, and token type
    /// </returns>
    /// <remarks>
    /// This endpoint authenticates users using Auth0's OAuth2 flow. It first attempts
    /// client credentials authentication, and if that fails, falls back to password
    /// grant authentication. The endpoint supports both development and production
    /// environments with appropriate configuration.
    /// 
    /// Authentication flow:
    /// 1. Attempts client credentials grant (simpler for testing)
    /// 2. Falls back to password grant if client credentials fail
    /// 3. Validates Auth0 response and extracts access token
    /// 4. Returns standardized token response
    /// 
    /// Environment variables used:
    /// - AUTH0_DOMAIN: Auth0 domain for authentication
    /// - SWAGGER_AUTH0_CLIENT_ID: Client ID for Auth0 application
    /// - SWAGGER_AUTH0_CLIENT_SECRET: Client secret for Auth0 application
    /// - AUTH0_AUDIENCE: API audience for token validation
    /// 
    /// This endpoint is typically used for:
    /// - User login and authentication
    /// - Obtaining access tokens for API calls
    /// - Development and testing authentication flows
    /// - Integration with client applications
    /// </remarks>
    /// <response code="200">Authentication successful with access token</response>
    /// <response code="400">Invalid credentials or authentication failed</response>
    /// <response code="500">Internal server error during authentication</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var auth0Domain = Environment.GetEnvironmentVariable("AUTH0_DOMAIN") ??
                             Environment.GetEnvironmentVariable("AUTH0_ISSUER")?.Replace("https://", "").Replace("/", "") ??
                             "dev-0kihwasowi558bwz.us.auth0.com";
            var clientId = Environment.GetEnvironmentVariable("SWAGGER_AUTH0_CLIENT_ID") ?? "zGQjzMlakCdootkguZtLs4MKCRkL9yPC";
            var clientSecret = Environment.GetEnvironmentVariable("SWAGGER_AUTH0_CLIENT_SECRET") ?? "SjcKzfbVcL_sUlvHhnUFlOkV_pHJGGtNa8pagL7kvEzhfcuJTSFexBiJtBjAs0ph";
            var audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE") ?? "https://api.normaize.com";

            logger.LogInformation("Auth0 login attempt for user: {Username}", request.Username);

            // Try client credentials first (simpler for testing)
            var tokenRequest = new
            {
                grant_type = "client_credentials",
                client_id = clientId,
                client_secret = clientSecret,
                audience
            };

            var json = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"https://{auth0Domain}/oauth/token", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Auth0 login response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Auth0 client credentials failed: {StatusCode}, {Error}", response.StatusCode, responseContent);

                // Fallback: Try password grant if client credentials fails
                logger.LogInformation("Trying password grant as fallback...");
                var passwordRequest = new
                {
                    grant_type = "password",
                    username = request.Username,
                    password = request.Password,
                    audience,
                    client_id = clientId,
                    client_secret = clientSecret,
                    scope = "openid profile email"
                };

                var passwordJson = JsonSerializer.Serialize(passwordRequest);
                var passwordContent = new StringContent(passwordJson, Encoding.UTF8, "application/json");

                var passwordResponse = await httpClient.PostAsync($"https://{auth0Domain}/oauth/token", passwordContent);
                var passwordResponseContent = await passwordResponse.Content.ReadAsStringAsync();

                if (!passwordResponse.IsSuccessStatusCode)
                {
                    logger.LogWarning("Auth0 password grant also failed: {StatusCode}, {Error}", passwordResponse.StatusCode, passwordResponseContent);
                    return BadRequest<TokenResponseDto>("Login failed");
                }

                responseContent = passwordResponseContent;
            }

            var tokenResponse = JsonSerializer.Deserialize<Auth0TokenResponseDto>(responseContent);

            if (tokenResponse?.AccessToken == null)
            {
                logger.LogError("Auth0 login response parsed but AccessToken is null");
                return BadRequest<TokenResponseDto>("Invalid response from Auth0 - no access token");
            }

            return Success(new TokenResponseDto
            {
                Token = tokenResponse.AccessToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenType = tokenResponse.TokenType
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login");
            return InternalServerError<TokenResponseDto>("Internal server error during login");
        }
    }

    /// <summary>
    /// Tests authentication and returns detailed authentication information
    /// </summary>
    /// <returns>
    /// Detailed authentication test result including user information, claims,
    /// grant type, and authentication status
    /// </returns>
    /// <remarks>
    /// This endpoint tests the current authentication state and returns detailed
    /// information about the authenticated user. It extracts user claims, grant
    /// type, and other authentication details from the JWT token.
    /// 
    /// The endpoint provides:
    /// - User ID extraction from claims
    /// - Grant type identification (client-credentials, password, etc.)
    /// - Client credentials token detection
    /// - Complete list of user claims
    /// - Authentication timestamp
    /// 
    /// This endpoint is typically used for:
    /// - Authentication debugging and testing
    /// - Verifying token validity and claims
    /// - Development and testing authentication flows
    /// - Monitoring authentication state
    /// </remarks>
    /// <response code="200">Authentication test successful with detailed information</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="500">Internal server error during authentication test</response>
    [HttpGet("test")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AuthTestResponseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public ActionResult<ApiResponse<AuthTestResponseDto>> TestAuth()
    {
        try
        {
            var claims = User.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value }).ToList();
            var userId = User.GetUserIdWithFallback();
            var grantType = User.GetGrantType();
            var isClientCredentials = User.IsClientCredentialsToken();

            var authTestResponse = new AuthTestResponseDto
            {
                Message = "Authentication successful",
                UserId = userId,
                GrantType = grantType,
                IsClientCredentials = isClientCredentials,
                Claims = claims,
                Timestamp = DateTime.UtcNow
            };

            return Success(authTestResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during authentication test");
            return InternalServerError<AuthTestResponseDto>("Internal server error during authentication test");
        }
    }
}