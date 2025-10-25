# DDD Migration Standards & Principles

## Overview
This document defines the standards, principles, and migration roadmap for transitioning the Normaize server to a clean Domain-Driven Design (DDD) architecture. This serves as the definitive guide for maintaining consistency across development sessions and AI IDE interactions.

## Core Principles

### 1. Zero Legacy Dependencies
- **RULE**: No references to legacy projects (`Normaize.API`, `Normaize.Core`, `Normaize.Data`, `Normaize.Tests`)
- **APPROACH**: Migrate needed functionality to new DDD structure
- **VALIDATION**: Build should succeed without legacy project references

### 2. Domain-Driven Design (DDD) Architecture
- **STRUCTURE**: Follow clean architecture layers
  - `Domain`: Entities, Value Objects, Domain Services, Repositories (interfaces)
  - `Application`: Use Cases, Commands, Queries, Handlers, Application Services
  - `Infrastructure`: Repository implementations, External services, Database configurations
  - `Presentation`: API controllers, DTOs, Middleware

### 3. SOLID Principles
- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: Many client-specific interfaces over one general-purpose interface
- **Dependency Inversion**: Depend on abstractions, not concretions

### 4. Clean Code Standards
- **Naming**: Descriptive, intention-revealing names
- **Methods**: Small, focused, single purpose
- **Classes**: Cohesive, loosely coupled
- **Comments**: Explain why, not what
- **Error Handling**: Explicit, meaningful exceptions

## Migration Status

### ✅ Completed Components

#### Domain Layer
- [x] **Entities**: `DataSet`, `DataSetRow`, `NormalizationJob`, `NormalizationAuditLog`
- [x] **Value Objects**: `FileMetadata`, `DatasetStatistics`, `DuplicateRemovalOptions`, `FileType`, `StorageProvider`, `RetentionStrategy`
- [x] **Repository Interfaces**: `IDataSetRepository`, `IDataSetRowRepository`, `INormalizationJobRepository`
- [x] **Domain Events**: Base infrastructure for domain event publishing

#### Application Layer
- [x] **Commands**: `SubmitJobCommand`, `SubmitJobCommandHandler`
- [x] **Queries**: `GetJobStatusQuery`, `GetJobStatusQueryHandler`
- [x] **Interfaces**: Data loading and persistence abstractions
- [x] **DTOs**: Clean separation between layers

#### Infrastructure Layer
- [x] **Repositories**: Full implementation of all repository interfaces
- [x] **EF Core Configurations**: Proper value object conversions and entity mappings
- [x] **Services**: `RemoveDuplicatesHandler`, `NormalizationJobRouter`, `JobQueueService`, `JobProgressService`
- [x] **Workers**: `NormalizationWorker` for background processing
- [x] **Data Bridge**: Legacy data integration services

#### Testing Layer
- [x] **Domain Tests**: 107/107 tests passing (100%)
- [x] **Application Tests**: Comprehensive command and query testing
- [x] **Infrastructure Tests**: Repository and service integration tests
- [x] **Test Coverage**: 179 total tests with 100% success rate

### 🔄 Current Focus Areas

#### API Integration
- [ ] **Controllers**: Migrate legacy API controllers to use new DDD structure
- [ ] **Middleware**: Authentication, authorization, error handling
- [ ] **DTOs**: Request/response models for clean API contracts

#### Background Processing
- [ ] **Service Bus Integration**: Replace legacy message handling
- [ ] **Job Orchestration**: Complete workflow management
- [ ] **Error Recovery**: Robust failure handling and retry logic

#### File Processing
- [ ] **Upload Service**: Clean file upload and validation
- [ ] **Storage Integration**: S3/Azure blob storage abstraction
- [ ] **File Format Support**: CSV, Excel, JSON processing

## Technical Standards

### Code Quality Gates

#### Build Requirements
```bash
# All builds must pass
dotnet build --no-restore --verbosity minimal

# No warnings allowed
dotnet build --verbosity normal | findstr "warning" && exit 1

# Tests must pass 100%
dotnet test --no-build --verbosity minimal
```

#### Code Analysis
```bash
# Format must be clean
dotnet format --verify-no-changes

# Code analysis must pass
dotnet build -p:WarningsAsErrors=true
```

### Architecture Validation

#### Dependency Rules
1. **Domain** → No external dependencies except .NET primitives
2. **Application** → Can reference Domain only
3. **Infrastructure** → Can reference Domain and Application
4. **Presentation** → Can reference Application (not Domain directly)

#### Testing Strategy
- **Unit Tests**: Fast, isolated, deterministic
- **Integration Tests**: Database and external service integration
- **Architecture Tests**: Enforce dependency rules and patterns

### Naming Conventions

#### Files and Folders
```
Domain/
  Entities/
    DataSet.cs
    DataSetRow.cs
  ValueObjects/
    FileMetadata.cs
  Repositories/
    IDataSetRepository.cs

Application/
  Commands/
    SubmitJobCommand.cs
    SubmitJobCommandHandler.cs
  Queries/
    GetJobStatusQuery.cs
    GetJobStatusQueryHandler.cs

Infrastructure/
  Repositories/
    DataSetRepository.cs
  Services/
    RemoveDuplicatesHandler.cs
  Data/
    Configurations/
      DataSetConfiguration.cs
```

#### Code Elements
- **Entities**: PascalCase, meaningful business names
- **Value Objects**: PascalCase, descriptive of the value
- **Services**: PascalCase, action-oriented names ending in "Service" or "Handler"
- **Interfaces**: PascalCase, starting with "I"
- **Methods**: PascalCase, verb-oriented
- **Properties**: PascalCase, noun-oriented
- **Parameters**: camelCase, descriptive
- **Private fields**: _camelCase with underscore prefix

## Decision Log

### ✅ Accepted Decisions

1. **Entity Framework Core 9**: Primary ORM with proper value object support
2. **PostgreSQL**: Database with jsonb support for flexible data storage
3. **MediatR**: CQRS implementation for clean command/query separation
4. **xUnit + Moq**: Testing framework with comprehensive mocking
5. **Serilog**: Structured logging throughout the application
6. **Value Object Conversions**: EF Core HasConversion for proper persistence

### ❌ Rejected Approaches

1. **Legacy Project References**: Creates coupling and technical debt
2. **Anemic Domain Model**: Violates DDD principles
3. **Repository Pattern Violations**: Direct DbContext usage in application layer
4. **Primitive Obsession**: Using primitives instead of value objects
5. **Service Locator**: Violates dependency inversion principle

## Quality Checklist

### Before Each Commit
- [ ] Zero build warnings
- [ ] All tests passing (100%)
- [ ] Code formatted (dotnet format)
- [ ] No legacy project references
- [ ] Proper dependency injection registration
- [ ] Entity configurations complete
- [ ] Value objects properly mapped
- [ ] Exception handling implemented
- [ ] Logging added for operations
- [ ] Tests cover new functionality

### Before Each PR
- [ ] Architecture tests passing
- [ ] Integration tests working
- [ ] API contracts validated
- [ ] Performance benchmarks acceptable
- [ ] Security review completed
- [ ] Documentation updated
- [ ] Migration guide updated

## Next Steps Roadmap

### Phase 1: API Layer Migration (Current)
1. **Controllers**: Migrate existing endpoints to use new handlers
2. **Middleware**: Port authentication and error handling
3. **Configuration**: Environment-specific settings
4. **Health Checks**: System monitoring endpoints

### Phase 2: Background Processing Enhancement
1. **Job Scheduling**: Advanced workflow management
2. **Error Handling**: Comprehensive retry and fallback logic
3. **Monitoring**: Detailed job progress tracking
4. **Scalability**: Horizontal scaling support

### Phase 3: Advanced Features
1. **Real-time Updates**: SignalR integration for live progress
2. **Caching**: Redis integration for performance
3. **File Processing**: Enhanced format support
4. **Analytics**: Usage metrics and reporting

### Phase 4: Production Readiness
1. **Security Hardening**: Authentication, authorization, rate limiting
2. **Performance Optimization**: Query optimization, caching strategies
3. **Monitoring**: APM integration, alerting
4. **Documentation**: API documentation, deployment guides

## Common Patterns & Templates

### Entity Creation
```csharp
public static DataSet Create(string name, string description, string userId, 
    FileMetadata fileInfo, DatasetStatistics statistics)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be null or empty", nameof(name));
    
    // Validation logic
    
    return new DataSet
    {
        Id = Guid.NewGuid(),
        Name = name,
        // Property assignments
        CreatedAt = DateTime.UtcNow,
        CreatedBy = userId
    };
}
```

### Value Object Pattern
```csharp
public class FileMetadata : ValueObject
{
    public string OriginalFileName { get; }
    public string StoragePath { get; }
    public FileType FileType { get; }
    
    private FileMetadata(string originalFileName, string storagePath, FileType fileType)
    {
        // Validation and assignment
    }
    
    public static FileMetadata Create(string originalFileName, string storagePath, 
        FileType fileType, long sizeInBytes, string checksum)
    {
        // Factory method with validation
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return OriginalFileName;
        yield return StoragePath;
        yield return FileType;
        yield return SizeInBytes;
        yield return Checksum;
    }
}
```

### Repository Implementation
```csharp
public class DataSetRepository : IDataSetRepository
{
    private readonly DataNormalizationDbContext _context;
    private readonly ILogger<DataSetRepository> _logger;

    public DataSetRepository(DataNormalizationDbContext context, ILogger<DataSetRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DataSet?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Retrieving dataset: {DataSetId}", id);
        
        return await _context.DataSets
            .AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.Id == id);
    }
}
```

## Troubleshooting Guide

### Common Issues

#### EF Core Value Object Conversion
**Problem**: Value objects not persisting correctly
**Solution**: Ensure HasConversion is properly configured in entity configuration

#### Test Failures
**Problem**: Tests failing after code changes
**Solution**: Check for breaking changes in interfaces, update mocks accordingly

#### Build Errors
**Problem**: Legacy project references causing build failures
**Solution**: Remove legacy references, migrate needed functionality

#### Dependency Injection
**Problem**: Services not resolving at runtime
**Solution**: Verify proper registration in `InfrastructureServiceCollectionExtensions`

This document should be the single source of truth for maintaining quality and consistency throughout the migration process.