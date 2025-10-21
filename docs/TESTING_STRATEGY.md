# Testing Strategy for DDD Data Normalization

## Overview

This document outlines the comprehensive testing strategy for the DDD-based data normalization system. The testing structure mirrors the domain architecture and follows industry best practices for maintainable, reliable tests.

## Test Project Structure

```
tests/
├── Normaize.DataNormalization.Domain.Tests/          # Pure domain testing
│   ├── Aggregates/                                   # Aggregate root tests
│   ├── ValueObjects/                                 # Value object tests
│   └── Events/                                       # Domain event tests
├── Normaize.DataNormalization.Application.Tests/     # Use case testing
│   ├── Commands/                                     # Command handler tests
│   ├── Queries/                                      # Query handler tests
│   └── Services/                                      # Application service tests
└── Normaize.DataNormalization.Infrastructure.Tests/ # Integration testing
    ├── Services/                                      # Infrastructure service tests
    ├── Repositories/                                  # Repository implementation tests
    └── Handlers/                                      # Concrete handler tests
```

## Testing Patterns by Layer

### Domain Layer Tests (`Domain.Tests`)

**Purpose**: Test pure business logic without external dependencies

**Characteristics**:
- No mocking required
- Fast execution
- Focus on business rules and invariants
- Test domain events and state transitions

**Example Patterns**:
```csharp
[Fact]
public void Start_WhenQueued_ShouldTransitionToProcessing()
{
    // Arrange
    var job = CreateValidJob();

    // Act
    job.Start();

    // Assert
    job.Status.Should().Be(JobStatus.Processing);
    job.DomainEvents.Should().ContainSingle(e => e is JobStarted);
}
```

**What to Test**:
- Aggregate creation and factory methods
- State transitions and business rules
- Domain event emission
- Value object validation
- Invariant enforcement

### Application Layer Tests (`Application.Tests`)

**Purpose**: Test use case orchestration with mocked dependencies

**Characteristics**:
- Mock external dependencies (repositories, services)
- Test command/query handlers
- Verify orchestration logic
- Test error handling and validation

**Example Patterns**:
```csharp
[Fact]
public async Task HandleAsync_WithValidCommand_ShouldCreateAndEnqueueJob()
{
    // Arrange
    var command = new SubmitJobCommand(dataSetId, operationType, parameters);

    // Act
    var result = await _handler.HandleAsync(command);

    // Assert
    _mockRepository.Verify(r => r.SaveAsync(It.Is<NormalizationJob>(j =>
        j.DataSetId == command.DataSetId &&
        j.Status == JobStatus.Queued
    )), Times.Once);
}
```

**What to Test**:
- Command/query handler logic
- Input validation
- Dependency interaction
- Error scenarios
- Transaction boundaries

### Infrastructure Layer Tests (`Infrastructure.Tests`)

**Purpose**: Test concrete implementations and integration points

**Characteristics**:
- Mock external dependencies (databases, APIs)
- Test adapter implementations
- Verify mapping logic
- Test error handling and retries

**Example Patterns**:
```csharp
[Fact]
public async Task EnqueueAsync_WithValidJob_ShouldSaveJob()
{
    // Arrange
    var job = NormalizationJob.Create(dataSetId, operationType, parameters);

    // Act
    await _jobQueue.EnqueueAsync(job);

    // Assert
    _mockRepository.Verify(r => r.SaveAsync(job), Times.Once);
}
```

**What to Test**:
- Repository implementations
- Service adapters
- Mapping configurations
- Error handling
- Retry logic

## Test Organization Principles

### 1. Mirror Production Structure
- Test folders match source folders
- Test classes mirror production classes
- Test methods follow `MethodName_Scenario_ExpectedBehavior` pattern

### 2. Clear Test Names
```csharp
// Good
public void Start_WhenQueued_ShouldTransitionToProcessing()
public void UpdateProgress_WhenNotProcessing_ShouldThrowInvalidOperationException()
public async Task HandleAsync_WithValidCommand_ShouldCreateAndEnqueueJob()

// Avoid
public void TestStart()
public void TestUpdateProgress()
public void TestHandleAsync()
```

### 3. Arrange-Act-Assert Pattern
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and mocks
    var job = CreateValidJob();
    
    // Act - Execute the method under test
    job.Start();
    
    // Assert - Verify the expected outcome
    job.Status.Should().Be(JobStatus.Processing);
}
```

### 4. Test Data Builders
```csharp
private static NormalizationJob CreateValidJob()
{
    return NormalizationJob.Create(
        Guid.NewGuid(),
        "REMOVE_DUPLICATE_ROWS",
        "{\"columns\":[\"name\",\"email\"]}");
}
```

## Testing Tools and Libraries

### Primary Testing Framework
- **xUnit**: Primary testing framework
- **FluentAssertions**: Readable assertion syntax
- **Moq**: Mocking framework for dependencies

### Test Categories
```csharp
[Trait("Category", "Unit")]        // Fast, isolated tests
[Trait("Category", "Integration")] // Tests with external dependencies
[Trait("Category", "Contract")]    // Interface contract tests
```

## Test Execution Strategy

### Local Development
```bash
# Run all tests
dotnet test

# Run specific layer tests
dotnet test tests/Normaize.DataNormalization.Domain.Tests
dotnet test tests/Normaize.DataNormalization.Application.Tests
dotnet test tests/Normaize.DataNormalization.Infrastructure.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### CI/CD Pipeline
- Run all tests on every commit
- Generate coverage reports
- Fail build on test failures
- Run integration tests against test database

## Coverage Targets

- **Domain Layer**: 100% coverage (pure business logic)
- **Application Layer**: 95% coverage (orchestration logic)
- **Infrastructure Layer**: 90% coverage (implementation details)

## Best Practices

### 1. Test Independence
- Each test should be independent
- No shared state between tests
- Use fresh instances for each test

### 2. Fast Feedback
- Domain tests should run in milliseconds
- Application tests should run in seconds
- Infrastructure tests may take longer but should be optimized

### 3. Clear Assertions
```csharp
// Good - Specific and clear
job.Status.Should().Be(JobStatus.Processing);
job.DomainEvents.Should().ContainSingle(e => e is JobStarted);

// Avoid - Vague assertions
job.Should().NotBeNull();
Assert.True(job.Status == JobStatus.Processing);
```

### 4. Meaningful Test Data
- Use realistic test data
- Avoid magic numbers and strings
- Create test data builders for complex objects

### 5. Error Testing
- Test both happy path and error scenarios
- Verify exception types and messages
- Test boundary conditions

## Integration Testing Strategy

### Database Integration Tests
- Use in-memory database for fast execution
- Test repository implementations
- Verify EF Core mappings

### API Integration Tests
- Test complete request/response cycles
- Verify authentication and authorization
- Test error handling and status codes

### Background Processing Tests
- Test job queue operations
- Verify retry logic
- Test graceful shutdown scenarios

## Maintenance Guidelines

### 1. Keep Tests Simple
- One assertion per test when possible
- Clear test names that explain the scenario
- Avoid complex test setup

### 2. Refactor Tests with Code
- Update tests when changing business logic
- Maintain test coverage during refactoring
- Remove obsolete tests

### 3. Monitor Test Performance
- Track test execution time
- Optimize slow tests
- Consider test parallelization

This testing strategy ensures comprehensive coverage while maintaining fast feedback and clear organization that mirrors the production code structure.
