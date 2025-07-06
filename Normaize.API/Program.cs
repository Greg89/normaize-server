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
Env.Load();

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
builder.Services.AddAutoMapper(typeof(Program));

// Services
builder.Services.AddScoped<IDataProcessingService, Normaize.Core.Services.DataProcessingService>();
builder.Services.AddScoped<IDataAnalysisService, Normaize.Core.Services.DataAnalysisService>();
builder.Services.AddScoped<IDataVisualizationService, Normaize.Core.Services.DataVisualizationService>();
builder.Services.AddScoped<IFileUploadService, Normaize.Core.Services.FileUploadService>();
builder.Services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
builder.Services.AddHttpContextAccessor();

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
        
        // Apply MySQL optimizations if not already applied
        await ApplyMySqlOptimizationsAsync(context);
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

// Helper method to apply MySQL optimizations
static async Task ApplyMySqlOptimizationsAsync(NormaizeContext context)
{
    try
    {
        Log.Information("Checking for MySQL optimizations...");
        
        // Check if optimizations table exists (indicates optimizations were applied)
        var optimizationsApplied = await context.Database.SqlQueryRaw<bool>(
            "SELECT COUNT(*) > 0 FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'DataSetStatistics'"
        ).FirstOrDefaultAsync();
        
        if (!optimizationsApplied)
        {
            Log.Information("Applying MySQL optimizations...");
            
            // Read and execute the optimization script
            var optimizationScript = await File.ReadAllTextAsync("Migrations/MySQL_Optimizations.sql");
            var commands = optimizationScript.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var command in commands)
            {
                var trimmedCommand = command.Trim();
                if (!string.IsNullOrEmpty(trimmedCommand) && !trimmedCommand.StartsWith("--"))
                {
                    try
                    {
                        await context.Database.ExecuteSqlRawAsync(trimmedCommand);
                    }
                    catch (Exception ex)
                    {
                        // Log but continue - some commands might fail if objects already exist
                        Log.Warning(ex, "MySQL optimization command failed (this is usually safe): {Command}", trimmedCommand.Substring(0, Math.Min(50, trimmedCommand.Length)));
                    }
                }
            }
            
            Log.Information("MySQL optimizations applied successfully");
        }
        else
        {
            Log.Information("MySQL optimizations already applied, skipping");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error applying MySQL optimizations - this is not critical");
    }
}

// Make Program class accessible for integration tests
public partial class Program { } 