using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Application.Commands;

public class SubmitJobCommandHandler : ICommandHandler<SubmitJobCommand, Guid>
{
    private readonly INormalizationJobRepository _repository;
    private readonly IJobQueue _jobQueue;

    public SubmitJobCommandHandler(INormalizationJobRepository repository, IJobQueue jobQueue)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
    }

    public async Task<Guid> HandleAsync(SubmitJobCommand command)
    {
        // Create the job using the domain aggregate
        var job = NormalizationJob.Create(
            command.DataSetId, 
            command.OperationType, 
            command.OperationParameters);

        // Save to repository
        await _repository.SaveAsync(job);

        // Enqueue for processing
        await _jobQueue.EnqueueAsync(job);

        return job.Id;
    }
}