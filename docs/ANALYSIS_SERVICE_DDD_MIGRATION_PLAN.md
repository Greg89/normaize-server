# Analysis Service DDD Migration Plan

## Overview
This document outlines the migration strategy for `IDataAnalysisService` from the legacy `Normaize.Core` project to the new DDD architecture. The goal is to preserve all functionality while implementing proper domain-driven design principles.

## Legacy Service Analysis

### Current IDataAnalysisService Methods
1. `CreateAnalysisAsync(CreateAnalysisDto)` - Creates new analysis
2. `GetAnalysisAsync(int id)` - Retrieves analysis by ID
3. `GetAnalysesByDataSetAsync(int dataSetId)` - Gets analyses for dataset
4. `GetAnalysesByStatusAsync(AnalysisStatus status)` - Gets analyses by status
5. `GetAnalysesByTypeAsync(AnalysisType type)` - Gets analyses by type
6. `GetAnalysisResultAsync(int analysisId)` - Gets analysis results
7. `DeleteAnalysisAsync(int id)` - Soft deletes analysis
8. `RunAnalysisAsync(int analysisId)` - Executes analysis operation

### Key Legacy Features to Preserve
- **8 Analysis Types**: Normalization, Comparison, Statistical, DataCleaning, OutlierDetection, CorrelationAnalysis, TrendAnalysis, Custom
- **State Management**: Pending → Processing → Completed/Failed workflow
- **Chaos Engineering**: Resilience testing capabilities
- **Comprehensive Logging**: Structured logging with operation context
- **Timeout Handling**: Configurable timeouts for operations
- **Result Serialization**: JSON storage and retrieval of analysis results
- **Error Handling**: Detailed error messages and failure states
- **Validation**: Input validation for DTOs and business rules

## DDD Architecture Design

### 1. Domain Layer (`src/Normaize.DataNormalization.Domain`)

#### Analysis Aggregate Root
```csharp
// Aggregates/AnalysisAggregate/Analysis.cs
public class Analysis : Entity, IAggregateRoot
{
    public AnalysisId Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public AnalysisType Type { get; private set; }
    public AnalysisStatus Status { get; private set; }
    public DataSetId DataSetId { get; private set; }
    public DataSetId? ComparisonDataSetId { get; private set; }
    public AnalysisConfiguration? Configuration { get; private set; }
    public AnalysisResult? Result { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    // Domain methods with business logic
    public void Run()
    public void Complete(AnalysisResult result)
    public void Fail(string errorMessage)
    public void Cancel()
}
```

#### Value Objects
```csharp
// ValueObjects/AnalysisId.cs
public record AnalysisId(int Value) : IValueObject;

// ValueObjects/AnalysisConfiguration.cs
public record AnalysisConfiguration(string JsonConfiguration) : IValueObject;

// ValueObjects/AnalysisResult.cs
public record AnalysisResult(string JsonResult) : IValueObject;
```

#### Domain Events
```csharp
// Events/AnalysisCreatedEvent.cs
public record AnalysisCreatedEvent(AnalysisId AnalysisId) : IDomainEvent;

// Events/AnalysisStartedEvent.cs
public record AnalysisStartedEvent(AnalysisId AnalysisId) : IDomainEvent;

// Events/AnalysisCompletedEvent.cs
public record AnalysisCompletedEvent(AnalysisId AnalysisId, AnalysisResult Result) : IDomainEvent;

// Events/AnalysisFailedEvent.cs
public record AnalysisFailedEvent(AnalysisId AnalysisId, string ErrorMessage) : IDomainEvent;
```

#### Repository Interface
```csharp
// Repositories/IAnalysisRepository.cs
public interface IAnalysisRepository
{
    Task<Analysis?> GetByIdAsync(AnalysisId id);
    Task<IEnumerable<Analysis>> GetByDataSetIdAsync(DataSetId dataSetId);
    Task<IEnumerable<Analysis>> GetByStatusAsync(AnalysisStatus status);
    Task<IEnumerable<Analysis>> GetByTypeAsync(AnalysisType type);
    Task<Analysis> AddAsync(Analysis analysis);
    Task<Analysis> UpdateAsync(Analysis analysis);
    Task<bool> DeleteAsync(AnalysisId id);
}
```

### 2. Application Layer (`src/Normaize.DataNormalization.Application`)

#### Commands
```csharp
// Commands/CreateAnalysisCommand.cs
public record CreateAnalysisCommand(
    string Name,
    string? Description,
    AnalysisType Type,
    int DataSetId,
    int? ComparisonDataSetId,
    string? Configuration
) : IRequest<AnalysisDto>;

// Commands/RunAnalysisCommand.cs
public record RunAnalysisCommand(int AnalysisId) : IRequest<AnalysisDto>;

// Commands/DeleteAnalysisCommand.cs
public record DeleteAnalysisCommand(int AnalysisId) : IRequest<bool>;
```

#### Queries
```csharp
// Queries/GetAnalysisQuery.cs
public record GetAnalysisQuery(int AnalysisId) : IRequest<AnalysisDto?>;

// Queries/GetAnalysesByDataSetQuery.cs
public record GetAnalysesByDataSetQuery(int DataSetId) : IRequest<IEnumerable<AnalysisDto>>;

// Queries/GetAnalysesByStatusQuery.cs
public record GetAnalysesByStatusQuery(AnalysisStatus Status) : IRequest<IEnumerable<AnalysisDto>>;

// Queries/GetAnalysesByTypeQuery.cs
public record GetAnalysesByTypeQuery(AnalysisType Type) : IRequest<IEnumerable<AnalysisDto>>;

// Queries/GetAnalysisResultQuery.cs
public record GetAnalysisResultQuery(int AnalysisId) : IRequest<AnalysisResultDto>;
```

#### Command/Query Handlers
- Each command/query will have dedicated handlers with proper validation
- Handlers will use domain services and repositories
- Event publishing for domain events

#### Application Services
```csharp
// Interfaces/IAnalysisExecutionService.cs
public interface IAnalysisExecutionService
{
    Task<AnalysisResult> ExecuteAsync(Analysis analysis);
}
```

### 3. Infrastructure Layer (`src/Normaize.DataNormalization.Infrastructure`)

#### Repository Implementation
```csharp
// Repositories/AnalysisRepository.cs
public class AnalysisRepository : IAnalysisRepository
{
    // EF Core implementation with proper mapping
}
```

#### Analysis Execution Service
```csharp
// Services/AnalysisExecutionService.cs
public class AnalysisExecutionService : IAnalysisExecutionService
{
    // Implementation of all 8 analysis types from legacy service
    // Chaos engineering integration
    // Timeout handling
    // Structured logging
}
```

#### Event Handlers
```csharp
// EventHandlers/AnalysisEventHandlers.cs
public class AnalysisCreatedEventHandler : INotificationHandler<AnalysisCreatedEvent>
public class AnalysisCompletedEventHandler : INotificationHandler<AnalysisCompletedEvent>
// etc.
```

### 4. API Layer (`src/Normaize.DataNormalization.API`)

#### Controllers
```csharp
// Controllers/AnalysisController.cs
[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AnalysisDto>> CreateAnalysis([FromBody] CreateAnalysisCommand command)
    
    [HttpGet("{id}")]
    public async Task<ActionResult<AnalysisDto>> GetAnalysis(int id)
    
    [HttpPost("{id}/run")]
    public async Task<ActionResult<AnalysisDto>> RunAnalysis(int id)
    
    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> DeleteAnalysis(int id)
    
    [HttpGet("dataset/{dataSetId}")]
    public async Task<ActionResult<IEnumerable<AnalysisDto>>> GetAnalysesByDataSet(int dataSetId)
    
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<AnalysisDto>>> GetAnalysesByStatus(AnalysisStatus status)
    
    [HttpGet("type/{type}")]
    public async Task<ActionResult<IEnumerable<AnalysisDto>>> GetAnalysesByType(AnalysisType type)
    
    [HttpGet("{id}/result")]
    public async Task<ActionResult<AnalysisResultDto>> GetAnalysisResult(int id)
}
```

## Migration Strategy

### Phase 1: Domain Layer
1. Create Analysis aggregate with proper business logic
2. Implement value objects and entities
3. Define domain events
4. Create repository interface

### Phase 2: Application Layer  
1. Implement CQRS commands and queries
2. Create command/query handlers
3. Implement application services
4. Add DTOs and mapping

### Phase 3: Infrastructure Layer
1. Implement repository with EF Core
2. Create analysis execution service
3. Implement event handlers
4. Add chaos engineering integration

### Phase 4: API Layer
1. Create analysis controller
2. Implement all 8 API endpoints
3. Add proper error handling and validation

### Phase 5: Testing
1. Unit tests for domain logic
2. Integration tests for repositories
3. API tests for controllers
4. End-to-end functionality tests

## Testing Strategy

### Test Coverage Requirements
- **Domain Layer**: 95% code coverage
- **Application Layer**: 90% code coverage  
- **Infrastructure Layer**: 85% code coverage
- **API Layer**: 90% code coverage

### Key Test Scenarios
1. **Analysis Creation**: Valid/invalid DTOs, business rule validation
2. **Analysis Execution**: All 8 analysis types, timeout scenarios, chaos engineering
3. **State Transitions**: Pending → Processing → Completed/Failed workflows
4. **Error Handling**: Network failures, database timeouts, validation errors
5. **Concurrent Operations**: Multiple analyses, race conditions
6. **Integration**: End-to-end workflows with real database

## Migration Validation

### Functionality Parity Checklist
- [ ] All 8 analysis types work identically to legacy service
- [ ] State management preserves exact behavior
- [ ] Error messages match legacy implementation
- [ ] Timeout handling behaves consistently
- [ ] Chaos engineering features maintained
- [ ] Performance characteristics preserved
- [ ] API contracts remain compatible

### Success Criteria
1. All existing analysis functionality preserved
2. No breaking changes to API contracts
3. Performance equal or better than legacy
4. 100% test coverage for critical paths
5. Clean DDD architecture implementation
6. Proper domain event handling
7. Comprehensive error handling

## Implementation Notes

### Considerations
- Maintain backward compatibility with existing analysis data
- Preserve chaos engineering features for resilience testing
- Keep structured logging patterns consistent
- Ensure timeout configurations are preserved
- Maintain JSON serialization formats for results

### Dependencies
- Analysis service depends on DataSet aggregate (already migrated)
- Requires infrastructure abstractions for chaos engineering
- Needs structured logging service integration
- Requires event bus for domain events

This migration will serve as the template for migrating other complex services from the legacy Core project.