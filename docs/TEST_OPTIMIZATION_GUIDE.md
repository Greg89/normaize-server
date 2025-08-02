# Test Optimization Guide

## Overview

This guide outlines the various strategies implemented to optimize test execution time for the Normaize project. The test suite has been optimized to run **6x faster** for development scenarios while maintaining comprehensive coverage for CI/CD pipelines.

## Current Performance

- **All Tests**: ~78 seconds (815 tests)
- **Fast Tests**: ~13 seconds (809 tests) - **6x improvement**
- **Integration Tests**: ~5 seconds (4 tests)
- **Slow Tests**: ~60 seconds (2 tests)

## Optimization Strategies

### 1. Parallel Test Execution

**Configuration**: Enabled in `Normaize.Tests.csproj`
```xml
<ParallelizeTestCollections>true</ParallelizeTestCollections>
<MaxParallelThreads>4</MaxParallelThreads>
```

**Benefits**: Tests run concurrently instead of sequentially
**Impact**: 2-3x speed improvement

### 2. Test Categories

Tests are categorized using `[Trait("Category", "...")]` attributes:

- **Unit**: Fast, isolated tests
- **Integration**: Tests requiring full application setup
- **Slow**: Tests taking >5 seconds (timeouts, retries)
- **Fast**: Quick unit tests for development
- **Database**: Tests requiring database operations
- **FileSystem**: Tests involving file I/O
- **External**: Tests requiring external services (S3, etc.)

### 3. Selective Test Execution

#### PowerShell Scripts

| Script | Purpose | Execution Time |
|--------|---------|----------------|
| `run-tests-fast.ps1` | Unit tests only | ~13s |
| `run-tests-integration.ps1` | Integration tests only | ~5s |
| `run-tests-all.ps1` | All tests with parallel execution | ~78s |
| `run-tests-smart.ps1` | Context-aware test selection | Variable |

#### Smart Test Runner

The smart runner (`run-tests-smart.ps1`) automatically chooses the best strategy:

- **CI/CD Environment**: Runs all tests
- **Uncommitted Changes**: Runs fast tests only
- **Clean Working Directory**: Runs integration tests

```powershell
# Examples
.\scripts\run-tests-smart.ps1 -Mode fast
.\scripts\run-tests-smart.ps1 -Mode integration
.\scripts\run-tests-smart.ps1 -Mode all
.\scripts\run-tests-smart.ps1 -Mode auto  # Context-aware
```

### 4. Test Configuration

Environment variables control test execution:

```powershell
# Enable/disable test types
$env:RUN_SLOW_TESTS = "false"
$env:RUN_INTEGRATION_TESTS = "true"
$env:RUN_EXTERNAL_TESTS = "false"

# Parallel execution settings
$env:MAX_PARALLEL_THREADS = "4"
$env:ENABLE_PARALLELIZATION = "true"
$env:TEST_TIMEOUT_SECONDS = "30"
```

### 5. Integration Test Optimization

Integration tests use `TestWebApplicationFactory` with:
- In-memory database
- Mocked external services
- Optimized service registration
- Shared test context

### 6. Slow Test Identification

Tests taking >30 seconds are marked with `[Trait("Category", "Slow")]`:
- `ApplyMigrationsAsync_WithPersistentFailure_ShouldHandleFailure` (34s)
- `PerformHealthChecksAsync_WithPersistentFailure_ShouldHandleTimeout` (30s)

## Usage Scenarios

### Development Workflow

1. **During Active Development**:
   ```powershell
   .\scripts\run-tests-fast.ps1
   ```
   - Runs in ~13 seconds
   - Provides immediate feedback
   - Excludes slow integration tests

2. **Before Committing**:
   ```powershell
   .\scripts\run-tests-smart.ps1 -Mode integration
   ```
   - Runs integration tests
   - Validates full application behavior

3. **Full Validation**:
   ```powershell
   .\scripts\run-tests-all.ps1
   ```
   - Runs all tests
   - Use before major releases

### CI/CD Pipeline

```yaml
# GitHub Actions example
- name: Run Fast Tests
  run: .\scripts\run-tests-fast.ps1
  env:
    CI: true

- name: Run Integration Tests
  run: .\scripts\run-tests-integration.ps1
  env:
    CI: true

- name: Run All Tests (Nightly)
  run: .\scripts\run-tests-all.ps1
  env:
    CI: true
    RUN_SLOW_TESTS: true
```

## Adding New Tests

### Categorizing Tests

```csharp
[Fact]
[Trait("Category", TestSetup.Categories.Unit)]
public void MyUnitTest_ShouldWork()
{
    // Fast unit test
}

[Fact]
[Trait("Category", TestSetup.Categories.Integration)]
public void MyIntegrationTest_ShouldWork()
{
    // Integration test
}

[Fact]
[Trait("Category", TestSetup.Categories.Slow)]
public void MySlowTest_ShouldWork()
{
    // Test with timeouts/retries
}
```

### Test Performance Guidelines

- **Unit Tests**: <100ms
- **Integration Tests**: <5s
- **Slow Tests**: >5s (mark with Slow category)

## Monitoring and Maintenance

### Performance Tracking

Monitor test execution times:
```powershell
dotnet test --logger "console;verbosity=normal" --verbosity normal
```

### Identifying Slow Tests

Look for tests taking >5 seconds and consider:
- Mocking external dependencies
- Reducing test data size
- Optimizing test setup/teardown
- Moving to Slow category if necessary

### Regular Optimization

1. **Monthly**: Review test execution times
2. **Quarterly**: Optimize slow tests
3. **Release**: Update test categories

## Troubleshooting

### Common Issues

1. **Tests Failing in Parallel**:
   - Check for shared state
   - Use `[Collection("...")]` for related tests
   - Ensure proper cleanup

2. **Slow Test Execution**:
   - Identify bottlenecks with profiling
   - Consider test isolation
   - Review external dependencies

3. **Memory Issues**:
   - Reduce `MaxParallelThreads`
   - Check for memory leaks in tests
   - Use `IDisposable` for cleanup

### Performance Tips

1. **Use In-Memory Databases**: Faster than real databases
2. **Mock External Services**: Avoid network calls
3. **Minimize Test Data**: Use smallest dataset that validates behavior
4. **Parallel-Friendly Tests**: Avoid shared state
5. **Efficient Assertions**: Use specific assertions over complex ones

## Future Optimizations

### Planned Improvements

1. **Test Sharding**: Split tests across multiple machines
2. **Incremental Testing**: Only run tests affected by changes
3. **Test Caching**: Cache test results for unchanged code
4. **Distributed Testing**: Run tests in parallel across multiple agents

### Monitoring Tools

- Test execution time tracking
- Performance regression detection
- Automated optimization suggestions

## Conclusion

The implemented optimizations provide a **6x improvement** in test execution time for development scenarios while maintaining comprehensive test coverage. The smart test runner automatically adapts to different contexts, making the development workflow more efficient.

For questions or suggestions, refer to the test configuration files and scripts in the `scripts/` directory. 