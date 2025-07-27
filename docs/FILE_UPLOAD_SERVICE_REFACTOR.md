# FileUploadService Refactoring Breakdown

## Overview
The `FileUploadService.cs` has grown to 1024 lines and handles multiple responsibilities. This document outlines the refactoring plan to break it down into smaller, focused services following the same pattern used for `DataVisualizationService`.

## Current Issues
- **Size**: 1024 lines in a single file
- **Multiple Responsibilities**: File validation, processing, storage, configuration, and utilities
- **Maintainability**: Difficult to test and modify specific functionality
- **Reusability**: Logic is tightly coupled within a single service

## Proposed Sub-Services Breakdown

### 1. IFileValidationService
**Responsibilities:**
- File size validation
- File extension validation (allowed/blocked)
- File upload request validation
- File existence validation
- Input parameter validation

**Key Methods:**
- `ValidateFileAsync(FileUploadRequest fileRequest)`
- `IsFileSizeValid(long fileSize, IOperationContext context)`
- `IsFileExtensionValid(string fileExtension, IOperationContext context)`
- `ValidateFileUploadRequest(FileUploadRequest fileRequest)`
- `ValidateFileProcessingInputs(string filePath, string fileType)`
- `ValidateFilePath(string filePath)`

**Dependencies:**
- `FileUploadConfiguration`
- `IDataProcessingInfrastructure` (for logging)

### 2. IFileProcessingService
**Responsibilities:**
- File type-specific processing logic
- Data extraction from different file formats
- Schema generation
- Preview data generation

**Key Methods:**
- `ProcessFileAsync(string filePath, string fileType)`
- `ProcessCsvFileAsync(string filePath, DataSet dataSet, IOperationContext context)`
- `ProcessJsonFileAsync(string filePath, DataSet dataSet, IOperationContext context)`
- `ProcessExcelFileAsync(string filePath, DataSet dataSet, IOperationContext context)`
- `ProcessXmlFileAsync(string filePath, DataSet dataSet, IOperationContext context)`
- `ProcessTextFileAsync(string filePath, DataSet dataSet, IOperationContext context)`

**Dependencies:**
- `IStorageService`
- `DataProcessingConfiguration`
- `IDataProcessingInfrastructure` (for logging)

### 3. IFileStorageService (extends existing IStorageService)
**Responsibilities:**
- File saving operations
- File deletion operations
- File retrieval operations
- Storage provider detection

**Key Methods:**
- `SaveFileAsync(FileUploadRequest fileRequest)`
- `DeleteFileAsync(string filePath)`
- `GetStorageProviderFromPath(string filePath)`

**Dependencies:**
- `IStorageService` (existing)
- `IDataProcessingInfrastructure` (for chaos engineering and logging)

### 4. IFileConfigurationService
**Responsibilities:**
- Configuration validation
- Configuration logging
- Cross-validation logic

**Key Methods:**
- `ValidateConfiguration()`
- `LogConfiguration()`
- Configuration-related validation methods

**Dependencies:**
- `FileUploadConfiguration`
- `DataProcessingConfiguration`
- `IDataProcessingInfrastructure` (for logging)

### 5. IFileUtilityService
**Responsibilities:**
- File type detection
- Data hash generation
- File extension utilities
- Storage strategy determination

**Key Methods:**
- `GenerateDataHashAsync(string filePath)`
- `GetFileTypeFromExtension(string fileType)`
- `ShouldUseSeparateTable(DataSet dataSet)`
- `GetFileExtension(string fileName)`

**Dependencies:**
- `IStorageService`
- `DataProcessingConfiguration`
- `FileUploadConfiguration`
- `IDataProcessingInfrastructure` (for logging)

### 6. IFileUploadServices (Main Composite Interface)
**Responsibilities:**
- Orchestrates all file upload operations
- Provides unified interface for all file-related services
- Maintains the same public API as the original service

**Key Properties:**
- `IFileValidationService Validation { get; }`
- `IFileProcessingService Processing { get; }`
- `IFileStorageService Storage { get; }`
- `IFileConfigurationService Configuration { get; }`
- `IFileUtilityService Utilities { get; }`

## Implementation Strategy

### Phase 1: Create Interfaces
1. Create `IFileValidationService` interface
2. Create `IFileProcessingService` interface
3. Create `IFileConfigurationService` interface
4. Create `IFileUtilityService` interface
5. Create `IFileUploadServices` composite interface

### Phase 2: Extract Implementation Classes
1. Extract `FileValidationService` from `FileUploadService`
2. Extract `FileProcessingService` from `FileUploadService`
3. Extract `FileConfigurationService` from `FileUploadService`
4. Extract `FileUtilityService` from `FileUploadService`
5. Create `FileUploadServices` composite class

### Phase 3: Update Main Service
1. Update `FileUploadService` to use `IFileUploadServices`
2. Maintain the same public API
3. Delegate operations to appropriate sub-services

### Phase 4: Update Dependencies
1. Update dependency injection configuration
2. Update service registration in `ServiceConfiguration.cs`

### Phase 5: Update Tests
1. Create unit tests for each sub-service
2. Update existing `FileUploadService` tests
3. Ensure all functionality is properly tested

## Benefits

1. **Single Responsibility Principle**: Each service has a clear, focused responsibility
2. **Testability**: Smaller services are easier to unit test
3. **Maintainability**: Changes to specific functionality are isolated
4. **Reusability**: Services can be used independently in other contexts
5. **Consistency**: Follows the same pattern as the DataVisualizationService refactor

## Migration Notes

- The public API of `FileUploadService` will remain unchanged
- All existing functionality will be preserved
- The refactoring is internal and transparent to consumers
- Existing tests should continue to pass after the refactoring

## File Structure After Refactoring

```
Normaize.Core/
├── Interfaces/
│   ├── IFileValidationService.cs
│   ├── IFileProcessingService.cs
│   ├── IFileConfigurationService.cs
│   ├── IFileUtilityService.cs
│   └── IFileUploadServices.cs
├── Services/
│   ├── FileUpload/
│   │   ├── FileValidationService.cs
│   │   ├── FileProcessingService.cs
│   │   ├── FileConfigurationService.cs
│   │   ├── FileUtilityService.cs
│   │   └── FileUploadServices.cs
│   └── FileUploadService.cs (updated)
```

## Progress Tracking

- [x] Phase 1: Create Interfaces
  - [x] IFileValidationService
  - [x] IFileProcessingService
  - [x] IFileConfigurationService
  - [x] IFileUtilityService
  - [x] IFileStorageService
  - [x] IFileUploadServices
- [x] Phase 2: Extract Implementation Classes
  - [x] FileValidationService
  - [x] FileProcessingService
  - [x] FileConfigurationService
  - [x] FileUtilityService
  - [x] FileStorageService
  - [x] FileUploadServices
- [x] Phase 3: Update Main Service
  - [x] Refactored FileUploadService to use IFileUploadServices
  - [x] Maintained same public API
  - [x] Delegated operations to appropriate sub-services
- [x] Phase 4: Update Dependencies
  - [x] Updated dependency injection configuration
  - [x] Registered all sub-services and composite service
  - [x] Solution builds successfully
- [x] Phase 5: Update Tests
  - [x] Update existing FileUploadService tests to work with new architecture
  - [x] Create unit tests for each sub-service
  - [x] Ensure all functionality is properly tested 