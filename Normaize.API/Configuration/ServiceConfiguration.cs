using Microsoft.EntityFrameworkCore;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using Normaize.Core.Services;

namespace Normaize.API.Configuration;

public static class ServiceConfiguration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
        
        try
        {
            logger.LogInformation("Starting service configuration...");
            
            ConfigureConfigurationValidation(builder);
            ConfigureControllers(builder);
            ConfigureSwagger(builder);
            ConfigureHealthChecks(builder);
            ConfigureAuthentication(builder);
            ConfigureForwardedHeaders(builder);
            ConfigureDatabase(builder);
            ConfigureCors(builder);
            ConfigureAutoMapper(builder);
            ConfigureApplicationServices(builder);
            ConfigureStorageService(builder);
            ConfigureRepositories(builder);
            ConfigureHttpClient(builder);
            ConfigureCaching(builder);
            ConfigurePerformance(builder);
            
            logger.LogInformation("Service configuration completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during service configuration");
            throw;
        }
    }

    private static void ConfigureConfigurationValidation(WebApplicationBuilder builder)
    {
        // Configure and validate service configuration options
        builder.Services.Configure<ServiceConfigurationOptions>(
            builder.Configuration.GetSection(ServiceConfigurationOptions.SectionName));

        // Configure health check settings
        builder.Services.Configure<HealthCheckConfiguration>(
            builder.Configuration.GetSection(HealthCheckConfiguration.SectionName));

        // Configure startup settings
        builder.Services.Configure<StartupConfigurationOptions>(
            builder.Configuration.GetSection(StartupConfigurationOptions.SectionName));

        // Register configuration validation service
        builder.Services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();
        
        // Register IAppConfigurationService early so it's available for other configuration methods
        builder.Services.AddSingleton<IAppConfigurationService, Normaize.Data.Services.AppConfigurationService>();
    }

    private static void ConfigureControllers(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });
    }

    private static void ConfigureSwagger(WebApplicationBuilder builder)
    {
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

    private static void ConfigureHealthChecks(WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("startup", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Application started successfully"));
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
        
        try
        {
            var issuer = Environment.GetEnvironmentVariable("AUTH0_ISSUER");
            var audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE");

            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                logger.LogWarning("AUTH0_ISSUER or AUTH0_AUDIENCE environment variables not found. JWT authentication may not work correctly.");
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring authentication");
            throw;
        }
    }

    private static void ConfigureForwardedHeaders(WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
        
        try
        {
            // Get the app configuration service to check database connection
            var appConfigService = builder.Services.BuildServiceProvider().GetRequiredService<IAppConfigurationService>();
            var dbConfig = appConfigService.GetDatabaseConfig();
            
            if (appConfigService.HasDatabaseConnection())
            {
                logger.LogInformation("Configuring MySQL database connection");
                builder.Services.AddDbContext<NormaizeContext>(options =>
                {
                    options.UseMySql(dbConfig.ToConnectionString(), new MySqlServerVersion(new Version(8, 0, 0)));
                    
                    // Configure based on environment
                    var environment = appConfigService.GetEnvironment();
                    if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                    {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }
                });
            }
            else
            {
                logger.LogInformation("No database connection detected, using in-memory database");
                builder.Services.AddDbContext<NormaizeContext>(options =>
                    options.UseInMemoryDatabase("TestDatabase"));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring database");
            throw;
        }
    }

    private static void ConfigureCors(WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
        
        try
        {
            var environment = AppConfiguration.GetEnvironment();
            
            builder.Services.AddCors(options =>
            {
                if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Configuring permissive CORS for development environment");
                    options.AddPolicy("Development", policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                }
                else
                {
                    logger.LogInformation("Configuring restrictive CORS for production environment");
                    options.AddPolicy("Production", policy =>
                    {
                        policy.WithOrigins("https://yourdomain.com") // Replace with actual domains
                              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                              .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                              .AllowCredentials();
                    });
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring CORS");
            throw;
        }
    }

    private static void ConfigureAutoMapper(WebApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(typeof(Program), typeof(Normaize.Core.Mapping.MappingProfile));
    }

    private static void ConfigureApplicationServices(WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
        
        try
        {
            logger.LogInformation("Configuring application services");
            
            // Add memory cache
            builder.Services.AddMemoryCache();
            
            builder.Services.AddScoped<IDataProcessingService, DataProcessingService>();
            builder.Services.AddScoped<IDataAnalysisService, DataAnalysisService>();
            builder.Services.AddScoped<IDataVisualizationService, DataVisualizationService>();
            builder.Services.AddScoped<IFileUploadService, FileUploadService>();
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddScoped<Normaize.Core.Interfaces.IStructuredLoggingService, Normaize.Data.Services.StructuredLoggingService>();
            builder.Services.AddScoped<IMigrationService, MigrationService>();
            builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
            builder.Services.AddScoped<IStartupService, StartupService>();
            builder.Services.AddHttpContextAccessor();
            
            logger.LogInformation("Application services configured successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring application services");
            throw;
        }
    }

    private static void ConfigureStorageService(WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
        
        try
        {
            var appEnvironment = AppConfiguration.GetEnvironment();
            var storageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER")?.ToLowerInvariant();

            logger.LogInformation("Configuring storage service. Environment: {Environment}, Provider: {Provider}", 
                appEnvironment, storageProvider ?? "default");

            // Force in-memory storage for Test environment
            if (appEnvironment.Equals("Test", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Using in-memory storage for test environment");
                builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
            }
            else
            {
                // Environment-aware storage selection
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
                        logger.LogWarning("S3 storage provider selected but AWS credentials not found. Falling back to in-memory storage.");
                        builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
                    }
                    else
                    {
                        logger.LogInformation("Configuring S3 storage service");
                        builder.Services.AddScoped<IStorageService, S3StorageService>();
                    }
                }
                else
                {
                    logger.LogInformation("Using in-memory storage service");
                    builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring storage service");
            throw;
        }
    }

    private static void ConfigureRepositories(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IDataSetRepository, DataSetRepository>();
        builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        builder.Services.AddScoped<IDataSetRowRepository, DataSetRowRepository>();
    }

    private static void ConfigureHttpClient(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();
    }

    private static void ConfigureCaching(WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
        
        try
        {
            logger.LogInformation("Configuring caching services");
            
            // Memory cache is already configured in ConfigureApplicationServices
            
            // Note: Redis distributed cache configuration removed due to missing package
            // To enable Redis, add: Microsoft.Extensions.Caching.StackExchangeRedis package
            var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                logger.LogInformation("Redis connection string found but Redis package not available. Using in-memory cache only.");
            }
            else
            {
                logger.LogInformation("No Redis connection string found, using in-memory cache only");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring caching services");
            throw;
        }
    }

    private static void ConfigurePerformance(WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
        
        try
        {
            logger.LogInformation("Configuring performance optimizations");
            
            // Configure response compression
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
                options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
            });
            
            // Configure response caching
            builder.Services.AddResponseCaching();
            
            logger.LogInformation("Performance optimizations configured successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring performance optimizations");
            throw;
        }
    }
} 