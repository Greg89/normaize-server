using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        // Database
        services.AddDbContext<DataNormalizationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
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

        // Data access repositories  
        services.AddScoped<IDataSetRepository, DataSetRepository>();
        services.AddScoped<IDataSetRowRepository, DataSetRowRepository>();

        // Domain Event Publishing
        services.AddScoped<IDomainEventPublisher, MediatRDomainEventPublisher>();
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(InfrastructureServiceCollectionExtensions).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(Normaize.DataNormalization.Application.Statistics.Commands.GenerateDataSummary.GenerateDataSummaryCommand).Assembly);
        });

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

        return services;
    }
}
