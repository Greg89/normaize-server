
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

        // Program.cs maps health check endpoints unconditionally; make sure the required services exist.
        services.AddHealthChecks();

        // Repositories
        services.AddScoped<INormalizationJobRepository, NormalizationJobRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

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

        // External services - use lightweight test implementations
        // (no S3, no real file parsing pipeline, no persistent audit logging)
        services.AddSingleton<IFileStorageService, InMemoryTestFileStorageService>();
        services.AddSingleton<IFileProcessingService, FakeFileProcessingService>();
        services.AddSingleton<IAuditService, NoopAuditService>();

        // Command Handlers
        services.AddScoped<ICommandHandler<SubmitJobCommand, Guid>, SubmitJobCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid>, SubmitDuplicateRemovalJobCommandHandler>();
        services.AddScoped<ICommandHandler<RetryJobCommand>, RetryJobCommandHandler>();
        services.AddScoped<ICommandHandler<CancelJobCommand>, CancelJobCommandHandler>();

        // Query Handlers
        services.AddScoped<IQueryHandler<GetJobStatusQuery, JobStatusDto?>, GetJobStatusQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserJobsQuery, PaginatedResult<JobStatusDto>>, GetUserJobsQueryHandler>();

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