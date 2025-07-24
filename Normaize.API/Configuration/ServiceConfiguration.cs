using Microsoft.EntityFrameworkCore;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using Normaize.Core.Services;
using System.Diagnostics;

namespace Normaize.API.Configuration;

/// <summary>
/// Service configuration class responsible for setting up all application services.
/// Implements chaos engineering principles and follows SonarQube quality standards.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures all application services with proper error handling and resilience patterns.
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <exception cref="InvalidOperationException">Thrown when critical configuration fails</exception>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var correlationId = GenerateCorrelationId();
        var logger = CreateLogger(builder);
        
        try
        {
            logger.LogInformation("Starting service configuration. CorrelationId: {CorrelationId}", correlationId);
            
            // Phase 1: Core Configuration (must succeed)
            ConfigureCoreServices(builder, logger, correlationId);
            
            // Phase 2: Infrastructure Services (with fallbacks)
            ConfigureInfrastructureServices(builder, logger, correlationId);
            
            // Phase 3: Application Services (with resilience)
            ConfigureApplicationServices(builder, logger, correlationId);
            
            // Phase 4: Performance and Monitoring
            ConfigurePerformanceServices(builder, logger, correlationId);
            
            logger.LogInformation("Service configuration completed successfully. CorrelationId: {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Critical error during service configuration. CorrelationId: {CorrelationId}", correlationId);
            throw new InvalidOperationException($"Service configuration failed. CorrelationId: {correlationId}", ex);
        }
    }

    #region Phase 1: Core Configuration

    private static void ConfigureCoreServices(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogInformation("Configuring core services. CorrelationId: {CorrelationId}", correlationId);
        
        ConfigureConfigurationValidation(builder, logger, correlationId);
        ConfigureControllers(builder, logger, correlationId);
        ConfigureSwagger(builder, logger, correlationId);
        ConfigureHealthChecks(builder, logger, correlationId);
        ConfigureAuthentication(builder, logger, correlationId);
        ConfigureForwardedHeaders(builder, logger, correlationId);
    }

    private static void ConfigureConfigurationValidation(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring configuration validation. CorrelationId: {CorrelationId}", correlationId);
        
        // Configure and validate service configuration options
        builder.Services.Configure<ServiceConfigurationOptions>(
            builder.Configuration.GetSection(ServiceConfigurationOptions.SectionName));

        // Configure health check settings
        builder.Services.Configure<HealthCheckConfiguration>(
            builder.Configuration.GetSection(HealthCheckConfiguration.SectionName));

        // Configure startup settings
        builder.Services.Configure<StartupConfigurationOptions>(
            builder.Configuration.GetSection(StartupConfigurationOptions.SectionName));

        // Configure storage settings
        builder.Services.Configure<StorageConfiguration>(
            builder.Configuration.GetSection("Storage"));

        // Register configuration validation service
        builder.Services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();
        
        // Register IAppConfigurationService early so it's available for other configuration methods
        builder.Services.AddSingleton<IAppConfigurationService, Normaize.Data.Services.AppConfigurationService>();
        
        // Register storage configuration service
        builder.Services.AddScoped<IStorageConfigurationService, StorageConfigurationService>();
        
        logger.LogDebug("Configuration validation services registered. CorrelationId: {CorrelationId}", correlationId);
    }

    private static void ConfigureControllers(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring controllers. CorrelationId: {CorrelationId}", correlationId);
        
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });
    }

    private static void ConfigureSwagger(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring Swagger. CorrelationId: {CorrelationId}", correlationId);
        
        // Only enable Swagger in development environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Enabling Swagger for development environment. CorrelationId: {CorrelationId}", correlationId);
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "Normaize API", Version = "v1" });
                
                // Add JWT authentication to Swagger
                c.AddSecurityDefinition(AppConstants.Auth.BEARER, new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = $"JWT Authorization header using the Bearer scheme. Example: \"{AppConstants.Auth.AUTHORIZATION_HEADER}: {AppConstants.Auth.BEARER} {{token}}\"",
                    Name = AppConstants.Auth.AUTHORIZATION_HEADER,
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = AppConstants.Auth.JWT_SCHEME
                });
                
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = AppConstants.Auth.BEARER
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
        else
        {
            logger.LogInformation("Swagger disabled for {Environment} environment. CorrelationId: {CorrelationId}", environment, correlationId);
        }
    }

    private static void ConfigureHealthChecks(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring health checks. CorrelationId: {CorrelationId}", correlationId);
        
        builder.Services.AddHealthChecks()
            .AddCheck("startup", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Application started successfully"));
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring authentication. CorrelationId: {CorrelationId}", correlationId);
        
        var issuer = Environment.GetEnvironmentVariable("AUTH0_ISSUER");
        var audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE");

        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            logger.LogWarning("AUTH0_ISSUER or AUTH0_AUDIENCE environment variables not found. JWT authentication may not work correctly. CorrelationId: {CorrelationId}", correlationId);
        }

        builder.Services.AddAuthentication(AppConstants.Auth.BEARER)
            .AddJwtBearer(AppConstants.Auth.BEARER, options =>
            {
                options.Authority = issuer;
                options.Audience = audience;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });
    }

    private static void ConfigureForwardedHeaders(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring forwarded headers. CorrelationId: {CorrelationId}", correlationId);
        
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    #endregion

    #region Phase 2: Infrastructure Services

    private static void ConfigureInfrastructureServices(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogInformation("Configuring infrastructure services. CorrelationId: {CorrelationId}", correlationId);
        
        ConfigureDatabase(builder, logger, correlationId);
        ConfigureCors(builder, logger, correlationId);
        ConfigureAutoMapper(builder, logger, correlationId);
        ConfigureStorageService(builder, logger, correlationId);
        ConfigureRepositories(builder, logger, correlationId);
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring database. CorrelationId: {CorrelationId}", correlationId);
        
        var appConfigService = GetAppConfigurationService(builder);
        var dbConfig = appConfigService.GetDatabaseConfig();
        var environment = appConfigService.GetEnvironment();
        
        if (appConfigService.HasDatabaseConnection())
        {
            logger.LogInformation("Configuring MySQL database connection. Environment: {Environment}, CorrelationId: {CorrelationId}", 
                environment, correlationId);
            
            builder.Services.AddDbContext<NormaizeContext>(options =>
            {
                options.UseMySql(dbConfig.ToConnectionString(), new MySqlServerVersion(new Version(8, 0, 0)));
                
                // Configure based on environment
                if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });
        }
        else
        {
            logger.LogInformation("No database connection detected, using in-memory database. CorrelationId: {CorrelationId}", correlationId);
            builder.Services.AddDbContext<NormaizeContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));
        }
    }

    private static void ConfigureCors(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring CORS. CorrelationId: {CorrelationId}", correlationId);
        
        var appConfigService = GetAppConfigurationService(builder);
        var environment = appConfigService.GetEnvironment();
        
        builder.Services.AddCors(options =>
        {
            // Use environment-specific CORS configuration
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Configuring development CORS for {Environment} environment. CorrelationId: {CorrelationId}", 
                    environment, correlationId);
                
                // Development policy - localhost only for local development
                options.AddPolicy("Development", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000",    // React default
                            "http://localhost:4200",    // Angular default
                            "http://localhost:8080",    // Vue default
                            "http://127.0.0.1:3000",
                            "http://127.0.0.1:4200",
                            "http://127.0.0.1:8080"
                        )
                        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept")
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            }
            else if (environment.Equals("Beta", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Configuring beta CORS for {Environment} environment. CorrelationId: {CorrelationId}", 
                    environment, correlationId);
                
                // Beta policy - allows beta.normaize.com and localhost for testing
                options.AddPolicy("Beta", policy =>
                {
                    policy.WithOrigins(
                            "https://beta.normaize.com",    // Beta production site
                            "http://localhost:3000",        // Local development
                            "http://localhost:4200",        // Local development
                            "http://localhost:8080",        // Local development
                            "http://127.0.0.1:3000",
                            "http://127.0.0.1:4200",
                            "http://127.0.0.1:8080"
                        )
                        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept")
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            }
            else
            {
                logger.LogInformation("Configuring restrictive CORS for {Environment} environment. CorrelationId: {CorrelationId}", 
                    environment, correlationId);
                options.AddPolicy("Restrictive", policy =>
                {
                    policy.WithOrigins("https://app.normaize.com")
                          .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                          .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                          .AllowCredentials();
                });
            }
        });
    }

    private static void ConfigureAutoMapper(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring AutoMapper. CorrelationId: {CorrelationId}", correlationId);
        
        builder.Services.AddAutoMapper(typeof(Program), typeof(Core.Mapping.MappingProfile));
    }

    private static void ConfigureStorageService(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring storage service. CorrelationId: {CorrelationId}", correlationId);
        
        var appConfigService = GetAppConfigurationService(builder);
        var appEnvironment = appConfigService.GetEnvironment();
        var storageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER")?.ToLowerInvariant();

        logger.LogInformation("Configuring storage service. Environment: {Environment}, Provider: {Provider}, CorrelationId: {CorrelationId}", 
            appEnvironment, storageProvider ?? "default", correlationId);

        // Force in-memory storage for Test environment
        if (appEnvironment.Equals("Test", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Using in-memory storage for test environment. CorrelationId: {CorrelationId}", correlationId);
            builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
        }
        else
        {
            // Environment-aware storage selection with fallback
            if (string.IsNullOrEmpty(storageProvider))
            {
                storageProvider = "memory";
            }

            if (storageProvider == "s3")
            {
                var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
                
                if (string.IsNullOrEmpty(awsAccessKey) || string.IsNullOrEmpty(awsSecretKey))
                {
                    logger.LogWarning("S3 storage provider selected but AWS credentials not found. Falling back to in-memory storage. CorrelationId: {CorrelationId}", correlationId);
                    builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
                }
                else
                {
                    logger.LogInformation("Configuring S3 storage service. CorrelationId: {CorrelationId}", correlationId);
                    builder.Services.AddScoped<IStorageService, S3StorageService>();
                }
            }
            else
            {
                logger.LogInformation("Using in-memory storage service. CorrelationId: {CorrelationId}", correlationId);
                builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
            }
        }
    }

    private static void ConfigureRepositories(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring repositories. CorrelationId: {CorrelationId}", correlationId);
        
        builder.Services.AddScoped<IDataSetRepository, DataSetRepository>();
        builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        builder.Services.AddScoped<IDataSetRowRepository, DataSetRowRepository>();
    }

    #endregion

    #region Phase 3: Application Services

    private static void ConfigureApplicationServices(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogInformation("Configuring application services. CorrelationId: {CorrelationId}", correlationId);
        
        // Add memory cache
        builder.Services.AddMemoryCache();
        
        // Configure chaos engineering
        builder.Services.Configure<ChaosEngineeringOptions>(
            builder.Configuration.GetSection(ChaosEngineeringOptions.SectionName));
        builder.Services.AddSingleton<IChaosEngineeringService, ChaosEngineeringService>();
        
        // Register infrastructure services first
        builder.Services.AddScoped<IDataProcessingInfrastructure, DataProcessingInfrastructure>();
        
        builder.Services.AddScoped<IDataProcessingService, DataProcessingService>();
        builder.Services.AddScoped<IDataAnalysisService, DataAnalysisService>();
        builder.Services.AddScoped<IDataVisualizationService, DataVisualizationService>();
        builder.Services.AddScoped<IFileUploadService, FileUploadService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
        builder.Services.AddScoped<IMigrationService, MigrationService>();
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
        builder.Services.AddScoped<IStartupService, StartupService>();
        builder.Services.AddHttpContextAccessor();
        
        logger.LogInformation("Application services configured successfully. CorrelationId: {CorrelationId}", correlationId);
    }

    #endregion

    #region Phase 4: Performance and Monitoring

    private static void ConfigurePerformanceServices(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogInformation("Configuring performance and monitoring services. CorrelationId: {CorrelationId}", correlationId);
        
        ConfigureHttpClient(builder, logger, correlationId);
        ConfigureCaching(logger, correlationId);
        ConfigurePerformance(builder, logger, correlationId);
    }

    private static void ConfigureHttpClient(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring HTTP client. CorrelationId: {CorrelationId}", correlationId);
        
        builder.Services.AddHttpClient();
    }

    private static void ConfigureCaching(ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring caching services. CorrelationId: {CorrelationId}", correlationId);
        
        // Memory cache is already configured in ConfigureApplicationServices
        
        // Note: Redis distributed cache configuration removed due to missing package
        // To enable Redis, add: Microsoft.Extensions.Caching.StackExchangeRedis package
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            logger.LogInformation("Redis connection string found but Redis package not available. Using in-memory cache only. CorrelationId: {CorrelationId}", correlationId);
        }
        else
        {
            logger.LogInformation("No Redis connection string found, using in-memory cache only. CorrelationId: {CorrelationId}", correlationId);
        }
    }

    private static void ConfigurePerformance(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring performance optimizations. CorrelationId: {CorrelationId}", correlationId);
        
        // Configure response compression
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        });
        
        // Configure response caching
        builder.Services.AddResponseCaching();
        
        logger.LogInformation("Performance optimizations configured successfully. CorrelationId: {CorrelationId}", correlationId);
    }

    #endregion

    #region Helper Methods

    private static ILogger CreateLogger(WebApplicationBuilder builder)
    {
        // Create a temporary service provider to get logger
        var serviceProvider = builder.Services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILogger<object>>();
    }

    private static IAppConfigurationService GetAppConfigurationService(WebApplicationBuilder builder)
    {
        var serviceProvider = builder.Services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IAppConfigurationService>();
    }

    private static string GenerateCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion
} 