using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Normaize.API.Middleware;

public static class Auth0Middleware
{
    public static IApplicationBuilder UseAuth0(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Auth0Middleware");

            // Check if this is an OPTIONS request (CORS preflight)
            if (context.Request.Method == "OPTIONS")
            {
                await next();
                return;
            }

            // Extract user information from JWT token
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? context.User.FindFirst("sub")?.Value;
                var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var name = context.User.FindFirst(ClaimTypes.Name)?.Value;

                // Add user info to context for use in controllers
                context.Items["UserId"] = userId;
                context.Items["UserEmail"] = email;
                context.Items["UserName"] = name;

                logger.LogDebug("User information extracted: UserId={UserId}", userId);
            }
            else
            {
                // Only log warnings for protected endpoints that are not authenticated
                var endpoint = context.GetEndpoint();
                var requiresAuth = endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>() != null;
                
                if (requiresAuth)
                {
                    logger.LogWarning("Unauthenticated request to protected endpoint: {Method} {Path}", 
                        context.Request.Method, context.Request.Path);
                }
            }

            await next();
        });
    }
}