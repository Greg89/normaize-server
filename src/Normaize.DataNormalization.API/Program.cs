using Normaize.DataNormalization.Infrastructure;
using Normaize.DataNormalization.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for simple console logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add basic services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Auth0 JWT Authentication
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

Console.WriteLine($"🔐 Auth0 Configuration Check:");
Console.WriteLine($"   Domain: {(string.IsNullOrEmpty(auth0Domain) ? "NOT SET" : auth0Domain)}");
Console.WriteLine($"   Audience: {(string.IsNullOrEmpty(auth0Audience) ? "NOT SET" : auth0Audience)}");

if (!string.IsNullOrEmpty(auth0Domain) && !string.IsNullOrEmpty(auth0Audience))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Domain}/";
            options.Audience = auth0Audience;
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://{auth0Domain}/",
                ValidateAudience = true,
                ValidAudience = auth0Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                NameClaimType = ClaimTypes.NameIdentifier
            };
            
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                context.Principal?.FindFirst("sub")?.Value;
                    Log.Information("JWT token validated for user: {UserId}", userId);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();
    Console.WriteLine("✓ Auth0 JWT authentication configured");
}
else
{
    // Register empty authentication/authorization to prevent 500 errors when [Authorize] is used
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options => 
        {
            // No validation - this is just to prevent exceptions
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, parameters) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)
            };
        });
    builder.Services.AddAuthorization();
    
    Console.WriteLine("⚠ Warning: Auth0 configuration missing - API endpoints will be unprotected");
    Console.WriteLine("  Please set Auth0:Domain and Auth0:Audience in configuration");
}

// Configure Swagger with JWT Bearer authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Normaize Data Normalization API",
        Version = "v1",
        Description = "Clean DDD Architecture API for data normalization operations."
    });
    
    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token"
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
            new string[] {}
        }
    });
});

// Configure CORS for production and development
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? 
                    new[] { "http://localhost:5173", "http://localhost:3000" };

Console.WriteLine($"🌐 Configuring CORS with {allowedOrigins.Length} allowed origin(s):");
foreach (var origin in allowedOrigins)
{
    Console.WriteLine($"   - {origin}");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains(); // Allow subdomains
    });
    
    // Fallback development policy (only used when no production origins configured)
    options.AddPolicy("DevelopmentOnly", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

Console.WriteLine($"✓ CORS configured for origins: {string.Join(", ", allowedOrigins)}");

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

// Configure URLs for Railway deployment
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var url = $"http://0.0.0.0:{port}";

// Only set URLs if not in development (Railway handles this)
if (!app.Environment.IsDevelopment())
{
    app.Urls.Add(url);
}
else
{
    app.Urls.Add("http://localhost:5001");
}

Console.WriteLine("🚀 Starting Normaize Data Normalization API...");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🌐 Listening on: {(app.Environment.IsDevelopment() ? "http://localhost:5001" : url)}");

// Add global exception handler for better error logging
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Unhandled exception in request {Method} {Path}", 
            context.Request.Method, context.Request.Path);
        throw;
    }
});

// Configure pipeline
// Only enable Swagger in development or when explicitly configured
var enableSwagger = app.Environment.IsDevelopment() || 
                   builder.Configuration.GetValue<bool>("Features:EnableSwagger", false);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Normaize API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root
        c.DocumentTitle = "Normaize API Documentation";
    });
    Console.WriteLine("✓ Swagger UI enabled");
}
else
{
    Console.WriteLine("⚠ Swagger UI disabled for production");
}

// Use production-ready CORS policy
var corsPolicy = allowedOrigins.Length > 0 && !allowedOrigins.Contains("*") 
    ? "AllowSpecificOrigins" 
    : "DevelopmentOnly";
app.UseCors(corsPolicy);

Console.WriteLine($"✓ Using CORS policy: {corsPolicy}");

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

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

// Apply database migrations in background to not block startup
_ = Task.Run(async () =>
{
    await Task.Delay(1000); // Brief delay to ensure server is starting
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var dbContext = scope.ServiceProvider.GetService<Normaize.DataNormalization.Infrastructure.Data.DataNormalizationDbContext>();
        
        if (dbContext == null)
        {
            logger.LogWarning("⚠ Database context not registered - skipping migrations");
            return;
        }

        logger.LogInformation("🔄 Checking for pending database migrations...");
        
        // Ensure PostgreSQL extensions are created before migrations
        try
        {
            logger.LogInformation("🔧 Ensuring PostgreSQL extensions are installed...");
            await dbContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
            await dbContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            logger.LogInformation("✅ PostgreSQL extensions verified");
        }
        catch (Exception extEx)
        {
            logger.LogWarning(extEx, "⚠ Could not create PostgreSQL extensions - may already exist or lack permissions");
        }
        
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingCount = pendingMigrations.Count();
        
        if (pendingCount > 0)
        {
            logger.LogInformation("📦 Applying {Count} pending migration(s): {Migrations}", 
                pendingCount, string.Join(", ", pendingMigrations));
            
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("✅ Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("✅ Database is up to date - no migrations needed");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error applying database migrations in background");
        logger.LogWarning("⚠ Application will continue running, but database may not be up to date");
    }
});

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }