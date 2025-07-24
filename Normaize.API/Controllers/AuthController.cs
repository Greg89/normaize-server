using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(HttpClient httpClient, ILogger<AuthController> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get a client credentials token from Auth0 for testing (development only)
    /// </summary>
    /// <returns>Auth0 JWT token for API access</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var auth0Domain = Environment.GetEnvironmentVariable("AUTH0_DOMAIN") ?? 
                             Environment.GetEnvironmentVariable("AUTH0_ISSUER")?.Replace("https://", "").Replace("/", "") ?? 
                             "dev-0kihwasowi558bwz.us.auth0.com";
            var clientId = Environment.GetEnvironmentVariable("SWAGGER_AUTH0_CLIENT_ID") ?? "zGQjzMlakCdootkguZtLs4MKCRkL9yPC";
            var clientSecret = Environment.GetEnvironmentVariable("SWAGGER_AUTH0_CLIENT_SECRET") ?? "SjcKzfbVcL_sUlvHhnUFlOkV_pHJGGtNa8pagL7kvEzhfcuJTSFexBiJtBjAs0ph";
            var audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE") ?? "https://api.normaize.com";

            _logger.LogInformation("Auth0 login attempt for user: {Username}", request.Username);

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

            var response = await _httpClient.PostAsync($"https://{auth0Domain}/oauth/token", content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Auth0 login response status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auth0 client credentials failed: {StatusCode}, {Error}", response.StatusCode, responseContent);
                
                // Fallback: Try password grant if client credentials fails
                _logger.LogInformation("Trying password grant as fallback...");
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

                var passwordResponse = await _httpClient.PostAsync($"https://{auth0Domain}/oauth/token", passwordContent);
                var passwordResponseContent = await passwordResponse.Content.ReadAsStringAsync();
                
                if (!passwordResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Auth0 password grant also failed: {StatusCode}, {Error}", passwordResponse.StatusCode, passwordResponseContent);
                    return BadRequest(new { message = "Login failed", error = passwordResponseContent });
                }
                
                responseContent = passwordResponseContent;
            }

            var tokenResponse = JsonSerializer.Deserialize<Auth0TokenResponse>(responseContent);

            if (tokenResponse?.AccessToken == null)
            {
                _logger.LogError("Auth0 login response parsed but AccessToken is null");
                return BadRequest(new { message = "Invalid response from Auth0 - no access token" });
            }

            return Ok(new TokenResponse
            {
                Token = tokenResponse.AccessToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenType = tokenResponse.TokenType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "Internal server error during login" });
        }
    }

    /// <summary>
    /// Test endpoint that requires authentication
    /// </summary>
    /// <returns>Authentication test result</returns>
    [HttpGet("test")]
    [Authorize]
    public ActionResult<object> TestAuth()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        
        return Ok(new
        {
            message = "Authentication successful",
            userId = userId,
            claims = claims,
            timestamp = DateTime.UtcNow
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class Auth0TokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
}

public class TokenResponse
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
} 