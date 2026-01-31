using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Normaize.DataNormalization.API.Authentication;

/// <summary>
/// Authentication handler used when Auth0 configuration is missing.
/// It intentionally denies authentication so [Authorize] endpoints return 401
/// instead of silently accepting arbitrary bearer tokens.
/// </summary>
public sealed class DenyAllAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.Fail("Authentication is not configured"));
}
