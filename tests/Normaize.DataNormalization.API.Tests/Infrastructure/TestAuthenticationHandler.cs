using System.Security.Claims;

namespace Normaize.DataNormalization.API.Tests.Infrastructure;

/// <summary>
/// Default authentication handler for integration tests.
/// Reads identity from X-Test-* headers and falls back to deterministic defaults.
/// </summary>
public sealed class TestAuthenticationHandler(
    Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
    Microsoft.Extensions.Logging.ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-Test-User-Id"].FirstOrDefault() ?? "test-user-id";
        var email = Request.Headers["X-Test-User-Email"].FirstOrDefault() ?? "test@example.com";
        var name = Request.Headers["X-Test-User-Name"].FirstOrDefault() ?? "Test User";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("email", email),
            new Claim(ClaimTypes.Name, name),
            new Claim("name", name),
            new Claim("email_verified", "true")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
