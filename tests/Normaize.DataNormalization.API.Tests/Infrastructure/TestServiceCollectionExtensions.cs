using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

namespace Normaize.DataNormalization.API.Tests.Infrastructure;

/// <summary>
/// Service collection extensions for test environment
/// </summary>
public static class TestServiceCollectionExtensions
{
    public static IServiceCollection AddTestDataNormalizationInfrastructure(
        this IServiceCollection services)
    {
        // Database - Use InMemory for testing with unique database name per test
        services.AddDbContext<DataNormalizationDbContext>(options =>
        {
            options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid().ToString("N"));
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        return AddTestDataNormalizationInfrastructureWithoutDatabase(services);
    }

    public static IServiceCollection AddTestDataNormalizationInfrastructureWithoutDatabase(
        this IServiceCollection services)
    {

        // Repositories
        services.AddScoped<INormalizationJobRepository, NormalizationJobRepository>();

        // Data access repositories  
        services.AddScoped<IDataSetRepository, DataSetRepository>();
        services.AddScoped<IDataSetRowRepository, DataSetRowRepository>();

        // Domain Event Publishing
        services.AddScoped<IDomainEventPublisher, MediatRDomainEventPublisher>();
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(MediatRDomainEventPublisher).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(SubmitJobCommandHandler).Assembly);
        });

        // Application Services
        services.AddScoped<IJobQueue, JobQueueService>();
        services.AddScoped<IJobProgress, JobProgressService>();
        services.AddScoped<INormalizationJobRouter, NormalizationJobRouter>();

        // Command Handlers
        services.AddScoped<ICommandHandler<SubmitJobCommand, Guid>, SubmitJobCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid>, SubmitDuplicateRemovalJobCommandHandler>();
        services.AddScoped<ICommandHandler<RetryJobCommand>, RetryJobCommandHandler>();
        services.AddScoped<ICommandHandler<CancelJobCommand>, CancelJobCommandHandler>();

        // Query Handlers
        services.AddScoped<IQueryHandler<GetJobStatusQuery, JobStatusDto?>, GetJobStatusQueryHandler>();

        // Operation Handlers
        services.AddScoped<IRemoveDuplicatesHandler, RemoveDuplicatesHandler>();

        // Data loading and persistence services
        services.AddScoped<IDataSetDataLoader, DataSetDataLoader>();
        services.AddScoped<IDataSetDataPersister, DataSetDataPersister>();
        services.AddScoped<IDuplicateRemovalProcessor, DuplicateRemovalProcessor>();

        // No background workers in tests - they are not needed for integration testing

        return services;
    }
}