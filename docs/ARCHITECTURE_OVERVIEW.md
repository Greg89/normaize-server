# Architecture Overview

This document explains the clean architecture structure of the Normaize application.

## Layer Structure

```
Normaize.Core/          ← Domain/Business Logic
Normaize.Data/          ← Infrastructure (Database, External Services)
Normaize.API/           ← Presentation/HTTP Layer
```

## Layer Responsibilities

### Core Layer (`Normaize.Core/`)
**Purpose**: Pure business logic and domain models
- **Interfaces**: Define contracts for all services (`IDatabaseHealthService`, `IMigrationService`, etc.)
- **Models**: Domain entities (`DataSet`, `Analysis`) and DTOs
- **Services**: Business logic implementations (`DataProcessingService`, `FileUploadService`)
- **No Dependencies**: Pure business logic with no external dependencies

### Data Layer (`Normaize.Data/`) 
**Purpose**: Infrastructure concerns and data persistence
- **DbContext**: EF Core configuration and database context
- **Repositories**: Data access implementations (`DataSetRepository`, `AnalysisRepository`)
- **Services**: Infrastructure services (`DatabaseHealthService`, `MigrationService`)
- **Migrations**: Database schema changes
- **External Services**: Storage implementations (`SftpStorageService`, `LocalStorageService`)

### API Layer (`Normaize.API/`)
**Purpose**: HTTP presentation and application setup
- **Controllers**: HTTP request handling (`DataSetsController`, `HealthController`)
- **Middleware**: Cross-cutting concerns (authentication, logging, exception handling)
- **Configuration**: Application setup and dependency injection
- **No Business Logic**: Only orchestration and HTTP concerns

## Dependency Flow

```
API Layer → Core Layer ← Data Layer
     ↓           ↑           ↓
External Services → Core Layer
```

### Key Principles
1. **Dependencies Point Inward**: All dependencies point toward the Core layer
2. **Core Layer is Independent**: Core has no dependencies on other layers
3. **API Depends on Core**: API layer only knows about Core interfaces
4. **Data Implements Core**: Data layer implements Core interfaces

## Why This Structure Works

### 1. **Clean Separation**
- Business logic is isolated in Core
- Infrastructure concerns are isolated in Data
- HTTP concerns are isolated in API

### 2. **Testability**
- Core can be tested without database
- API can be tested with mocked services
- Data can be tested with in-memory database

### 3. **Flexibility**
- Can swap database implementations without changing business logic
- Can add new storage providers without changing API
- Can test with different implementations

### 4. **Maintainability**
- Changes to infrastructure don't affect business logic
- Changes to API don't affect data layer
- Clear boundaries between responsibilities

## Service Registration

Services are registered in `Program.cs` following the dependency flow:

```csharp
// Core services (business logic)
builder.Services.AddScoped<IDataProcessingService, DataProcessingService>();

// Infrastructure services (implement Core interfaces)
builder.Services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
builder.Services.AddScoped<IMigrationService, MigrationService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();

// Data access
builder.Services.AddScoped<IDataSetRepository, DataSetRepository>();
```

## Migration and Health Checks

The refactored migration and health check system follows clean architecture:

1. **Interfaces in Core**: `IDatabaseHealthService`, `IMigrationService`
2. **Implementation in Data**: `DatabaseHealthService`, `MigrationService`
3. **Usage in API**: Controllers use interfaces, not implementations

This ensures:
- API layer doesn't know about database details
- Services can be easily mocked for testing
- Implementation can be swapped without changing API

## Benefits of This Architecture

1. **Standards Compliance**: Follows clean architecture principles
2. **Team Collaboration**: Clear boundaries for different team members
3. **Scalability**: Easy to add new features without breaking existing code
4. **Testing**: Comprehensive test coverage at all layers
5. **Maintenance**: Changes are isolated to specific layers 