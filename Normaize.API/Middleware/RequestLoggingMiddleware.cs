using Microsoft.AspNetCore.Http;
using Normaize.API.Services;
using System.Diagnostics;

namespace Normaize.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IStructuredLoggingService _loggingService;

    public RequestLoggingMiddleware(RequestDelegate next, IStructuredLoggingService loggingService)
    {
        _next = next;
        _loggingService = loggingService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        
        // Get user info from Auth0 middleware
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Items["UserId"]?.ToString();

        try
        {
            // Log request start
            _loggingService.LogRequestStart(method, path, userId);

            // Process the request
            await _next(context);

            // Log request completion
            stopwatch.Stop();
            _loggingService.LogRequestEnd(method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Log the exception with full context
            _loggingService.LogException(ex, $"Request processing failed: {method} {path}");
            
            // Re-throw to let the global exception handler deal with it
            throw;
        }
    }
} 