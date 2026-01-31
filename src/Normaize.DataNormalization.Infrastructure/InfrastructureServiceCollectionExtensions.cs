using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Infrastructure.Repositories;
using Normaize.DataNormalization.Infrastructure.Services;
using Normaize.DataNormalization.Infrastructure.Handlers;
using Normaize.DataNormalization.Infrastructure.Workers;
using Normaize.DataNormalization.Infrastructure.HealthChecks;
using Normaize.DataNormalization.Infrastructure.Behaviors;
using Normaize.DataNormalization.Infrastructure.Telemetry;
using Normaize.DataNormalization.Infrastructure.Logging;

namespace Normaize.DataNormalization.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDataNormalizationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Note: Serilog is configured earlier via builder.Host.ConfigureSerilog() in Program.cs
        // This ensures proper initialization order with the host builder

        // OpenTelemetry Tracing
        services.AddOpenTelemetryTracing(configuration);

        // Database
        services.AddDbContext<DataNormalizationDbContext>(options =>
        {
            // Try standard .NET connection string first, fallback to Railway's DATABASE_URL
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? ConvertDatabaseUrl(configuration["DATABASE_URL"]);
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(DataNormalizationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Repositories
        services.AddScoped<INormalizationJobRepository, NormalizationJobRepository>();
        services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        services.AddScoped<IStatisticsRepository, StatisticsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Data access repositories  
        services.AddScoped<IDataSetRepository, DataSetRepository>();
        services.AddScoped<IDataSetRowRepository, DataSetRowRepository>();

        // File Storage - Always use S3 (local storage removed)
        services.AddScoped<IFileStorageService, S3FileStorageService>();
        Console.WriteLine("✓ Configured S3 file storage");

        services.AddScoped<IFileProcessingService, FileProcessingService>();
        services.AddScoped<IFileValidationService, FileValidationService>();
        services.AddScoped<IFileHashService, FileHashService>();
        services.AddScoped<IAuditService, AuditService>();

        // Domain Event Publishing
        services.AddScoped<IDomainEventPublisher, MediatRDomainEventPublisher>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(InfrastructureServiceCollectionExtensions).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(Normaize.DataNormalization.Application.Statistics.Commands.GenerateDataSummary.GenerateDataSummaryCommand).Assembly);
        });

        // MediatR Pipeline Behaviors - Observability
        // This behavior adds logging, timing, and tracing to all MediatR requests
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ObservabilityBehavior<,>));

        // Application Services
        services.AddScoped<IJobQueue, JobQueueService>();
        services.AddScoped<IJobProgress, JobProgressService>();
        services.AddScoped<INormalizationJobRouter, NormalizationJobRouter>();
        services.AddScoped<IAnalysisExecutionService, AnalysisExecutionService>();
        services.AddScoped<IAnalysisMapper, AnalysisMapper>();

        // Statistical Services
        services.AddScoped<IStatisticalCalculationService, StatisticalCalculationService>();
        services.AddScoped<IStatisticsMapper, StatisticsMapper>();
        services.AddScoped<Normaize.DataNormalization.Application.Common.Interfaces.IMapper, StatisticsMapper>();

        // Visualization Services
        services.AddScoped<IChartGenerationService, ChartGenerationService>();
        services.AddScoped<IDataSummaryService, DataSummaryService>();
        services.AddScoped<IVisualizationCacheService, VisualizationCacheService>();

        // Caching
        services.AddMemoryCache();

        // Environment Services
        services.AddSingleton<IEnvironmentService, EnvironmentService>();

        // Command Handlers - Jobs
        services.AddScoped<ICommandHandler<SubmitJobCommand, Guid>, SubmitJobCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid>, SubmitDuplicateRemovalJobCommandHandler>();
        services.AddScoped<ICommandHandler<RetryJobCommand>, RetryJobCommandHandler>();
        services.AddScoped<ICommandHandler<CancelJobCommand>, CancelJobCommandHandler>();

        // Command Handlers - Analysis
        services.AddScoped<ICommandHandler<CreateAnalysisCommand, AnalysisDto>, CreateAnalysisCommandHandler>();
        services.AddScoped<ICommandHandler<RunAnalysisCommand, AnalysisDto>, RunAnalysisCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteAnalysisCommand, bool>, DeleteAnalysisCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAnalysisCommand, AnalysisDto>, UpdateAnalysisCommandHandler>();
        services.AddScoped<ICommandHandler<ResetAnalysisCommand, AnalysisDto>, ResetAnalysisCommandHandler>();

        // Query Handlers - Jobs
        services.AddScoped<IQueryHandler<GetJobStatusQuery, JobStatusDto?>, GetJobStatusQueryHandler>();

        // Query Handlers - Analysis
        services.AddScoped<IQueryHandler<GetAnalysisQuery, AnalysisDto?>, GetAnalysisQueryHandler>();
        services.AddScoped<IQueryHandler<GetAnalysesByDataSetQuery, IEnumerable<AnalysisDto>>, GetAnalysesByDataSetQueryHandler>();
        services.AddScoped<IQueryHandler<GetAnalysesByStatusQuery, IEnumerable<AnalysisDto>>, GetAnalysesByStatusQueryHandler>();
        services.AddScoped<IQueryHandler<GetAnalysesByTypeQuery, IEnumerable<AnalysisDto>>, GetAnalysesByTypeQueryHandler>();
        services.AddScoped<IQueryHandler<GetAnalysisResultQuery, AnalysisResultDto?>, GetAnalysisResultQueryHandler>();
        services.AddScoped<IQueryHandler<GetAllAnalysesQuery, IEnumerable<AnalysisDto>>, GetAllAnalysesQueryHandler>();

        // Operation Handlers
        services.AddScoped<IRemoveDuplicatesHandler, RemoveDuplicatesHandler>();

        // Data loading and persistence services
        services.AddScoped<IDataSetDataLoader, DataSetDataLoader>();
        services.AddScoped<IDataSetDataPersister, DataSetDataPersister>();
        services.AddScoped<IDuplicateRemovalProcessor, DuplicateRemovalProcessor>();

        // Background Workers
        services.AddScoped<IBackgroundWorker, NormalizationWorker>();
        services.AddHostedService<WorkerHostedService>();

        // Health Checks
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                tags: new[] { "database", "sql", "ready" })
            .AddCheck<ConfigurationHealthCheck>(
                "configuration",
                tags: new[] { "configuration", "ready" })
            .AddCheck<StorageHealthCheck>(
                "storage",
                tags: new[] { "storage", "ready" });

        return services;
    }

    /// <summary>
    /// Converts a DATABASE_URL (postgresql://user:pass@host:port/db) to Npgsql connection string format
    /// </summary>
    private static string? ConvertDatabaseUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
            return null;

        try
        {
            // Parse postgresql://user:password@host:port/database
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');

            return $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        }
        catch
        {
            // If parsing fails, return as-is (might already be in correct format)
            return databaseUrl;
        }
    }
}
