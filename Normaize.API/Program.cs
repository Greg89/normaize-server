using Microsoft.EntityFrameworkCore;
using Normaize.API.Middleware;
using Normaize.Core.Interfaces;
using Normaize.API.Services;
using Normaize.Data;
using Normaize.Data.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using Serilog;
using Serilog.Events;


// Load environment variables from .env file
var currentDir = Directory.GetCurrentDirectory();

// Find the project root directory (where .env file is located)
var projectRoot = currentDir;
while (!File.Exists(Path.Combine(projectRoot, ".env")) && Directory.GetParent(projectRoot) != null)
{
    projectRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
}

var envPath = Path.Combine(projectRoot, ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}
else
{
    Env.Load(); // Fallback to default behavior
}

// Configure Serilog
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL");

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information
    );

// Add Seq sink for non-local environments
if (!string.IsNullOrEmpty(seqUrl) && environment != "Development")
{
    loggerConfiguration.WriteTo.Seq(seqUrl, 
        restrictedToMinimumLevel: LogEventLevel.Information,
        apiKey: Environment.GetEnvironmentVariable("SEQ_API_KEY"));
}

Log.Logger = loggerConfiguration.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for dependency injection
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Normaize API", Version = "v1" });
            // c.EnableAnnotations(); // Commented out as it's not available in this version
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("startup", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Application started successfully"));

// Add JWT Authentication for Auth0
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("AUTH0_ISSUER");
        options.Audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE");
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Database
var connectionString = $"Server={Environment.GetEnvironmentVariable("MYSQLHOST")};Database={Environment.GetEnvironmentVariable("MYSQLDATABASE")};User={Environment.GetEnvironmentVariable("MYSQLUSER")};Password={Environment.GetEnvironmentVariable("MYSQLPASSWORD")};Port={Environment.GetEnvironmentVariable("MYSQLPORT")};CharSet=utf8mb4;AllowLoadLocalInfile=true;Convert Zero Datetime=True;Allow Zero Datetime=True;";

// Only add database context if connection string is available
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLHOST")))
{
    builder.Services.AddDbContext<NormaizeContext>(options =>
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
}
else
{
    // Use in-memory database for testing/CI environments
    builder.Services.AddDbContext<NormaizeContext>(options =>
        options.UseInMemoryDatabase("TestDatabase"));
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program), typeof(Normaize.Core.Mapping.MappingProfile));

// Services
builder.Services.AddScoped<IDataProcessingService, Normaize.Core.Services.DataProcessingService>();
builder.Services.AddScoped<IDataAnalysisService, Normaize.Core.Services.DataAnalysisService>();
builder.Services.AddScoped<IDataVisualizationService, Normaize.Core.Services.DataVisualizationService>();
builder.Services.AddScoped<IFileUploadService, Normaize.Core.Services.FileUploadService>();
builder.Services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
builder.Services.AddHttpContextAccessor();

// Storage Service Registration
var appEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var storageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER")?.ToLowerInvariant();

// Environment-aware storage selection
if (string.IsNullOrEmpty(storageProvider))
{
    if (appEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase))
    {
        storageProvider = "sftp";
    }
    else
    {
        storageProvider = "memory"; // Use in-memory for dev/beta
    }
}

switch (storageProvider)
{
    case "sftp":
        builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, SftpStorageService>();
        Log.Information("Using SFTP storage service for production");
        break;
    case "local":
        builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, LocalStorageService>();
        Log.Information("Using local storage service");
        break;
    case "memory":
    default:
        builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, InMemoryStorageService>();
        Log.Information("Using in-memory storage service for development/beta");
        break;
}

// Repositories
builder.Services.AddScoped<IDataSetRepository, DataSetRepository>();
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<IDataSetRowRepository, DataSetRowRepository>();

// HTTP Client
builder.Services.AddHttpClient();

var app = builder.Build();

// Apply database migrations automatically (only for Railway/Production environments)
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLHOST")))
{
    try
    {
        Log.Information("Applying database migrations...");
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NormaizeContext>();
        context.Database.Migrate();
        Log.Information("Database migrations applied successfully");
        
        // Note: MySQL optimizations are now handled through EF Core migrations
        Log.Information("Database setup complete");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error applying database migrations");
        // Don't throw - allow application to start even if migrations fail
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("Beta", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Normaize API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Add Auth0 middleware
app.UseAuth0();

// Add request logging middleware (skip in test environment)
if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
{
    app.UseMiddleware<RequestLoggingMiddleware>();
}

app.MapControllers();

// Map health checks
app.MapHealthChecks("/health/startup");

// Global exception handler (skip in test environment)
if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
{
    app.UseMiddleware<ExceptionHandlingMiddleware>();
}

// Use PORT environment variable if set (for Railway)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

try
{
    Log.Information("Starting Normaize API on port {Port}", port);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { } 