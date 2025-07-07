using Microsoft.AspNetCore.Http;
using Normaize.API.Services;
using System.Diagnostics;

namespace Normaize.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        
        // Get user info from Auth0 middleware
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Items["UserId"]?.ToString();

        // Get logging service from service provider
        var loggingService = context.RequestServices.GetRequiredService<IStructuredLoggingService>();

        try
        {
            // Log request start
            loggingService.LogRequestStart(method, path, userId);

            // Process the request
            await _next(context);

            // Log request completion
            stopwatch.Stop();
            loggingService.LogRequestEnd(method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Log the exception with full context
            loggingService.LogException(ex, $"Request processing failed: {method} {path}");
            
            // Re-throw to let the global exception handler deal with it
            throw;
        }
    }
} 