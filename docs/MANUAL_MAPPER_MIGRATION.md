# Manual Mapper Migration

## Overview

This document details the migration from AutoMapper to a custom manual mapping solution in the Normaize project. This change was implemented to resolve package version conflicts and provide better control over object mapping operations.

## Background

### Why We Migrated Away from AutoMapper

1. **Package Version Conflicts**: AutoMapper and its extension packages had version conflicts that prevented keeping all packages up to date
2. **Dependency Management**: Reducing external dependencies for better maintainability
3. **Type Safety**: Manual mapping provides compile-time type checking
4. **Performance**: Eliminates runtime reflection overhead
5. **Control**: Full control over mapping logic and error handling

### Previous AutoMapper Implementation

The project previously used:
- `AutoMapper` (12.0.1)
- `AutoMapper.Extensions.Microsoft.DependencyInjection` (12.0.1)
- `MappingProfile.cs` for configuration

## New Manual Mapper Implementation

### Core Components

#### 1. ManualMapper Class
**Location**: `Normaize.Core/Mapping/ManualMapper.cs`

The new manual mapper is implemented as a static class with extension methods:

```csharp
public static class ManualMapper
{
    // Entity to DTO mappings
    public static DataSetDto ToDto(this DataSet dataSet)
    public static AnalysisDto ToDto(this Analysis analysis)
    public static UserSettingsDto ToDto(this UserSettings userSettings)
    
    // DTO to Entity mappings
    public static DataSet ToEntity(this CreateDataSetDto dto)
    public static Analysis ToEntity(this CreateAnalysisDto dto)
    public static UserSettings ToEntity(this UpdateUserSettingsDto dto)
    
    // Collection mappings
    public static IEnumerable<TDto> ToDtoCollection<TEntity, TDto>(
        this IEnumerable<TEntity> entities, 
        Func<TEntity, TDto> mapper)
    
    // Null-safe mapping
    public static TDto MapSafely<TEntity, TDto>(
        this TEntity entity, 
        Func<TEntity, TDto> mapper) where TEntity : class
}
```

### Key Features

1. **Extension Methods**: All mapping methods are implemented as extension methods for clean, fluent syntax
2. **Null Safety**: Built-in null checking to prevent null reference exceptions
3. **Type Safety**: Compile-time type checking for all mappings
4. **Collection Support**: Generic collection mapping with type-safe delegates
5. **Explicit Mapping**: All property mappings are explicitly defined for clarity

### Usage Examples

#### Basic Entity to DTO Mapping
```csharp
// Before (AutoMapper)
var dto = _mapper.Map<DataSetDto>(dataSet);

// After (Manual Mapper)
var dto = dataSet.ToDto();
```

#### Collection Mapping
```csharp
// Before (AutoMapper)
var dtos = _mapper.Map<IEnumerable<DataSetDto>>(dataSets);

// After (Manual Mapper)
var dtos = dataSets.ToDtoCollection(ds => ds.ToDto());
```

#### DTO to Entity Mapping
```csharp
// Before (AutoMapper)
var entity = _mapper.Map<DataSet>(createDto);

// After (Manual Mapper)
var entity = createDto.ToEntity();
```

## Migration Steps

### 1. Remove AutoMapper Packages
```bash
dotnet remove Normaize.Core/Normaize.Core.csproj package AutoMapper
dotnet remove Normaize.API/Normaize.API.csproj package AutoMapper
dotnet remove Normaize.API/Normaize.API.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection
```

### 2. Delete AutoMapper Configuration
- Removed `MappingProfile.cs`
- Removed AutoMapper configuration from `ServiceConfiguration.cs`
- Removed `services.AddAutoMapper()` calls

### 3. Update Service Dependencies
**Before**:
```csharp
public class DataProcessingService
{
    private readonly IMapper _mapper;
    
    public DataProcessingService(IMapper mapper)
    {
        _mapper = mapper;
    }
}
```

**After**:
```csharp
public class DataProcessingService
{
    // No mapper dependency needed
    public DataProcessingService()
    {
    }
}
```

### 4. Update Service Methods
**Before**:
```csharp
public async Task<DataSetDto> GetDataSetAsync(int id)
{
    var dataSet = await _repository.GetByIdAsync(id);
    return _mapper.Map<DataSetDto>(dataSet);
}
```

**After**:
```csharp
public async Task<DataSetDto> GetDataSetAsync(int id)
{
    var dataSet = await _repository.GetByIdAsync(id);
    return dataSet.ToDto();
}
```

### 5. Update Unit Tests
**Before**:
```csharp
private readonly Mock<IMapper> _mockMapper;

[SetUp]
public void Setup()
{
    _mockMapper = new Mock<IMapper>();
    _service = new DataProcessingService(_mockMapper.Object);
}
```

**After**:
```csharp
[SetUp]
public void Setup()
{
    _service = new DataProcessingService();
}
```

## Benefits Achieved

### 1. Package Management
- ✅ All packages can now be updated to latest versions
- ✅ No more version conflicts
- ✅ Reduced dependency footprint

### 2. Performance
- ✅ No runtime reflection overhead
- ✅ Compile-time type checking
- ✅ Direct property assignment

### 3. Maintainability
- ✅ Explicit mapping logic
- ✅ Easy to debug and trace
- ✅ No hidden mapping behavior

### 4. Type Safety
- ✅ Compile-time validation
- ✅ IntelliSense support
- ✅ Refactoring safety

## Current Mapping Coverage

The manual mapper currently supports mappings for:

### DataSet Mappings
- `DataSet` ↔ `DataSetDto`
- `DataSet` ↔ `CreateDataSetDto`

### Analysis Mappings
- `Analysis` ↔ `AnalysisDto`
- `Analysis` ↔ `CreateAnalysisDto`

### UserSettings Mappings
- `UserSettings` ↔ `UserSettingsDto`
- `UserSettings` ↔ `UpdateUserSettingsDto`

### Profile Mappings
- `UserProfile` ↔ `ProfileInfoDto`

## Adding New Mappings

To add a new mapping, follow this pattern:

```csharp
// Entity to DTO
public static NewDto ToDto(this NewEntity entity)
{
    if (entity == null) return null!;
    
    return new NewDto
    {
        Id = entity.Id,
        Name = entity.Name,
        // ... map all properties explicitly
    };
}

// DTO to Entity
public static NewEntity ToEntity(this CreateNewDto dto)
{
    if (dto == null) return null!;
    
    return new NewEntity
    {
        Name = dto.Name,
        // ... map all properties explicitly
    };
}
```

## Testing

All mappings are thoroughly tested through:
- Unit tests for individual mapping methods
- Integration tests for service layer operations
- Build verification to ensure all mappings compile correctly

## Migration Verification

The migration was verified by:
1. ✅ Building the entire solution successfully
2. ✅ Running all unit tests (all tests pass)
3. ✅ Running integration tests
4. ✅ Verifying no AutoMapper references remain
5. ✅ Confirming all services work with new mapping approach

## Future Considerations

### Potential Enhancements
1. **Validation Integration**: Add validation attributes to mapping methods
2. **Conditional Mapping**: Support for conditional property mapping
3. **Deep Copy Support**: Add deep copy functionality for complex objects
4. **Mapping Caching**: Consider caching for frequently used mappings

### Monitoring
- Monitor performance impact of manual mapping
- Track any mapping-related bugs or issues
- Consider automated mapping generation for large DTOs

## Conclusion

The migration from AutoMapper to manual mapping has been successfully completed. This change provides:

- Better package management
- Improved performance
- Enhanced type safety
- Greater control over mapping logic
- Reduced external dependencies

The manual mapper is now the standard approach for all object mapping operations in the Normaize project. 