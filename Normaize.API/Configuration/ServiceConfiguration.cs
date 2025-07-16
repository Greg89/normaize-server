using Microsoft.EntityFrameworkCore;
using Normaize.Core.Interfaces;
using Normaize.Core.Constants;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using Normaize.Core.Services;
using Normaize.API.Configuration;

namespace Normaize.API.Configuration;

public static class ServiceConfiguration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
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
        builder.Services.AddAuthentication(AppConstants.Auth.BEARER)
            .AddJwtBearer(AppConstants.Auth.BEARER, options =>
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
        var dbConfig = AppConfiguration.GetDatabaseConfig();
        
        if (AppConfiguration.HasDatabaseConnection())
        {
            builder.Services.AddDbContext<NormaizeContext>(options =>
                options.UseMySql(dbConfig.ToConnectionString(), new MySqlServerVersion(new Version(8, 0, 0))));
        }
        else
        {
            // Use in-memory database for testing/CI environments
            builder.Services.AddDbContext<NormaizeContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));
        }
    }

    private static void ConfigureCors(WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    private static void ConfigureAutoMapper(WebApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(typeof(Program), typeof(Normaize.Core.Mapping.MappingProfile));
    }

    private static void ConfigureApplicationServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IDataProcessingService, DataProcessingService>();
        builder.Services.AddScoped<IDataAnalysisService, DataAnalysisService>();
        builder.Services.AddScoped<IDataVisualizationService, DataVisualizationService>();
        builder.Services.AddScoped<IFileUploadService, FileUploadService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<Normaize.Core.Interfaces.IStructuredLoggingService, Normaize.Data.Services.StructuredLoggingService>();
        builder.Services.AddScoped<IMigrationService, MigrationService>();
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
        builder.Services.AddSingleton<IAppConfigurationService, Normaize.Data.Services.AppConfigurationService>();
        builder.Services.AddHttpContextAccessor();
    }

    private static void ConfigureStorageService(WebApplicationBuilder builder)
    {
        var appEnvironment = AppConfiguration.GetEnvironment();
        var storageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER")?.ToLowerInvariant();

        // Force in-memory storage for Test environment
        if (appEnvironment.Equals("Test", StringComparison.OrdinalIgnoreCase))
        {
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
                    builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
                }
                else
                {
                    builder.Services.AddScoped<IStorageService, S3StorageService>();
                }
            }
            else
            {
                builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
            }
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
} 