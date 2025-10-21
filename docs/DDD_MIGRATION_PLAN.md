## DDD Migration Plan for Normaize

### Objectives
- Align the solution with DDD tactical/strategic patterns
- Isolate domain model from infrastructure and delivery
- Improve maintainability, testability, and evolution of features

### Target Architecture (High-Level)
- Bounded Contexts: `DataNormalization`, `DataSets`, `UserSettings`, `Audit`
- Layers per context:
  - Domain: entities/aggregates, value objects, domain services, domain events, repositories (interfaces)
  - Application: use cases (commands/queries), DTOs, orchestrations, transaction boundary
  - Infrastructure: EF Core mappings, repositories (implementations), external integrations
  - Interface/Delivery: Web API controllers, background workers

```
API/Workers → Application (UseCases) → Domain (Aggregates) ←→ Infrastructure (Repos, Mappings)
```

### Proposed Bounded Contexts
- DataNormalization: normalization jobs, processors, policies, job lifecycle
- DataSets: dataset lifecycle, storage metadata, ownership/access
- UserSettings: preferences, feature toggles
- Audit: audit trails, health/diagnostics
 
### Tactical Patterns
- Aggregates with invariants (e.g., `NormalizationJob`, `DataSet`)
- Value Objects (e.g., `ColumnName`, `RetentionStrategy`, `CaseSensitivity`)
- Domain Events (e.g., `JobStarted`, `JobCompleted`, `JobFailed`)
- Repositories per aggregate root (interfaces in Domain)
- Application services: command handlers and query handlers

### Folder Structure (Incremental)

```
src/
  DataNormalization/
    Domain/
      Aggregates/
      ValueObjects/
      Events/
      Services/
      Repositories/
    Application/
      Commands/
      Queries/
      DTOs/
      Mappers/
    Infrastructure/
      EfCore/
      Repositories/
      Adapters/
    Interface/
      Api/
      Workers/
```

### Migration Strategy (Phased)
1. Identify Contexts and Ownership
   - Map current modules to contexts; define boundaries and dependencies
2. Carve Out Domain Contracts
   - Create domain projects per context with aggregates/value objects
   - Move repository interfaces to domain
3. Introduce Application Layer
   - Add use-case oriented services (commands/queries)
   - Keep existing controllers calling into new application services
4. Infrastructure Adapters
   - Implement repositories/mappings outside domain
   - Wrap existing services as adapters where needed
5. Eventing & Integration
   - Emit domain events; add handlers in application/infrastructure as needed
6. Strangler Fig Refactor
   - Route new feature work to DDD modules; migrate old endpoints/use cases gradually

### Concrete Steps for This Codebase
- DataNormalization
  - Define `NormalizationJob` aggregate with state machine (Queued, Processing, Succeeded, Failed, DeadLettered)
  - Value objects for job parameters (duplicate removal options)
  - Domain events when job transitions states
  - Application commands: `SubmitJob`, `RetryJob`, `ReportProgress`
  - Infra: map existing EF models to aggregate via repository

- DataSets
  - Aggregate `DataSet` with invariants for schema and ownership
  - Application queries for listing/inspecting sets

- Cross-cutting
  - Validation via domain invariants; keep FluentValidation in application layer
  - Transaction boundary at application command handler (per use case)
  - Outbox pattern for domain events if integrating with external systems

### Project/Package Layout (Dotnet)
- `Normaize.DataNormalization.Domain`
- `Normaize.DataNormalization.Application`
- `Normaize.DataNormalization.Infrastructure`
- `Normaize.API` depends only on `.Application`

### Coding Guidelines
- Domain model free of EF Core attributes; use EF configurations in Infrastructure
- No direct DbContext usage in Application/Domain
- Constructor invariants and factory methods for aggregates
- Keep handlers small; orchestrate, not implement domain logic

### Testing Strategy
- Domain: pure unit tests on aggregates/value objects
- Application: use-case tests with in-memory repositories
- Infrastructure: integration tests against test DB
- Contract tests for repository interfaces

### Acceptance Criteria for Each Phase
- Existing features continue to work
- New modules expose use cases via Application layer
- Domain free of infrastructure concerns

### Risks & Mitigations
- Scope creep → adopt strangler pattern, prioritize high-churn areas first
- Performance regressions → add benchmarks around critical flows
- Knowledge gap → lightweight docs and examples per context

### Integration with Background Processing Redesign

The DDD migration and background processing redesign are tightly coupled and should proceed together:

**Shared Domain Model**
- `NormalizationJob` aggregate becomes the source of truth for job state transitions
- Domain events (`JobStarted`, `JobCompleted`, `JobFailed`) drive both API responses and worker orchestration
- Value objects (`DuplicateRemovalOptions`, `RetentionStrategy`) encapsulate job parameters

**Application Layer Integration**
- Command handlers (`SubmitJobCommand`, `RetryJobCommand`) orchestrate domain operations
- Query handlers (`GetJobStatusQuery`) read job state for API responses
- Application services implement the `INormalizationJobRouter` interface, delegating to domain-specific handlers

**Infrastructure Layer Alignment**
- `IJobQueue` implementation uses EF Core repositories that map to `NormalizationJob` aggregate
- `IJobProgress` implementation publishes domain events and updates aggregate state
- Background worker depends only on Application layer interfaces, not domain directly

**Concrete Integration Points**
```csharp
// Domain Events
public record JobStarted(Guid JobId, Guid DataSetId, string OperationType);
public record JobProgressUpdated(Guid JobId, int Percentage, string Message);
public record JobCompleted(Guid JobId, object? Result);
public record JobFailed(Guid JobId, string Error, int RetryCount);

// Application Command Handler
public class SubmitJobCommandHandler : IRequestHandler<SubmitJobCommand, Guid>
{
    public async Task<Guid> Handle(SubmitJobCommand request, CancellationToken ct)
    {
        var job = NormalizationJob.Create(request.DataSetId, request.OperationType, request.Parameters);
        await _jobRepository.SaveAsync(job, ct);
        await _eventPublisher.PublishAsync(new JobStarted(job.Id, job.DataSetId, job.OperationType), ct);
        return job.Id;
    }
}

// Application Router (implements INormalizationJobRouter)
public class NormalizationJobRouter : INormalizationJobRouter
{
    public async Task HandleAsync(NormalizationJob job, IJobProgress progress, CancellationToken ct)
    {
        var handler = _handlerFactory.CreateHandler(job.OperationType);
        await handler.HandleAsync(job, progress, ct);
    }
}
```

### Updated Migration Strategy (Combined)

**Phase 1: Domain Foundation**
1. Create `Normaize.DataNormalization.Domain` with `NormalizationJob` aggregate
2. Define value objects for job parameters and domain events
3. Create repository interfaces in domain layer

**Phase 2: Application Layer + Background Contracts**
1. Create `Normaize.DataNormalization.Application` with command/query handlers
2. Implement `INormalizationJobRouter` and `IJobProgress` interfaces in Application layer
3. Create handler interfaces (`IRemoveDuplicatesHandler`) in Application layer

**Phase 3: Infrastructure Implementation**
1. Implement `IJobQueue` using EF Core repositories that map to `NormalizationJob` aggregate
2. Implement `IJobProgress` to publish domain events and update aggregate state
3. Create concrete handlers (`RemoveDuplicatesHandler`) in Infrastructure layer

**Phase 4: Worker Integration**
1. Implement `IBackgroundWorker` that depends on Application layer interfaces
2. Create `WorkerHostedService` that hosts the new worker
3. Add feature flag to switch between old and new implementations

**Phase 5: API Migration**
1. Update controllers to use Application command/query handlers
2. Remove direct repository access from controllers
3. Ensure API responses are driven by domain events

### Updated Project Structure (Combined)

```
src/
  Normaize.DataNormalization.Domain/
    Aggregates/
      NormalizationJob.cs
    ValueObjects/
      DuplicateRemovalOptions.cs
      RetentionStrategy.cs
    Events/
      JobStarted.cs
      JobCompleted.cs
      JobFailed.cs
    Repositories/
      INormalizationJobRepository.cs
      
  Normaize.DataNormalization.Application/
    Commands/
      SubmitJobCommand.cs
      SubmitJobCommandHandler.cs
    Queries/
      GetJobStatusQuery.cs
      GetJobStatusQueryHandler.cs
    Interfaces/
      INormalizationJobRouter.cs
      IJobProgress.cs
      IRemoveDuplicatesHandler.cs
    DTOs/
      JobStatusDto.cs
      
  Normaize.DataNormalization.Infrastructure/
    Repositories/
      NormalizationJobRepository.cs
    Handlers/
      RemoveDuplicatesHandler.cs
    Services/
      JobQueueService.cs (implements IJobQueue)
      JobProgressService.cs (implements IJobProgress)
      NormalizationJobRouter.cs (implements INormalizationJobRouter)
    Workers/
      NormalizationWorker.cs (implements IBackgroundWorker)
      WorkerHostedService.cs
```

### Next Steps (Combined Approach)
1. Create `Normaize.DataNormalization.Domain` project with `NormalizationJob` aggregate and domain events
2. Create `Normaize.DataNormalization.Application` project with command/query handlers and background processing interfaces
3. Implement `Normaize.DataNormalization.Infrastructure` with EF mappings, repositories, and concrete handlers
4. Create new background worker that uses Application layer interfaces
5. Add feature flag to switch between implementations
6. Update API controllers to use Application layer
7. Remove old background service and direct repository access


