using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using Normaize.Core.Services;
using Normaize.Core.Services.Visualization;
using Normaize.Core.Services.FileUpload;
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
            builder.Configuration.GetSection(SharedConstants.ConfigurationSections.STORAGE));

        // Register configuration validation service
        builder.Services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();

        // Register IAppConfigurationService early so it's available for other configuration methods
        builder.Services.AddSingleton<IAppConfigurationService, AppConfigurationService>();

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
                // Use the global JSON configuration for consistency
                var defaultOptions = Normaize.Core.Configuration.JsonConfiguration.DefaultOptions;
                options.JsonSerializerOptions.PropertyNamingPolicy = defaultOptions.PropertyNamingPolicy;
                options.JsonSerializerOptions.DictionaryKeyPolicy = defaultOptions.DictionaryKeyPolicy;
                options.JsonSerializerOptions.WriteIndented = defaultOptions.WriteIndented;
                options.JsonSerializerOptions.DefaultIgnoreCondition = defaultOptions.DefaultIgnoreCondition;
                options.JsonSerializerOptions.Encoder = defaultOptions.Encoder;
                options.JsonSerializerOptions.ReferenceHandler = defaultOptions.ReferenceHandler;
                options.JsonSerializerOptions.Converters.Clear();
                foreach (var converter in defaultOptions.Converters)
                {
                    options.JsonSerializerOptions.Converters.Add(converter);
                }
            });

        // Configure global JSON options for HttpClient and other services
        builder.Services.Configure<System.Text.Json.JsonSerializerOptions>(options =>
        {
            var defaultOptions = Normaize.Core.Configuration.JsonConfiguration.DefaultOptions;
            options.PropertyNamingPolicy = defaultOptions.PropertyNamingPolicy;
            options.DictionaryKeyPolicy = defaultOptions.DictionaryKeyPolicy;
            options.WriteIndented = defaultOptions.WriteIndented;
            options.DefaultIgnoreCondition = defaultOptions.DefaultIgnoreCondition;
            options.Encoder = defaultOptions.Encoder;
            options.ReferenceHandler = defaultOptions.ReferenceHandler;
            options.Converters.Clear();
            foreach (var converter in defaultOptions.Converters)
            {
                options.Converters.Add(converter);
            }
        });

        logger.LogDebug("JSON serialization configured with camelCase naming policy. CorrelationId: {CorrelationId}", correlationId);
    }

    private static void ConfigureSwagger(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring Swagger. CorrelationId: {CorrelationId}", correlationId);

        // Only enable Swagger in development environment
        var environment = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.ASPNETCORE_ENVIRONMENT) ?? SharedConstants.Environment.DEVELOPMENT;

        if (environment.Equals(SharedConstants.Environment.DEVELOPMENT, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Enabling Swagger for development environment. CorrelationId: {CorrelationId}", correlationId);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Normaize API",
                    Version = "v1",
                    Description = "API for Normaize data processing and analysis platform"
                });

                // Add JWT authentication to Swagger
                c.AddSecurityDefinition(AuthConstants.Auth.BEARER, new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = $"JWT Authorization header using the Bearer scheme. Example: \"{AuthConstants.Auth.AUTHORIZATION_HEADER}: {AuthConstants.Auth.BEARER} {{token}}\"",
                    Name = AuthConstants.Auth.AUTHORIZATION_HEADER,
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = AuthConstants.Auth.JWT_SCHEME
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = AuthConstants.Auth.BEARER
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Include XML comments if available
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
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

        var issuer = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.AUTH0_ISSUER);
        var audience = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.AUTH0_AUDIENCE);

        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            logger.LogWarning("AUTH0_ISSUER or AUTH0_AUDIENCE environment variables not found. JWT authentication may not work correctly. CorrelationId: {CorrelationId}", correlationId);
        }

        builder.Services.AddAuthentication(AuthConstants.Auth.BEARER)
            .AddJwtBearer(AuthConstants.Auth.BEARER, options =>
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

                // Add JWT event handlers for debugging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        logger.LogError("JWT Authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        logger.LogInformation("JWT Token validated successfully for user: {User}",
                            context.Principal?.Identity?.Name ?? "unknown");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        logger.LogInformation("JWT Message received for path: {Path}", context.Request.Path);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        logger.LogWarning("JWT Challenge issued for path: {Path}, Error: {Error}",
                            context.Request.Path, context.Error);
                        return Task.CompletedTask;
                    }
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
        ConfigureStorageService(builder, logger, correlationId);
        ConfigureRepositories(builder, logger, correlationId);
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring database. CorrelationId: {CorrelationId}", correlationId);

        // Get environment directly instead of using service provider
        var environment = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.ASPNETCORE_ENVIRONMENT) ?? SharedConstants.Environment.DEVELOPMENT;

        // Check for database connection directly
        var mysqlHost = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.MYSQLHOST);
        var mysqlDatabase = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.MYSQLDATABASE);
        var mysqlUser = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.MYSQLUSER);
        var mysqlPassword = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.MYSQLPASSWORD);
        var mysqlPort = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.MYSQLPORT) ?? DatabaseConstants.Database.DEFAULT_PORT;

        var hasDatabaseConnection = !string.IsNullOrEmpty(mysqlHost) &&
                                   !string.IsNullOrEmpty(mysqlDatabase) &&
                                   !string.IsNullOrEmpty(mysqlUser) &&
                                   !string.IsNullOrEmpty(mysqlPassword);

        if (hasDatabaseConnection)
        {
            logger.LogInformation("Configuring MySQL database connection. Environment: {Environment}, CorrelationId: {CorrelationId}",
                environment, correlationId);

            var connectionString = $"{DatabaseConstants.Database.SERVER_PREFIX}{mysqlHost};{DatabaseConstants.Database.DATABASE_PREFIX}{mysqlDatabase};{DatabaseConstants.Database.USER_PREFIX}{mysqlUser};{DatabaseConstants.Database.PASSWORD_PREFIX}{mysqlPassword};{DatabaseConstants.Database.PORT_PREFIX}{mysqlPort};{DatabaseConstants.Database.CHARSET_PREFIX}{DatabaseConstants.Database.DEFAULT_CHARSET};{DatabaseConstants.Database.ALLOW_LOAD_LOCAL_INFILE};{DatabaseConstants.Database.CONVERT_ZERO_DATETIME};{DatabaseConstants.Database.ALLOW_ZERO_DATETIME};";

            builder.Services.AddDbContext<NormaizeContext>(options =>
            {
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(DatabaseConstants.Database.MYSQL_VERSION)));

                // Configure based on environment
                if (environment.Equals(SharedConstants.Environment.DEVELOPMENT, StringComparison.OrdinalIgnoreCase))
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
                options.UseInMemoryDatabase(DatabaseConstants.Database.TEST_DATABASE_NAME));
        }
    }

    private static void ConfigureCors(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring CORS. CorrelationId: {CorrelationId}", correlationId);

        // Get environment directly instead of using service provider
        var environment = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.ASPNETCORE_ENVIRONMENT) ?? SharedConstants.Environment.DEVELOPMENT;

        builder.Services.AddCors(options =>
        {
            // Use environment-specific CORS configuration
            if (environment.Equals(SharedConstants.Environment.DEVELOPMENT, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Configuring development CORS for {Environment} environment. CorrelationId: {CorrelationId}",
                    environment, correlationId);

                // Development policy - localhost only for local development
                options.AddPolicy("DevelopmentPolicy", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000",    // React default
                            "http://localhost:4200",    // Angular default
                            "http://localhost:8080",    // Vue default
                            "http://localhost:5173",    // Vite/React default
                            "http://127.0.0.1:3000",
                            "http://127.0.0.1:4200",
                            "http://127.0.0.1:8080",
                            "http://127.0.0.1:5173"
                        )
                        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept")
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            }
            else if (environment.Equals(SharedConstants.Environment.BETA, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Configuring beta CORS for {Environment} environment. CorrelationId: {CorrelationId}",
                    environment, correlationId);

                // Beta policy - allows beta.normaize.com and localhost for testing
                options.AddPolicy("BetaPolicy", policy =>
                {
                    policy.WithOrigins(
                            "https://beta.normaize.com",    // Beta production site
                            "http://localhost:3000",        // Local development
                            "http://localhost:4200",        // Local development
                            "http://localhost:8080",        // Local development
                            "http://localhost:5173",        // Vite/React development
                            "http://127.0.0.1:3000",
                            "http://127.0.0.1:4200",
                            "http://127.0.0.1:8080",
                            "http://127.0.0.1:5173"
                        )
                        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept")
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            }
            else
            {
                logger.LogInformation("Configuring production CORS for {Environment} environment. CorrelationId: {CorrelationId}",
                    environment, correlationId);

                // Production policy - strict origin control
                options.AddPolicy("ProductionPolicy", policy =>
                {
                    policy.WithOrigins(
                            "https://normaize.com",         // Production site
                            "https://www.normaize.com",     // Production site with www
                            "https://app.normaize.com"      // Production app subdomain
                        )
                        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept")
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            }
        });
    }



    private static void ConfigureStorageService(WebApplicationBuilder builder, ILogger logger, string correlationId)
    {
        logger.LogDebug("Configuring storage service. CorrelationId: {CorrelationId}", correlationId);

        // Get environment directly instead of using service provider
        var environment = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.ASPNETCORE_ENVIRONMENT) ?? SharedConstants.Environment.DEVELOPMENT;
        var storageProvider = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.STORAGE_PROVIDER)?.ToLowerInvariant();

        logger.LogInformation("Configuring storage service. Environment: {Environment}, Provider: {Provider}, CorrelationId: {CorrelationId}",
            environment, storageProvider ?? "default", correlationId);

        // Force in-memory storage for Test environment
        if (environment.Equals(SharedConstants.Environment.TEST, StringComparison.OrdinalIgnoreCase))
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
                var awsAccessKey = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.AWS_ACCESS_KEY_ID);
                var awsSecretKey = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.AWS_SECRET_ACCESS_KEY);

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
        builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
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

        // Register visualization services
        builder.Services.AddScoped<IStatisticalCalculationService, StatisticalCalculationService>();
        builder.Services.AddScoped<IChartGenerationService, ChartGenerationService>();
        builder.Services.AddScoped<ICacheManagementService, CacheManagementService>();
        builder.Services.AddScoped<IVisualizationValidationService, VisualizationValidationService>();
        builder.Services.AddScoped<IVisualizationServices, VisualizationServices>();

        // Register file upload sub-services
        builder.Services.AddScoped<IFileValidationService, FileValidationService>();
        builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();
        builder.Services.AddScoped<IFileConfigurationService, FileConfigurationService>();
        builder.Services.AddScoped<IFileUtilityService, FileUtilityService>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
        builder.Services.AddScoped<IFileUploadServices, FileUploadServices>();

        // Register core data processing services
        builder.Services.AddScoped<IDataProcessingService, DataProcessingService>();
        builder.Services.AddScoped<IDataSetLifecycleService, DataSetLifecycleService>();
        builder.Services.AddScoped<IDataSetQueryService, DataSetQueryService>();
        builder.Services.AddScoped<IDataSetPreviewService, DataSetPreviewService>();
        builder.Services.AddScoped<IDataMigrationService, DataMigrationService>();

        builder.Services.AddScoped<IDataAnalysisService, DataAnalysisService>();
        builder.Services.AddScoped<IDataVisualizationService, DataVisualizationService>();
        builder.Services.AddScoped<IFileUploadService, FileUploadService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
        builder.Services.AddScoped<IMigrationService, MigrationService>();
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
        builder.Services.AddScoped<IStartupService, StartupService>();
        builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
        builder.Services.AddHttpContextAccessor();

        // Register job queue service and configuration
        builder.Services.Configure<JobQueueOptions>(
            builder.Configuration.GetSection("JobQueue"));
        builder.Services.AddScoped<IJobQueueService, JobQueueService>();

        // Register background services
        builder.Services.AddHostedService<DataNormalizationBackgroundService>();
        builder.Services.Configure<DataNormalizationBackgroundServiceOptions>(
            builder.Configuration.GetSection("DataNormalizationBackgroundService"));

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
        // To enable Redis, add: Microsoft.Extensions.Caching.StackExchangeRedis package
        var redisConnectionString = Environment.GetEnvironmentVariable(SharedConstants.EnvironmentVariables.REDIS_CONNECTION_STRING);
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

    private static string GenerateCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion
}