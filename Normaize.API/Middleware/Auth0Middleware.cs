using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace Normaize.API.Middleware;

public static class Auth0Middleware
{
    public static IApplicationBuilder UseAuth0(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Extract user information from JWT token
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var name = context.User.FindFirst(ClaimTypes.Name)?.Value;

                // Add user info to context for use in controllers
                context.Items["UserId"] = userId;
                context.Items["UserEmail"] = email;
                context.Items["UserName"] = name;
            }

            await next();
        });
    }
} 