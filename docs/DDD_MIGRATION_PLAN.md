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

### Detailed Migration Strategy (Infrastructure-First Approach)

The migration follows a **strangler fig pattern** - building new functionality alongside existing code and gradually replacing it. This minimizes risk and allows for incremental validation.

#### Phase 1: Infrastructure Foundation (Week 1-2)
**Goal**: Establish the new DDD infrastructure without touching existing functionality

1. **Complete Domain Layer**
   - ✅ Create `NormalizationJob` aggregate (DONE)
   - ✅ Define domain events (DONE)
   - ✅ Create repository interfaces (DONE)
   - Add value objects for job parameters (`DuplicateRemovalOptions`, `RetentionStrategy`)

2. **Complete Application Layer**
   - ✅ Create command/query contracts (DONE)
   - ✅ Create background processing interfaces (DONE)
   - Implement concrete command/query handlers
   - Add application service interfaces

3. **Complete Infrastructure Layer**
   - ✅ Create basic service implementations (DONE)
   - Implement EF Core repository (`NormalizationJobRepository`)
   - Create EF Core configurations and mappings
   - Implement concrete handlers (`RemoveDuplicatesHandler`)
   - Add database migrations for new tables

4. **Testing Infrastructure**
   - ✅ Set up test projects (DONE)
   - Add integration test database setup
   - Create test data builders
   - Add contract tests for repository interfaces

#### Phase 2: Background Processing Migration (Week 3)
**Goal**: Replace the existing background service with DDD-based implementation

1. **Implement New Background Worker**
   - Create `IBackgroundWorker` implementation
   - Create `WorkerHostedService` wrapper
   - Implement job router with operation-specific handlers
   - Add heartbeat and visibility timeout logic

2. **Feature Flag Implementation**
   - Add configuration for `UseNewBackgroundWorker`
   - Create service registration that switches between old/new workers
   - Add monitoring and metrics for both implementations

3. **Parallel Operation**
   - Run both workers simultaneously during transition
   - Compare job processing results
   - Monitor performance and error rates
   - Gradually increase traffic to new worker

4. **Validation & Rollback**
   - Add comprehensive logging for comparison
   - Create rollback procedures
   - Monitor job completion rates and error patterns

#### Phase 3: API Endpoint Migration (Week 4-6)
**Goal**: Migrate API endpoints one by one to use Application layer

**Migration Order** (Low Risk → High Risk):
1. **Get Job Status** (`GET /api/normalization/jobs/{id}`)
   - Low risk: Read-only operation
   - Simple query handler implementation
   - Easy to validate results

2. **List Jobs** (`GET /api/normalization/jobs`)
   - Read-only operation
   - Add pagination and filtering
   - Validate against existing endpoint

3. **Submit Job** (`POST /api/normalization/jobs`)
   - Higher risk: Creates new jobs
   - Command handler implementation
   - Validate job creation and queuing

4. **Retry Job** (`POST /api/normalization/jobs/{id}/retry`)
   - Complex business logic
   - State transition validation
   - Error handling verification

5. **Cancel Job** (`DELETE /api/normalization/jobs/{id}`)
   - State management complexity
   - Background worker coordination
   - Final validation step

**Per-Endpoint Migration Process**:
```csharp
// 1. Create Application layer handler
public class GetJobStatusQueryHandler : IQueryHandler<GetJobStatusQuery, JobStatusDto?>
{
    // Implementation using new DDD structure
}

// 2. Add feature flag to controller
[HttpGet("{id}")]
public async Task<IActionResult> GetJobStatus(Guid id)
{
    if (_featureFlags.UseNewDataNormalization)
    {
        var query = new GetJobStatusQuery(id);
        var result = await _queryHandler.HandleAsync(query);
        return Ok(result);
    }
    
    // Existing implementation
    return await _legacyService.GetJobStatusAsync(id);
}

// 3. Test both paths
// 4. Monitor and validate
// 5. Remove feature flag and legacy code
```

#### Phase 4: Data Migration & Cleanup (Week 7-8)
**Goal**: Migrate existing data and remove legacy code

1. **Data Migration**
   - Create migration scripts for existing `DataNormalizationJob` records
   - Map to new `NormalizationJob` aggregate structure
   - Validate data integrity
   - Handle edge cases and corrupted data

2. **Legacy Code Removal**
   - Remove old background service
   - Remove legacy repository implementations
   - Remove unused interfaces and DTOs
   - Clean up old test files

3. **Final Validation**
   - Run comprehensive integration tests
   - Performance testing
   - Load testing with production-like data
   - Security review

#### Phase 5: Documentation & Training (Week 9)
**Goal**: Document new patterns and train team

1. **Update Documentation**
   - API documentation
   - Architecture diagrams
   - Development guidelines
   - Troubleshooting guides

2. **Team Training**
   - DDD patterns and practices
   - New testing strategies
   - Debugging techniques
   - Performance monitoring

### Risk Mitigation Strategies

#### Technical Risks
- **Data Loss**: Parallel operation with validation
- **Performance Regression**: Load testing and monitoring
- **Integration Issues**: Comprehensive integration tests
- **Rollback Complexity**: Feature flags and staged rollouts

#### Operational Risks
- **Team Knowledge Gap**: Pair programming and documentation
- **Timeline Pressure**: Phased approach with clear milestones
- **Scope Creep**: Strict adherence to migration phases

### Success Metrics

#### Technical Metrics
- Test coverage > 90% for new code
- Performance within 10% of baseline
- Zero data loss during migration
- Error rates < 0.1%

#### Process Metrics
- Migration completed within 9 weeks
- Team confidence score > 8/10
- Documentation completeness > 95%
- Rollback time < 30 minutes

### Rollback Procedures

#### Immediate Rollback (< 30 minutes)
1. Disable feature flags
2. Stop new background worker
3. Restart old background worker
4. Verify system stability

#### Full Rollback (< 4 hours)
1. Revert code changes
2. Restore database from backup
3. Redeploy previous version
4. Validate all functionality

### Monitoring & Alerting

#### Key Metrics to Monitor
- Job processing rates
- Error rates by endpoint
- Response times
- Memory usage
- Database performance

#### Alert Thresholds
- Error rate > 1%
- Response time > 2x baseline
- Memory usage > 80%
- Job queue depth > 1000

This migration strategy prioritizes infrastructure first because it:
1. **Reduces Risk**: Infrastructure changes don't affect user-facing APIs
2. **Enables Validation**: Can test new patterns without breaking existing functionality
3. **Provides Foundation**: Creates the base for API migrations
4. **Allows Rollback**: Easy to revert infrastructure changes if issues arise

## 🚀 **NEXT STEPS: Database Schema Migration & Entity Mapping**

### **Phase 1a: Complete Database Foundation (Week 2-3)**

Before proceeding to service migration, we need to establish complete database schema compatibility between the existing system and our new DDD bounded context. Currently, we have a mismatch:

**Current Status:**
- ✅ New DDD `NormalizationJob` aggregate designed
- ✅ New `DataNormalizationDbContext` with basic schema
- ❌ Missing entities: `DataSet`, `UserSettings`, `Analysis`, `AuditLogs`
- ❌ No data migration strategy from existing `DataNormalizationJob` → new `NormalizationJob`

### **Critical Database Mapping Issues to Resolve**

#### 1. **Entity Mismatch Analysis**

**Existing System Entities:**
```csharp
// In main NormaizeContext
DataSet                    // Core entity - we need this!
DataNormalizationJob       // Maps to our NormalizationJob aggregate  
DataNormalizationAuditLog  // Maps to domain events
UserSettings              // Cross-cutting concern
Analysis                  // Related to dataset processing
DataSetRow                // Raw data storage
DataSetAuditLog           // Dataset audit trail
```

**Our DDD Context Currently Has:**
```csharp
// In DataNormalizationDbContext  
NormalizationJob          // ✅ Main aggregate
// Missing everything else! ❌
```

#### 2. **Required Entity Mappings for DDD Context**

To make our DDD implementation functional, we need to add these entities to our `DataNormalizationDbContext`:

**Priority 1: Essential Entities**
- `DataSet` - Required by `NormalizationJob.DataSetId` foreign key
- `DataNormalizationAuditLog` - Required for domain events persistence
- Migration mapping from existing `DataNormalizationJob` → `NormalizationJob`

**Priority 2: Integration Entities** 
- `UserSettings` - For user preferences in normalization operations
- `Analysis` - For linking normalization results to analysis workflows

#### 3. **Detailed Implementation Steps**

**Step 1: Add Missing Entities to DDD Context (Week 2)**

1. **Create DataSet Entity in DDD Context**
   ```csharp
   // Add to DataNormalizationDbContext
   public DbSet<DataSet> DataSets { get; set; }
   
   // Map existing DataSet model or create DDD-specific version
   modelBuilder.Entity<DataSet>(entity =>
   {
       entity.ToTable("datasets"); // Map to existing table
       // Configure to match existing schema
   });
   ```

2. **Add AuditLog Entity for Domain Events**
   ```csharp
   public DbSet<NormalizationAuditLog> AuditLogs { get; set; }
   
   // Either reuse existing DataNormalizationAuditLog or create new
   modelBuilder.Entity<NormalizationAuditLog>(entity =>
   {
       entity.ToTable("data_normalization_audit_logs", "data_normalization");
       // Map domain events to audit records
   });
   ```

3. **Update NormalizationJob Aggregate**
   ```csharp
   // Add navigation properties
   public DataSet DataSet { get; private set; }
   public IReadOnlyCollection<NormalizationAuditLog> AuditLogs { get; private set; }
   ```

**Step 2: Create Data Migration Strategy (Week 2)**

1. **Bidirectional Entity Mapping**
   ```csharp
   // Extension methods for mapping between contexts
   public static class EntityMappingExtensions
   {
       public static NormalizationJob ToNormalizationJob(this DataNormalizationJob legacy)
       {
           return NormalizationJob.Create(
               legacy.DataSetId,
               legacy.OperationType,
               legacy.OperationParameters ?? "{}",
               legacy.MaxRetries
           );
       }
       
       public static DataNormalizationJob ToLegacyJob(this NormalizationJob domainJob)
       {
           // Map back for legacy compatibility
       }
   }
   ```

2. **Migration Scripts**
   ```sql
   -- Copy existing data to new schema
   INSERT INTO data_normalization.normalization_jobs 
   SELECT id, dataset_id, operation_type, operation_parameters, 
          status, retry_count, max_retries, created_at, started_at, 
          completed_at, error_message, result, progress_percentage
   FROM data_normalization_jobs 
   WHERE is_deleted = false;
   ```

**Step 3: Dual-Write Pattern Implementation (Week 3)**

During transition period, implement dual-write to both schemas:

```csharp
public class DualContextJobRepository : INormalizationJobRepository
{
    private readonly NormaizeContext _legacyContext;
    private readonly DataNormalizationDbContext _dddContext;
    
    public async Task SaveAsync(NormalizationJob job)
    {
        // Write to new DDD context
        await _dddContext.NormalizationJobs.AddAsync(job);
        await _dddContext.SaveChangesAsync();
        
        // Also write to legacy context for compatibility
        var legacyJob = job.ToLegacyJob();
        await _legacyContext.DataNormalizationJobs.AddAsync(legacyJob);
        await _legacyContext.SaveChangesAsync();
    }
}
```

### **Phase 1b: Repository Integration & Testing (Week 3-4)**

**Step 4: Integration Repository Pattern**

1. **Cross-Context Repository**
   ```csharp
   public class IntegratedNormalizationRepository : INormalizationJobRepository
   {
       private readonly IFeatureFlags _featureFlags;
       private readonly DataNormalizationDbContext _dddContext;
       private readonly NormaizeContext _legacyContext;
       
       public async Task<NormalizationJob?> GetByIdAsync(Guid jobId)
       {
           if (_featureFlags.UseNewDataNormalizationSchema)
           {
               return await _dddContext.NormalizationJobs
                   .Include(j => j.DataSet)
                   .FirstOrDefaultAsync(j => j.Id == jobId);
           }
           
           // Fallback to legacy and map
           var legacyJob = await _legacyContext.DataNormalizationJobs
               .Include(j => j.DataSet)
               .FirstOrDefaultAsync(j => j.Id == jobId.ToString());
               
           return legacyJob?.ToNormalizationJob();
       }
   }
   ```

2. **Data Consistency Validation**
   ```csharp
   public class DataMigrationValidator
   {
       public async Task ValidateDataConsistency()
       {
           // Compare counts
           var legacyCount = await _legacyContext.DataNormalizationJobs.CountAsync();
           var dddCount = await _dddContext.NormalizationJobs.CountAsync();
           
           // Compare sample records
           // Validate foreign key integrity
           // Check business rule compliance
       }
   }
   ```

**Step 5: Enhanced Testing Strategy**

```csharp
[Fact]
public async Task Repository_ShouldWorkWithBothSchemas()
{
    // Arrange
    var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");
    
    // Act - Save to both contexts
    await _dualRepository.SaveAsync(job);
    
    // Assert - Verify in both contexts
    var dddJob = await _dddContext.NormalizationJobs.FindAsync(job.Id);
    var legacyJob = await _legacyContext.DataNormalizationJobs.FindAsync(job.Id.ToString());
    
    Assert.NotNull(dddJob);
    Assert.NotNull(legacyJob);
    Assert.Equal(dddJob.OperationType, legacyJob.OperationType);
}
```

### **Phase 1c: Background Worker Integration (Week 4-5)**

**Step 6: Worker Compatibility**

1. **Dual-Schema Worker**
   ```csharp
   public class CompatibleNormalizationWorker : IBackgroundWorker
   {
       public async Task ProcessJobsAsync(CancellationToken ct)
       {
           // Try new schema first
           var newJob = await _dddQueue.DequeueAsync();
           if (newJob != null)
           {
               await ProcessDDDJob(newJob);
               return;
           }
           
           // Fallback to legacy schema
           var legacyJob = await _legacyQueue.DequeueAsync();
           if (legacyJob != null)
           {
               await ProcessLegacyJob(legacyJob);
           }
       }
   }
   ```

2. **Feature Flag Integration**
   ```csharp
   services.AddScoped<INormalizationJobRepository>(provider =>
   {
       var featureFlags = provider.GetService<IFeatureFlags>();
       if (featureFlags.UseNewDataNormalizationSchema)
       {
           return provider.GetService<DataNormalizationRepository>();
       }
       return provider.GetService<LegacyNormalizationRepository>();
   });
   ```

### **Updated Timeline with Database Foundation**

**Week 1-2: Database Schema Completion**
- ✅ Add missing entities to DataNormalizationDbContext
- ✅ Create entity mapping extensions  
- ✅ Implement dual-write repositories
- ✅ Create data migration scripts

**Week 3-4: Integration & Validation**  
- ✅ Cross-context repository implementation
- ✅ Data consistency validation tools
- ✅ Enhanced integration tests
- ✅ Performance benchmarking

**Week 5-6: Background Processing Migration**
- ✅ Compatible background worker
- ✅ Feature flag integration
- ✅ Parallel operation validation
- ✅ Migration tooling

**Week 7-8: API Endpoint Migration**
- ✅ Migrate individual endpoints
- ✅ Validate business logic compatibility
- ✅ Performance monitoring

**Week 9: Final Migration & Cleanup**
- ✅ Remove legacy schema dependencies
- ✅ Clean up dual-write code
- ✅ Performance optimization

### **Risk Mitigation for Database Migration**

**Data Integrity Risks:**
- **Dual-write validation**: Automated tests comparing both schemas
- **Rollback strategy**: Quick schema rollback with data restoration
- **Foreign key validation**: Automated FK integrity checks

**Performance Risks:**
- **Dual-write overhead**: Monitor performance impact during transition
- **Index optimization**: Ensure new schema has proper indexes
- **Connection pooling**: Separate pools for each context

**Compatibility Risks:**
- **Schema evolution**: Version compatibility between old/new schemas
- **Business rule validation**: Ensure domain invariants work with existing data
- **API contract preservation**: Maintain existing API behavior

### **Success Criteria for Database Migration**

1. **Zero Data Loss**: All existing records migrated successfully
2. **Performance Parity**: New schema performs within 10% of existing
3. **Business Rule Compliance**: All domain invariants validated against existing data
4. **Test Coverage**: 100% test coverage for entity mapping and dual-write scenarios
5. **Rollback Capability**: < 30 minute rollback to legacy schema

This database-first approach ensures we have a solid foundation before migrating services and APIs, reducing risk and ensuring compatibility throughout the migration process.


