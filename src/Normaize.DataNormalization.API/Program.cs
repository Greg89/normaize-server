using Normaize.DataNormalization.Infrastructure;
using Normaize.DataNormalization.Application;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure environment
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
builder.Environment.EnvironmentName = "Development";

// Configure Serilog for simple console logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add basic services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger without authentication complexity
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Normaize Data Normalization API",
        Version = "v1",
        Description = "Clean DDD Architecture API for data normalization operations."
    });
});

// Add CORS for development testing
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add application layers with error handling
if (!builder.Configuration.GetValue<bool>("SkipInfrastructureRegistration"))
{
    try
    {
        builder.Services.AddDataNormalizationInfrastructure(builder.Configuration);
        Console.WriteLine("✓ Infrastructure services registered successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Warning: Infrastructure registration failed: {ex.Message}");
        Console.WriteLine("API will run in limited mode");
    }
}
else
{
    Console.WriteLine("⚠ Skipping infrastructure registration (test mode)");
}

// Build the application
var app = builder.Build();

// Configure URLs
app.Urls.Add("http://localhost:5001");

Console.WriteLine("🚀 Starting Normaize Data Normalization API...");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🌐 URL: http://localhost:5001");

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Normaize API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger at root
    c.DocumentTitle = "Normaize API Documentation";
});

app.UseCors("AllowAll");
app.MapControllers();

// Health check endpoints with detailed responses
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                tags = e.Value.Tags,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        }, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await context.Response.WriteAsync(result);
    }
});

// Separate ready/live endpoints for Kubernetes-style health checks
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Always healthy if app is running
});

Console.WriteLine("✅ Starting web server...");

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }