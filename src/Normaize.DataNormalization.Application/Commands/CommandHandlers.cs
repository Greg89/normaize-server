using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Application.Commands;

public class SubmitDuplicateRemovalJobCommandHandler : ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid>
{
    private readonly INormalizationJobRepository _repository;
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IJobQueue _jobQueue;

    public SubmitDuplicateRemovalJobCommandHandler(
        INormalizationJobRepository repository, 
        IDataSetRepository dataSetRepository,
        IJobQueue jobQueue)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
    }

    public async Task<Guid> HandleAsync(SubmitDuplicateRemovalJobCommand command)
    {
        // Validate that the dataset exists
        var dataSet = await _dataSetRepository.GetByIdAsync(command.DataSetId);
        if (dataSet == null)
        {
            throw new InvalidOperationException($"Dataset with ID {command.DataSetId} not found");
        }

        // Create the job using the domain aggregate with type-safe options
        var job = NormalizationJob.CreateDuplicateRemovalJob(command.DataSetId, command.Options);

        // Save to repository
        await _repository.SaveAsync(job);

        // Enqueue for processing
        await _jobQueue.EnqueueAsync(job);

        return job.Id;
    }
}

public class RetryJobCommandHandler : ICommandHandler<RetryJobCommand>
{
    private readonly INormalizationJobRepository _repository;
    private readonly IJobQueue _jobQueue;

    public RetryJobCommandHandler(INormalizationJobRepository repository, IJobQueue jobQueue)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
    }

    public async Task HandleAsync(RetryJobCommand command)
    {
        var job = await _repository.GetByIdAsync(command.JobId);
        if (job == null)
            throw new InvalidOperationException($"Job with ID {command.JobId} not found");

        // Use domain logic to schedule retry
        job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5)); // 5 minute delay

        // Update the repository
        await _repository.UpdateAsync(job);

        // Re-enqueue for processing
        await _jobQueue.EnqueueAsync(job);
    }
}

public class CancelJobCommandHandler : ICommandHandler<CancelJobCommand>
{
    private readonly INormalizationJobRepository _repository;

    public CancelJobCommandHandler(INormalizationJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task HandleAsync(CancelJobCommand command)
    {
        var job = await _repository.GetByIdAsync(command.JobId);
        if (job == null)
            throw new InvalidOperationException($"Job with ID {command.JobId} not found");

        // Move to dead letter with cancellation reason
        job.MoveToDeadLetter("Cancelled by user");

        // Update the repository
        await _repository.UpdateAsync(job);
    }
}