# Core Project Migration Checklist

This document tracks the migration of all services, interfaces, and components from the legacy `Normaize.Core` project to the new DDD architecture. The goal is to ensure no functionality is lost during the migration.

## Migration Status Legend
- ✅ **COMPLETED** - Fully migrated and tested
- 🔄 **IN PROGRESS** - Currently being worked on
- ⏳ **PLANNED** - Next in queue
- 📋 **TODO** - Not yet started
- ❌ **DEPRECATED** - Functionality replaced or no longer needed

---

## 1. Data Processing & Normalization Services

### Core Data Processing
- ✅ **IDataNormalizationService / DataNormalizationService** - Migrated to DDD with CQRS pattern
- ✅ **IDuplicateRowRemovalProcessor / DuplicateRowRemovalProcessor** - Migrated to `DuplicateRemovalProcessor`
- ✅ **IJobQueueService** - Migrated to `IJobQueue` interface with `JobQueueService`
- 📋 **IDataProcessingService / DataProcessingService** - Legacy data processing logic
- 📋 **IDataProcessingInfrastructure** - Infrastructure abstraction

### Data Analysis & Statistics
- ✅ **IDataAnalysisService / DataAnalysisService** - Fully migrated to DDD with CQRS pattern
- ✅ **IStatisticalCalculationService / StatisticalCalculationService** - Fully migrated with comprehensive test coverage

---

## 2. Data Management Services

### Dataset Lifecycle
- ✅ **IDataSetRepository / DataSetRepository** - Core repository migrated to Domain layer
- ✅ **IDataSetRowRepository / DataSetRowRepository** - Row-level operations migrated
- 📋 **IDataSetLifecycleService / DataSetLifecycleService** - Complete dataset lifecycle management
- 📋 **IDataSetPreviewService / DataSetPreviewService** - Dataset preview and sampling
- 📋 **IDataSetQueryService / DataSetQueryService** - Advanced querying capabilities

### Data Migration & Import
- 📋 **IDataMigrationService / DataMigrationService** - Data migration utilities
- 📋 **IMigrationService** - Generic migration interface

---

## 3. File Management Services

### File Upload & Processing
- ✅ **IFileStorageService / FileStorageService** - Migrated with S3 integration and comprehensive tests (8 tests)
- 📋 **IFileUploadService / FileUploadService** - Main file upload service (depends on FileStorageService)
- 📋 **IFileUploadServices / FileUploadServices** - Extended upload services
- ✅ **IFileProcessingService / FileProcessingService** - File processing pipeline complete with 24 comprehensive tests (CSV, JSON, XML, Excel, TXT)
- 📋 **IFileValidationService / FileValidationService** - File validation logic
- 📋 **IFileUtilityService / FileUtilityService** - File utility operations

### File Storage & Configuration
- ✅ **FileMetadata, FileType, StorageProvider** - Value objects created in Domain layer
- 📋 **IStorageService** - Generic storage interface
- 📋 **IFileConfigurationService / FileConfigurationService** - File configuration management
- 📋 **IStorageConfigurationService** - Storage configuration

---

## 4. Visualization & Charting Services

### Chart Generation
- 📋 **IDataVisualizationService / DataVisualizationService** - Main visualization service
- 📋 **IChartGenerationService / ChartGenerationService** - Chart generation logic
- 📋 **IVisualizationServices / VisualizationServices** - Extended visualization services
- 📋 **IVisualizationValidationService / VisualizationValidationService** - Validation for visualizations

### Caching & Performance
- 📋 **ICacheManagementService / CacheManagementService** - Visualization caching

---

## 5. Configuration & Settings Services

### Application Configuration
- 📋 **IAppConfigurationService** - Application configuration management
- 📋 **IConfigurationValidationService** - Configuration validation
- 📋 **IUserSettingsService** - User preferences and settings

### Startup & Initialization
- 📋 **IStartupService** - Application startup procedures

---

## 6. Infrastructure & Monitoring Services

### Health & Monitoring
- 📋 **IHealthCheckService** - Health check implementations
- 📋 **IDatabaseHealthService** - Database health monitoring
- 📋 **IChaosEngineeringService** - Chaos engineering capabilities

### Logging & Auditing
- 📋 **IAuditService** - Audit trail functionality
- 📋 **IStructuredLoggingService** - Advanced logging capabilities

---

## 7. Models & DTOs Migration

### Domain Models
- ✅ **DataNormalizationJob** - Migrated to `NormalizationJob` aggregate
- ✅ **DataSet** - Migrated to Domain entity
- 📋 **Analysis** - Analysis result model
- 📋 **UserSettings** - User settings model
- 📋 **FileUploadRequest** - File upload request model

### Data Transfer Objects
- 📋 **AnalysisDto** - Analysis data transfer
- 📋 **DataNormalizationDto** - Normalization DTOs
- 📋 **DataSetDto** - Dataset transfer objects
- 📋 **DataSetStatisticsDto** - Statistics DTOs
- 📋 **VisualizationDto** - Visualization DTOs
- 📋 **UserSettingsDto** - User settings transfer
- 📋 **StorageDiagnosticsDto** - Storage diagnostics
- 📋 **HealthResponseDto** - Health check responses

### API Response Models
- 📋 **ApiResponse** - Standardized API responses
- 📋 **AuthDto** - Authentication DTOs
- 📋 **UserInfoDto / UserProfileDto** - User profile information

---

## 8. Extensions & Utilities

### Extension Methods
- 📋 **Extensions/** - Various extension methods and utilities

### Constants & Configuration
- 📋 **Constants/** - Application constants
- 📋 **Configuration/** - Configuration classes

### Mapping
- 📋 **Mapping/** - Object mapping logic

---

## Migration Strategy

### Phase 1: Core Data Services (CURRENT)
1. ✅ Data Normalization & Job Processing
2. ⏳ **DataAnalysisService** - Statistical analysis capabilities
3. ⏳ **DataSetLifecycleService** - Complete dataset management

### Phase 2: File Management
1. **FileUploadService** - Core file upload functionality
2. **FileProcessingService** - File processing pipeline
3. **FileStorageService** - Storage abstraction

### Phase 3: Visualization & Analytics
1. **DataVisualizationService** - Chart and graph generation
2. **ChartGenerationService** - Specific chart types
3. **StatisticalCalculationService** - Advanced statistics

### Phase 4: Configuration & Infrastructure
1. **AppConfigurationService** - Application configuration
2. **HealthCheckService** - Health monitoring
3. **AuditService** - Audit trail functionality

### Phase 5: User Management & Settings
1. **UserSettingsService** - User preferences
2. **AuthDto & UserProfileDto** - User management
3. **Configuration validation** - Settings validation

---

## Migration Principles

1. **One Service at a Time** - Complete each service with full test coverage before moving to next
2. **Maintain API Compatibility** - Ensure existing API contracts are preserved
3. **Test Coverage Required** - Every migrated service must have comprehensive tests
4. **DDD Compliance** - All services must follow DDD principles and patterns
5. **No Functionality Loss** - Every feature must be preserved or improved

---

## Testing Requirements

For each migrated service:
- [ ] Unit tests for all public methods
- [ ] Integration tests for database interactions
- [ ] API integration tests for controller endpoints
- [ ] Performance tests for critical paths
- [ ] Error handling and edge case tests

---

## Current Progress

- **Completed**: 12 services/interfaces (added FileProcessingService)
- **In Progress**: 0 services
- **Remaining**: ~41 services/interfaces
- **Overall Progress**: ~23% complete

**Recent Achievements**:
- ✅ FileProcessingService: 24 comprehensive tests covering CSV, JSON, XML, Excel, and TXT file processing
- ✅ Security tests: Path traversal detection, file type validation, size limit enforcement
- ✅ Error handling: Missing files, invalid formats, empty files, malformed data
- ✅ Test suite: 205/205 tests passing (100% pass rate maintained)

### Recent Achievements (October 26, 2025)
- ✅ Created and configured Application test project
- ✅ Fixed all Application layer tests (4/4 passing)
- ✅ Completed FileStorageService migration with 8 comprehensive tests
- ✅ Created FileMetadata, FileType, and StorageProvider value objects
- ✅ Completed FileProcessingService migration with 24 comprehensive tests
- ✅ Achieved 100% test pass rate: **205/205 tests passing**
  - Domain: 151 tests
  - Application: 4 tests  
  - Infrastructure: 50 tests (DataSetRepository: 18, FileStorageService: 8, FileProcessingService: 24)
  - API: 0 tests (pending API layer migration)

---

## Next Steps

### **Immediate Priority: Complete File Management Services**
Following our proven 8-step workflow (documented in DDD_MIGRATION_PLAN.md):

1. ✅ **FileProcessingService** (COMPLETE - 24 comprehensive tests)
   - ✅ Infrastructure implementation with CSV/JSON/XML/Excel/TXT support
   - ✅ Comprehensive test coverage for all file formats
   - ✅ Validation tests (empty files, size limits, path traversal)
   - ✅ Error handling tests (missing files, invalid formats)
   - Result: 24 new tests, 100% pass rate maintained (205/205)

2. **FileValidationService** (Next - Estimated 8-10 hours)
   - Domain layer: Create validation rules as value objects
   - Infrastructure: Implement validation logic
   - Testing: Edge cases, malformed files, size limits
   - Expected outcome: 10-15 new tests

3. **FileUploadService** (Estimated 12-15 hours)
   - Application layer: Create upload commands/queries
   - Infrastructure: Orchestrate FileStorageService + FileProcessingService
   - Testing: End-to-end upload workflows
   - Expected outcome: 15-20 new tests

### **Week 1-2**: Complete File Management Suite
- [ ] FileProcessingService migration
- [ ] FileValidationService migration  
- [ ] FileUploadService migration
- [ ] Integration tests for complete upload pipeline

### **Week 3**: Dataset Lifecycle Services
- [ ] DataSetLifecycleService
- [ ] DataSetPreviewService
- [ ] DataSetQueryService

### **Week 4**: Visualization Services
- [ ] DataVisualizationService
- [ ] ChartGenerationService
- [ ] Caching layer
