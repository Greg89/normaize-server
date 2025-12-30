## Background Processing Redesign for Data Normalization

### Goals
- Reliable, observable, and scalable background execution
- Clear separation of concerns and testability
- Predictable failure handling and retries
- Operational safety (graceful shutdown, idempotency, back-pressure)

### Current Issues Observed
- Tight loop with `Task.Delay` polling and single-threaded dequeue/execute
- Work orchestration coupled with job execution logic
- Retry strategy buried inside worker with limited visibility/metrics
- Limited concurrency control and back-pressure strategies

### Proposed Architecture
- Queue Abstraction: `IJobQueue` (persisted; visibility timeout; enqueue/dequeue/ack/nack; schedule)
- Worker Service: `IBackgroundWorker` (pull, dispatch, concurrency, heartbeat, graceful stop)
- Job Router: `INormalizationJobRouter` maps `OperationType` → handler
- Handlers: per use case, e.g., `IRemoveDuplicatesHandler` (idempotent, progress reporting)
- Progress + Telemetry: `IJobProgress` → status store; metrics/events for each stage

```
API → CommandService → IJobQueue.Enqueue → Worker(s) → Router → Handler → Progress/Repo → Ack
```

### Contract Sketches

```csharp
public interface IJobQueue
{
    Task EnqueueAsync(NormalizationJob job, CancellationToken ct);
    Task<QueuedJob?> DequeueAsync(CancellationToken ct); // with lease/visibility
    Task AckAsync(Guid jobId, CancellationToken ct);
    Task NackAsync(Guid jobId, string reason, TimeSpan? delay, CancellationToken ct); // schedule retry
}

public interface IBackgroundWorker
{
    Task RunAsync(CancellationToken stoppingToken);
}

public interface INormalizationJobRouter
{
    Task HandleAsync(NormalizationJob job, IJobProgress progress, CancellationToken ct);
}

public interface IJobProgress
{
    Task StartedAsync(Guid jobId);
    Task ReportAsync(Guid jobId, int percent, string message);
    Task SucceededAsync(Guid jobId, object? result);
    Task FailedAsync(Guid jobId, string error);
}
```

### Worker Algorithm (Resilient)
1. Dequeue with visibility lease
2. Mark Started; create per-job DI scope
3. Route to handler by operation type
4. On success: `AckAsync`
5. On failure: `NackAsync` with exponential backoff and capped retries
6. Heartbeat: periodically extend visibility while processing
7. Graceful shutdown: stop dequeues, finish in-flight, respect timeout

### Concurrency & Back-Pressure
- Config: `MaxConcurrentJobs`, `DequeueBatchSize`, `MaxRetries`, `BaseRetryDelay`
- Use bounded channel/semaphore to cap parallel handlers
- If queue grows beyond threshold, emit metric and optionally slow producers

### Idempotency & Exactly-Once Semantics (Pragmatic)
- Job `DeduplicationKey` to coalesce duplicates
- Handlers are idempotent given `(jobId, datasetId, parameters)`
- Persist progress checkpoints for long-running jobs

### Observability
- Metrics: dequeues, acks, nacks, processing latency, success/failure rates
- Logs with correlation: `jobId`, `datasetId`
- Traces around dequeue → handle → ack

### Error Handling & Retry Policy
- Immediate failures: `NackAsync(jobId, reason, backoff)` with jitter
- Non-retryable exceptions flagged by handler → direct dead-letter state
- Dead-letter store with summary payload for inspection

### Configuration (Options)
- `MaxConcurrentJobs` (default 4)
- `IdleDelayMs` (default 500–1000)
- `VisibilityTimeout` (extend during work)
- `MaxRetries` (default 5) and `BackoffBaseSeconds` (default 30)

### Graceful Shutdown
- Stop dequeues; wait `ShutdownGracePeriod`
- Send `CancellationToken` to handlers; allow cleanup
- After grace, force `Nack` of leased unacked jobs to be retried later

### Migration Plan
1. Extract queue boundary: create `IJobQueue` adapter on existing store
2. Extract router + handlers; move operation switch into router
3. Implement new worker using bounded concurrency and heartbeat
4. Wire progress to existing job status store and API
5. Add metrics and structured logs; dashboards/alerts
6. Flip registration to new worker; keep old behind feature flag during bake-in

### Registration Example

```csharp
services.Configure<WorkerOptions>(configuration.GetSection("BackgroundWorker"));
services.AddSingleton<IJobQueue, EfCoreJobQueue>();
services.AddSingleton<IBackgroundWorker, NormalizationWorker>();
services.AddSingleton<INormalizationJobRouter, NormalizationJobRouter>();
services.AddScoped<IRemoveDuplicatesHandler, RemoveDuplicatesHandler>();
services.AddHostedService(sp => new WorkerHostedService(sp.GetRequiredService<IBackgroundWorker>(), sp.GetRequiredService<ILogger<WorkerHostedService>>()));
```

```csharp
public sealed class WorkerHostedService : BackgroundService
{
    private readonly IBackgroundWorker _worker;
    private readonly ILogger<WorkerHostedService> _logger;
    public WorkerHostedService(IBackgroundWorker worker, ILogger<WorkerHostedService> logger)
    { _worker = worker; _logger = logger; }
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => _worker.RunAsync(stoppingToken);
}
```

### Acceptance Criteria
- Deterministic retries with DLQ; accurate progress updates
- Bounded concurrency; safe shutdown; no job loss on restarts
- Metrics cover SLOs; logs/traces enable root cause analysis

### Next Steps
- Implement interfaces and new worker skeleton
- Add integration tests with a seeded queue and cancellation
- Cut over using a feature flag and monitor


### Why This Is Better Than Current Implementation

The current worker loops, dequeues, and executes inside a single service with ad-hoc delays and inline retry logic. The redesign separates orchestration from execution, adds back-pressure and acks/nacks, and formalizes error handling.

- Reliability: Visibility/lease + Ack/Nack prevents job loss or double-processing on crashes; current approach risks in-flight loss if the process dies between updates.
- Scalability: Bounded concurrency and batch dequeues scale processing safely; current loop is effectively single-consumer and harder to tune.
- Observability: Standardized progress API, structured logs, and metrics give clear insight; today progress updates and errors are intermingled with worker control flow.
- Testability: Router/handler boundaries allow focused unit tests for job types; current switch-case inside the worker couples control flow and business logic.
- Operational safety: Heartbeats and graceful shutdown protect against stuck leases and allow safe rollouts; current implementation relies on generic delays and cancellation only.
- Extensibility: Adding a new normalization operation means adding a handler and router entry; currently it requires changing the worker itself.

Concretely, the redesign addresses each “Current Issues Observed”:
- Tight polling loop → Dequeue with visibility lease, heartbeat, and backoff
- Orchestration coupled with execution → Dedicated `INormalizationJobRouter` and per-operation handlers
- Buried retry logic → Centralized `NackAsync` with exponential backoff policy and DLQ path
- Limited concurrency → Bounded parallelism via options (`MaxConcurrentJobs`)


### Decision Rationale (Per Component)

- Queue with Lease (Ack/Nack): This is the standard pattern used by SQS, Azure Queues, RabbitMQ with manual ack. It ensures at-least-once with protection against consumer failure and supports retries without duplicating side-effects.
- Router: Encapsulates operation dispatch, keeping the worker ignorant of business permutations. Reduces churn in orchestration code when adding new features.
- Handlers: Single-responsibility units per operation, enabling idempotency guarantees and progress semantics tailored to each job type.
- Progress Interface: Normalizes state transitions and messages so API/UI can consume consistent signals regardless of handler internals.
- Heartbeat/Visibility Extension: For long-running jobs, prevents message from reappearing mid-process and avoids duplicate concurrent processing.
- Bounded Concurrency: Prevents resource exhaustion, allows predictable throughput tuning and fair scheduling under load.
- Dead-letter Queue/State: Makes persistent failures inspectable and prevents hot-looping on poison jobs.


### Naming Conventions and Alignment to Responsibilities

- `IJobQueue`: “Queue” communicates durable job storage and delivery semantics. Methods mirror queue actions:
  - `EnqueueAsync` (producer intent)
  - `DequeueAsync` (consumer retrieve with lease)
  - `AckAsync` (acknowledge completion)
  - `NackAsync` (negative ack with optional delay for retries)

- `IBackgroundWorker`: Describes the host-managed worker loop. `RunAsync` reflects lifecycle execution driven by a cancellation token.

- `INormalizationJobRouter`: “Router” indicates responsibility to route to a handler. `HandleAsync` conveys end-to-end handling of a job by delegating to the correct handler.

- `IJobProgress`: Explicitly about reporting progress/state transitions; verbs match the lifecycle:
  - `StartedAsync` → transition to Running
  - `ReportAsync` → incremental progress (percent/message)
  - `SucceededAsync` → terminal success with optional payload
  - `FailedAsync` → terminal failure cause

- Handler names (e.g., `IRemoveDuplicatesHandler`): Operation-oriented verbs clarify the action performed; `HandleAsync` processes the job’s domain-specific work.

- Options names (`MaxConcurrentJobs`, `VisibilityTimeout`, `MaxRetries`, `BackoffBaseSeconds`): Tunable knobs named after the operational concerns they control for clarity during ops and review.


### Comparison Snapshot (Current vs Proposed)

- Control Flow: switch-case inside worker → router + handlers
- Retry: inline catch + delay → nack with exponential backoff + DLQ
- Progress: inline updates → dedicated `IJobProgress` interface
- Concurrency: implicit/single → explicit bounded parallelism
- Failure Semantics: best-effort → at-least-once with lease/ack
- Extensibility: edit worker → add handler and router mapping


