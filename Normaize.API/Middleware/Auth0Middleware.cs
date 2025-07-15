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
            
            // Extract user information from JWT token
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? context.User.FindFirst("sub")?.Value;
                var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var name = context.User.FindFirst(ClaimTypes.Name)?.Value;

                // Log successful authentication
                logger.LogDebug("User authenticated: UserId={UserId}, Email={Email}, Name={Name}", 
                    userId, email, name);

                // Add user info to context for use in controllers
                context.Items["UserId"] = userId;
                context.Items["UserEmail"] = email;
                context.Items["UserName"] = name;
            }
            else
            {
                logger.LogDebug("Request not authenticated for path: {Path}", context.Request.Path);
            }

            await next();
        });
    }
} 