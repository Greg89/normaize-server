# Background Jobs — Next Steps

Context: background job dequeue/claim is now atomic on PostgreSQL (`FOR UPDATE SKIP LOCKED`) and worker throughput was improved (only delay when queue is empty). Postgres container tests exist and are intentionally separate from the default suite.

This doc captures the remaining follow-ups from the background job review so work can be resumed quickly.

## Current state (what’s already done)

- Atomic claim on PostgreSQL
  - `INormalizationJobRepository.ClaimNextQueuedJobAsync()`
  - Implementation: `NormalizationJobRepository.ClaimNextQueuedJobAsync()` uses row locking + `SKIP LOCKED`.
  - Tests (real Postgres/Testcontainers): `tests/Normaize.DataNormalization.PostgresTests/BackgroundJobs/ClaimNextQueuedJobAsyncTests.cs`
- Worker loop delay behavior
  - `NormalizationWorker.ProcessJobsAsync()` continues immediately after processing a job and only delays when no job is available.

## Remaining work (recommended order)

### 1) Make retry delays real (persisted + enforced)

Problem:
- `NormalizationJob.ScheduleRetry(DateTime retryAt)` accepts a timestamp but does not store it anywhere.
- Claim/dequeue currently only filters by `status = Queued`, so a “delayed retry” can be claimed immediately.

Proposed change:
- Add a persisted `NextAttemptAtUtc` (or `next_attempt_at`) column to `normalization_jobs`.
- Update `ScheduleRetry(retryAt)` to set that value.
- Update claim query to only return jobs that are due:
  - `status = 'Queued' AND (next_attempt_at IS NULL OR next_attempt_at <= now())`

Files to touch:
- Domain aggregate: `src/Normaize.DataNormalization.Domain/Aggregates/NormalizationJob.cs`
- EF config/migration: `src/Normaize.DataNormalization.Infrastructure/Data/Configurations/NormalizationJobConfiguration.cs` + new migration
- Claim logic: `src/Normaize.DataNormalization.Infrastructure/Repositories/NormalizationJobRepository.cs`
- Retry caller(s): `src/Normaize.DataNormalization.Infrastructure/Services/JobQueueService.cs` (and anywhere else retry is scheduled)

Tests to add:
- Postgres test proving delayed job is NOT claimable until time is reached.
  - Add to: `tests/Normaize.DataNormalization.PostgresTests/BackgroundJobs/`.

### 2) Fix inconsistent “Started” behavior in JobProgressService

Problem:
- `JobProgressService.StartedAsync()` checks `job.Status == Processing` and then may call `job.Start()` if `StartedAt == null`.
- But `NormalizationJob.Start()` throws unless the status is `Queued`.

Options:
- Option A (preferred): treat “started” as purely observational and NEVER call `Start()` from progress service.
  - If status is already `Processing`, just persist a missing `StartedAt` directly (or no-op).
- Option B: refactor domain so there is a separate method like `EnsureStartedTimestamp()` that doesn’t require `Queued`.

Files:
- `src/Normaize.DataNormalization.Infrastructure/Services/JobProgressService.cs`
- `src/Normaize.DataNormalization.Domain/Aggregates/NormalizationJob.cs`

### 3) Consolidate or remove duplicate queue/progress implementations

Problem:
- There are two implementations each for queue and progress:
  - Queue: `JobQueueService` vs `EfCoreJobQueue`
  - Progress: `JobProgressService` vs `EfCoreJobProgress`
- DI currently wires `IJobQueue` to `JobQueueService` and `IJobProgress` to `JobProgressService`, so the EF-Core variants may be dead code.

Decision:
- Either remove the unused ones, or explicitly document when each is intended to be used.

Files:
- `src/Normaize.DataNormalization.Infrastructure/Services/EfCoreJobQueue.cs`
- `src/Normaize.DataNormalization.Infrastructure/Services/EfCoreJobProgress.cs`
- DI wiring: `src/Normaize.DataNormalization.Infrastructure/InfrastructureServiceCollectionExtensions.cs`

### 4) Decide/clarify job completion semantics (ACK vs progress)

Observation:
- Worker calls `AckAsync(job.Id)` after handler completes.
- `JobQueueService.AckAsync()` currently does not transition job state.
- Completion is effectively done via `IJobProgress.SucceededAsync()`.

Decision:
- Option A: make ACK enforce completion (e.g., ensure job is `Succeeded`, maybe set `CompletedAt` if missing).
- Option B: keep ACK as a no-op and remove it from the worker to reduce confusion.

Files:
- `src/Normaize.DataNormalization.Infrastructure/Workers/NormalizationWorker.cs`
- `src/Normaize.DataNormalization.Infrastructure/Services/JobQueueService.cs`

### 5) Crash recovery for stuck Processing jobs

Problem:
- If a worker crashes mid-job, jobs can remain `Processing` forever.

Possible solutions:
- Add a lease/heartbeat field (e.g., `processing_heartbeat_at`) and a max age.
- Add a periodic reaper that moves stale jobs back to `Queued` or to `Failed`/`DeadLettered`.

Where:
- Repository method + scheduled maintenance job (hosted service) or a periodic step in `WorkerHostedService`.

Files:
- `src/Normaize.DataNormalization.Infrastructure/Workers/WorkerHostedService.cs`
- `src/Normaize.DataNormalization.Infrastructure/Repositories/NormalizationJobRepository.cs`

### 6) Add CancellationToken to router/handlers

Problem:
- `NormalizationWorker` runs with a cancellation token, but `INormalizationJobRouter.HandleAsync()` does not accept one.

Proposed:
- Thread `CancellationToken` through router and all handlers.

Files:
- Router: `src/Normaize.DataNormalization.Infrastructure/Services/NormalizationJobRouter.cs`
- Interfaces: `src/Normaize.DataNormalization.Application/Interfaces/*`
- Worker callsite: `src/Normaize.DataNormalization.Infrastructure/Workers/NormalizationWorker.cs`

## Notes

- Real Postgres tests are separate by design:
  - Project: `tests/Normaize.DataNormalization.PostgresTests/Normaize.DataNormalization.PostgresTests.csproj`
  - CI:
    - Non-blocking run in main CI workflow
    - Blocking nightly workflow: `.github/workflows/nightly-postgres-tests.yml`
