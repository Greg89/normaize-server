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
- 📋 **IStatisticalCalculationService / StatisticalCalculationService** - Statistical calculations for visualizations

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
- 📋 **IFileUploadService / FileUploadService** - Main file upload service
- 📋 **IFileUploadServices / FileUploadServices** - Extended upload services
- 📋 **IFileProcessingService / FileProcessingService** - File processing pipeline
- 📋 **IFileValidationService / FileValidationService** - File validation logic
- 📋 **IFileUtilityService / FileUtilityService** - File utility operations

### File Storage & Configuration
- 📋 **IFileStorageService / FileStorageService** - File storage abstraction
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

- **Completed**: 8 services/interfaces
- **In Progress**: 0 services
- **Remaining**: ~45 services/interfaces
- **Overall Progress**: ~15% complete

---

## Next Steps

1. **Immediate**: Start with `IDataAnalysisService` migration
2. **Week 1**: Complete data analysis and statistics services
3. **Week 2**: File management services migration
4. **Week 3**: Visualization services migration
5. **Week 4**: Configuration and infrastructure services
