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
using Microsoft.AspNetCore.HttpOverrides;

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

// Log all environment variables for debugging (excluding sensitive ones)
Log.Information("=== Environment Variables Debug ===");
var envVars = Environment.GetEnvironmentVariables();
foreach (var key in envVars.Keys)
{
    var keyStr = key.ToString();
    var value = envVars[key]?.ToString();
    
    // Skip sensitive environment variables
    if (keyStr?.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) == true ||
        keyStr?.Contains("SECRET", StringComparison.OrdinalIgnoreCase) == true ||
        keyStr?.Contains("KEY", StringComparison.OrdinalIgnoreCase) == true)
    {
        Log.Information("  {Key}: [REDACTED]", keyStr);
    }
    else
    {
        Log.Information("  {Key}: {Value}", keyStr, value ?? "NULL");
    }
}
Log.Information("=== End Environment Variables Debug ===");

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
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
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

// Forwarded Headers (for Railway proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Database
// Check for various ways Railway might provide database connection
var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST") ?? 
                Environment.GetEnvironmentVariable("MYSQL_HOST") ?? 
                Environment.GetEnvironmentVariable("DB_HOST");
var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? 
                   Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? 
                   Environment.GetEnvironmentVariable("DB_NAME");
var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER") ?? 
               Environment.GetEnvironmentVariable("MYSQL_USER") ?? 
               Environment.GetEnvironmentVariable("DB_USER");
var mysqlPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD") ?? 
                   Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? 
                   Environment.GetEnvironmentVariable("DB_PASSWORD");
var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT") ?? 
               Environment.GetEnvironmentVariable("MYSQL_PORT") ?? 
               Environment.GetEnvironmentVariable("DB_PORT") ?? 
               "3306";

// Log database configuration for debugging (without password)
Log.Information("Database configuration:");
Log.Information("  Host: {Host}", mysqlHost ?? "NOT SET");
Log.Information("  Database: {Database}", mysqlDatabase ?? "NOT SET");
Log.Information("  User: {User}", mysqlUser ?? "NOT SET");
Log.Information("  Port: {Port}", mysqlPort);

var connectionString = $"Server={mysqlHost};Database={mysqlDatabase};User={mysqlUser};Password={mysqlPassword};Port={mysqlPort};CharSet=utf8mb4;AllowLoadLocalInfile=true;Convert Zero Datetime=True;Allow Zero Datetime=True;";

// Only add database context if connection string is available
if (!string.IsNullOrEmpty(mysqlHost) && !string.IsNullOrEmpty(mysqlDatabase) && !string.IsNullOrEmpty(mysqlUser) && !string.IsNullOrEmpty(mysqlPassword))
{
    builder.Services.AddDbContext<NormaizeContext>(options =>
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
    Log.Information("Using MySQL database: {Database} on {Host}:{Port}", mysqlDatabase, mysqlHost, mysqlPort);
}
else
{
    // Use in-memory database for testing/CI environments
    builder.Services.AddDbContext<NormaizeContext>(options =>
        options.UseInMemoryDatabase("TestDatabase"));
    Log.Information("Using in-memory database for testing/CI environment");
    Log.Warning("Database connection parameters missing - using in-memory database");
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
builder.Services.AddScoped<IAuditService, Normaize.Data.Services.AuditService>();
builder.Services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
builder.Services.AddScoped<IMigrationService, Normaize.Data.Services.MigrationService>();
builder.Services.AddScoped<IHealthCheckService, Normaize.Data.Services.HealthCheckService>();
builder.Services.AddHttpContextAccessor();

// Storage Service Registration
var appEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var storageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER")?.ToLowerInvariant();

Log.Information("Current environment: {Environment}, Storage provider: {StorageProvider}", appEnvironment, storageProvider ?? "default");

// Force in-memory storage for Test environment
if (appEnvironment.Equals("Test", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, Normaize.Data.Services.InMemoryStorageService>();
    Log.Information("Using in-memory storage service for Test environment");
}
else
{
    // Environment-aware storage selection
    if (string.IsNullOrEmpty(storageProvider))
    {
        // Default to memory for all environments unless explicitly configured
        storageProvider = "memory";
    }

    switch (storageProvider)
    {
        case "sftp":
            // Only use SFTP if explicitly configured with proper credentials
            var sftpHost = Environment.GetEnvironmentVariable("SFTP_HOST");
            var sftpUsername = Environment.GetEnvironmentVariable("SFTP_USERNAME");
            var sftpPassword = Environment.GetEnvironmentVariable("SFTP_PASSWORD");
            var sftpPrivateKey = Environment.GetEnvironmentVariable("SFTP_PRIVATE_KEY");
            var sftpPrivateKeyPath = Environment.GetEnvironmentVariable("SFTP_PRIVATE_KEY_PATH");
            
            if (string.IsNullOrEmpty(sftpHost) || string.IsNullOrEmpty(sftpUsername) ||
                (string.IsNullOrEmpty(sftpPassword) && string.IsNullOrEmpty(sftpPrivateKey) && string.IsNullOrEmpty(sftpPrivateKeyPath)))
            {
                Log.Warning("SFTP storage requested but credentials not configured. Falling back to memory storage.");
                builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, Normaize.Data.Services.InMemoryStorageService>();
            }
            else
            {
                builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, Normaize.Data.Services.SftpStorageService>();
                Log.Information("Using SFTP storage service");
            }
            break;
        case "s3":
            // Only use S3 if explicitly configured with proper credentials
            var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            
            if (string.IsNullOrEmpty(awsAccessKey) || string.IsNullOrEmpty(awsSecretKey))
            {
                Log.Warning("S3 storage requested but credentials not configured. Falling back to memory storage.");
                builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, Normaize.Data.Services.InMemoryStorageService>();
            }
            else
            {
                builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, Normaize.Data.Services.S3StorageService>();
                Log.Information("Using S3 storage service");
            }
            break;
        case "minio":
            // MinIO is now supported via S3-compatible mode
            Log.Warning("MinIO storage requested. Please use STORAGE_PROVIDER=s3 with AWS_SERVICE_URL pointing to your MinIO endpoint.");
            builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, Normaize.Data.Services.InMemoryStorageService>();
            break;
        case "local":
            builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, Normaize.Data.Services.LocalStorageService>();
            Log.Information("Using local storage service");
            break;
        case "memory":
        default:
            builder.Services.AddScoped<Normaize.Core.Interfaces.IStorageService, Normaize.Data.Services.InMemoryStorageService>();
            Log.Information("Using in-memory storage service for {Environment}", appEnvironment);
            break;
    }
}

// Repositories
builder.Services.AddScoped<IDataSetRepository, Normaize.Data.Repositories.DataSetRepository>();
builder.Services.AddScoped<IAnalysisRepository, Normaize.Data.Repositories.AnalysisRepository>();
builder.Services.AddScoped<IDataSetRowRepository, Normaize.Data.Repositories.DataSetRowRepository>();

// HTTP Client
builder.Services.AddHttpClient();

var app = builder.Build();

// Apply database migrations and verify health (only for Railway/Production environments)
// Check for various ways Railway might provide database connection
var hasDatabaseConnection = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLHOST")) ||
                           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL")) ||
                           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQL_HOST")) ||
                           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_HOST")) ||
                           (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLDATABASE")) && 
                            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLUSER")));

// Log environment variables for debugging (without sensitive data)
Log.Information("Database connection detection:");
Log.Information("  MYSQLHOST: {MYSQLHOST}", Environment.GetEnvironmentVariable("MYSQLHOST") ?? "NOT SET");
Log.Information("  MYSQLDATABASE: {MYSQLDATABASE}", Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? "NOT SET");
Log.Information("  MYSQLUSER: {MYSQLUSER}", Environment.GetEnvironmentVariable("MYSQLUSER") ?? "NOT SET");
Log.Information("  MYSQLPORT: {MYSQLPORT}", Environment.GetEnvironmentVariable("MYSQLPORT") ?? "NOT SET");
Log.Information("  DATABASE_URL: {DATABASE_URL}", Environment.GetEnvironmentVariable("DATABASE_URL") ?? "NOT SET");
Log.Information("  ASPNETCORE_ENVIRONMENT: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NOT SET");
Log.Information("  Has database connection: {HasConnection}", hasDatabaseConnection);

// Also check if we're in a production-like environment and force migration attempts
var isProductionLike = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true ||
                       Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Staging", StringComparison.OrdinalIgnoreCase) == true ||
                       Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Beta", StringComparison.OrdinalIgnoreCase) == true;

// Check if we're in a containerized environment (Railway, Docker, etc.)
var isContainerized = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")) ||
                     File.Exists("/.dockerenv") ||
                     Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

Log.Information("Environment detection:");
Log.Information("  Is production-like: {IsProductionLike}", isProductionLike);
Log.Information("  Is containerized: {IsContainerized}", isContainerized);

if (hasDatabaseConnection || isProductionLike || isContainerized)
{
    try
    {
        Log.Information("Starting database setup and health verification...");
        using var scope = app.Services.CreateScope();
        
        // Apply migrations first
        var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
        var migrationResult = await migrationService.ApplyMigrations();
        
        if (!migrationResult.Success)
        {
            Log.Error("Migration failed: {Error}", migrationResult.ErrorMessage);
            
            // Log specific migration error details
            if (migrationResult.ErrorMessage?.Contains("Unknown column") == true)
            {
                Log.Error("Database schema mismatch detected. This may indicate a failed or incomplete migration.");
                Log.Error("Please check if all migrations have been applied correctly.");
            }
            
            // Fail fast in production
            var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (currentEnvironment == "Production" || currentEnvironment == "Staging")
            {
                Log.Fatal("Database migration failed in production environment. Application will not start.");
                throw new InvalidOperationException($"Database migration failed: {migrationResult.ErrorMessage}");
            }
            else
            {
                Log.Warning("Migration failed but continuing in Development mode");
            }
        }
        else
        {
            Log.Information("Database migrations applied successfully");
        }
        
        // Perform comprehensive startup health check using readiness probe
        var healthCheckService = scope.ServiceProvider.GetRequiredService<IHealthCheckService>();
        var healthResult = await healthCheckService.CheckReadinessAsync();
        
        if (!healthResult.IsHealthy)
        {
            Log.Error("Startup health check failed");
            foreach (var issue in healthResult.Issues)
            {
                Log.Error("Issue: {Issue}", issue);
            }
            
            // Fail fast in production
            var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (currentEnvironment == "Production" || currentEnvironment == "Staging")
            {
                Log.Fatal("Startup health check failed in production environment. Application will not start.");
                throw new InvalidOperationException($"Startup health check failed: {string.Join("; ", healthResult.Issues)}");
            }
            else
            {
                Log.Warning("Startup health check failed but continuing in Development mode");
            }
        }
        else
        {
            Log.Information("Startup health check passed - all systems healthy");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database setup and health verification");
        
        // Fail fast in production
        var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (currentEnvironment == "Production" || currentEnvironment == "Staging")
        {
            Log.Fatal("Database setup failed in production environment. Application will not start.");
            throw;
        }
        else
        {
            Log.Warning("Database setup failed but continuing in Development mode");
        }
    }
}
else
{
    Log.Information("No database connection detected and not in production-like environment, skipping migrations and health checks");
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

// Use forwarded headers (for Railway proxy)
app.UseForwardedHeaders();

// HTTPS Redirection
if (app.Environment.IsProduction() || app.Environment.IsEnvironment("beta"))
{
    // In production (Railway), HTTPS is handled by the load balancer
    // Only redirect if we're not behind a proxy and have HTTPS configured
    var httpsPort = Environment.GetEnvironmentVariable("HTTPS_PORT");
    if (!string.IsNullOrEmpty(httpsPort) && int.TryParse(httpsPort, out var httpsPortNumber))
    {
        app.UseHttpsRedirection();
    }
    else
    {
        // Log that we're skipping HTTPS redirection in Railway environment
        Log.Information("Skipping HTTPS redirection - running behind Railway load balancer");
    }
}
else
{
    // In development, always use HTTPS redirection
    app.UseHttpsRedirection();
}

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
app.MapHealthChecks("/health/readiness");

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