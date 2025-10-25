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

// Add health checks
builder.Services.AddHealthChecks();

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
app.MapHealthChecks("/health");

Console.WriteLine("✅ Starting web server...");

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }