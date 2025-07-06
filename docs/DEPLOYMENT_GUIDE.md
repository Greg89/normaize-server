# Database Deployment Guide for Normaize

## Overview

This guide explains how to properly deploy the enhanced file upload and storage changes to your beta site using Entity Framework Core migrations and supplementary MySQL optimizations.

## Deployment Process

### Step 1: EF Core Migration (Required)

The EF Core migration handles the core database schema changes:

```bash
# This creates the migration file
dotnet ef migrations add EnhancedFileUploadAndStorage --startup-project ../Normaize.API
```

**What the migration includes:**
- ✅ New `DataSetRows` table for large datasets
- ✅ Enhanced `DataSets` table with new columns
- ✅ JSON data types for better performance
- ✅ Proper indexes for fast queries
- ✅ Foreign key constraints for data integrity

### Step 2: Deploy Migration to Beta Site

#### Option A: Automatic Migration (Recommended for Beta)
```csharp
// In Program.cs - this automatically applies migrations on startup
app.MigrateDatabase();
```

#### Option B: Manual Migration
```bash
# Apply migration manually
dotnet ef database update --startup-project ../Normaize.API
```

### Step 3: Supplementary MySQL Optimizations (Optional but Recommended)

The `MySQL_Optimizations.sql` script provides additional performance optimizations:

```sql
-- Run this after the EF Core migration
-- This script adds:
-- 1. Additional performance indexes
-- 2. Stored procedures for common operations
-- 3. Views for data analysis
-- 4. Audit and statistics tables
-- 5. Triggers for data integrity
```

## Deployment Pipeline Integration

### GitHub Actions Workflow

Add this to your `.github/workflows/ci.yml`:

```yaml
- name: Apply Database Migrations
  run: |
    cd Normaize.Data
    dotnet ef database update --startup-project ../Normaize.API --connection "${{ secrets.DATABASE_CONNECTION_STRING }}"
  
- name: Apply MySQL Optimizations
  run: |
    mysql -h ${{ secrets.DB_HOST }} -u ${{ secrets.DB_USER }} -p${{ secrets.DB_PASSWORD }} ${{ secrets.DB_NAME }} < Normaize.Data/Migrations/MySQL_Optimizations.sql
```

### Railway Deployment

For Railway, add this to your `railway.toml`:

```toml
[build]
builder = "nixpacks"

[deploy]
startCommand = "cd Normaize.API && dotnet ef database update && dotnet run"
```

## Migration Files Structure

```
Normaize.Data/Migrations/
├── 20250703163140_InitialCreate.cs          # Original migration
├── 20250706183536_EnhancedFileUploadAndStorage.cs  # New migration
├── 20250706183536_EnhancedFileUploadAndStorage.Designer.cs
├── NormaizeContextModelSnapshot.cs
└── MySQL_Optimizations.sql                   # Supplementary optimizations
```

## What Each Migration Does

### EF Core Migration (`EnhancedFileUploadAndStorage.cs`)

**DataSets Table Changes:**
```sql
-- New columns added
ALTER TABLE DataSets ADD COLUMN DataHash VARCHAR(255);
ALTER TABLE DataSets ADD COLUMN FilePath VARCHAR(500) NOT NULL DEFAULT '';
ALTER TABLE DataSets ADD COLUMN ProcessedData JSON;
ALTER TABLE DataSets ADD COLUMN ProcessingErrors TEXT;
ALTER TABLE DataSets ADD COLUMN StorageProvider VARCHAR(50) DEFAULT 'Local';
ALTER TABLE DataSets ADD COLUMN UseSeparateTable BOOLEAN DEFAULT FALSE;

-- Column type changes
ALTER TABLE DataSets MODIFY COLUMN Schema JSON;
ALTER TABLE DataSets MODIFY COLUMN PreviewData JSON;
ALTER TABLE DataSets MODIFY COLUMN FileName VARCHAR(255);
ALTER TABLE DataSets MODIFY COLUMN FileType VARCHAR(50);
```

**New DataSetRows Table:**
```sql
CREATE TABLE DataSetRows (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    DataSetId INT NOT NULL,
    RowIndex INT NOT NULL,
    Data JSON NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (DataSetId) REFERENCES DataSets(Id) ON DELETE CASCADE
);
```

**Indexes Created:**
```sql
CREATE INDEX idx_dataset_schema ON DataSets(Schema);
CREATE INDEX IX_DataSets_IsProcessed ON DataSets(IsProcessed);
CREATE INDEX IX_DataSets_UploadedAt ON DataSets(UploadedAt);
CREATE INDEX IX_DataSets_UseSeparateTable ON DataSets(UseSeparateTable);
CREATE INDEX idx_datasetrow_dataset_row ON DataSetRows(DataSetId, RowIndex);
```

### MySQL Optimizations Script

**Additional Performance Indexes:**
```sql
CREATE INDEX idx_datasets_file_size ON DataSets(FileSize);
CREATE INDEX idx_datasets_row_count ON DataSets(RowCount);
CREATE INDEX idx_datasetrows_created_at ON DataSetRows(CreatedAt);
```

**Stored Procedures:**
```sql
-- Find datasets by column
CREATE PROCEDURE GetDataSetsByColumn(IN column_name VARCHAR(255))
-- Search data values
CREATE PROCEDURE GetDataSetsByValue(IN column_name VARCHAR(255), IN search_value VARCHAR(255))
-- Cleanup old datasets
CREATE PROCEDURE CleanupOldDataSets(IN days_to_keep INT)
```

**Views and Audit Tables:**
```sql
-- Dataset summary view
CREATE VIEW v_dataset_summary AS SELECT ...
-- Audit logging
CREATE TABLE DataSetAuditLog (...)
-- Statistics tracking
CREATE TABLE DataSetStatistics (...)
```

## Deployment Checklist

### Before Deployment
- [ ] Commit the EF Core migration files
- [ ] Test migration locally
- [ ] Backup production database
- [ ] Update connection string with optimized settings

### During Deployment
- [ ] Apply EF Core migration
- [ ] Run MySQL optimizations script
- [ ] Verify all tables and indexes created
- [ ] Test file upload functionality

### After Deployment
- [ ] Monitor database performance
- [ ] Check application logs for errors
- [ ] Verify file upload and processing works
- [ ] Test data retrieval and analysis

## Rollback Plan

### If Migration Fails
```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName --startup-project ../Normaize.API
```

### If Optimizations Cause Issues
```sql
-- Drop problematic indexes/procedures
DROP INDEX IF EXISTS idx_datasets_file_size ON DataSets;
DROP PROCEDURE IF EXISTS GetDataSetsByColumn;
```

## Environment-Specific Considerations

### Development Environment
```bash
# Local development
dotnet ef database update --startup-project ../Normaize.API
mysql -u root -p normaize < Normaize.Data/Migrations/MySQL_Optimizations.sql
```

### Beta/Staging Environment
```bash
# Use environment-specific connection strings
dotnet ef database update --startup-project ../Normaize.API --connection "$BETA_DB_CONNECTION"
mysql -h $BETA_DB_HOST -u $BETA_DB_USER -p$BETA_DB_PASSWORD $BETA_DB_NAME < MySQL_Optimizations.sql
```

### Production Environment
```bash
# Always backup first
mysqldump -u root -p normaize > backup_before_migration.sql

# Apply changes
dotnet ef database update --startup-project ../Normaize.API --connection "$PROD_DB_CONNECTION"
mysql -h $PROD_DB_HOST -u $PROD_DB_USER -p$PROD_DB_PASSWORD $PROD_DB_NAME < MySQL_Optimizations.sql
```

## Monitoring and Verification

### Check Migration Status
```bash
# Verify migration applied
dotnet ef migrations list --startup-project ../Normaize.API
```

### Verify Database Changes
```sql
-- Check new tables exist
SHOW TABLES LIKE 'DataSetRows';

-- Check new columns exist
DESCRIBE DataSets;

-- Check indexes created
SHOW INDEX FROM DataSets;
SHOW INDEX FROM DataSetRows;

-- Check JSON columns
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DataSets' AND DATA_TYPE = 'json';
```

### Performance Verification
```sql
-- Test JSON queries
SELECT * FROM DataSets WHERE JSON_CONTAINS(Schema, '"Sales"');

-- Check query performance
EXPLAIN SELECT * FROM DataSets WHERE UseSeparateTable = 0;
```

## Troubleshooting

### Common Issues

**1. Migration Fails with "Column already exists"**
```bash
# Check current database state
dotnet ef migrations list --startup-project ../Normaize.API
# Remove and recreate migration if needed
dotnet ef migrations remove --startup-project ../Normaize.API
dotnet ef migrations add EnhancedFileUploadAndStorage --startup-project ../Normaize.API
```

**2. MySQL Optimizations Script Fails**
```sql
-- Check MySQL version supports JSON
SELECT VERSION();
-- Should be 5.7+ for JSON support, 8.0+ for JSON indexes
```

**3. Performance Issues After Migration**
```sql
-- Check if indexes are being used
EXPLAIN SELECT * FROM DataSets WHERE UploadedAt > '2024-01-01';
-- Optimize tables if needed
OPTIMIZE TABLE DataSets;
OPTIMIZE TABLE DataSetRows;
```

## Summary

1. **EF Core Migration**: Handles core schema changes (required)
2. **MySQL Optimizations**: Provides additional performance benefits (recommended)
3. **Deployment Pipeline**: Integrate both into your CI/CD process
4. **Testing**: Always test in staging before production
5. **Monitoring**: Verify changes work correctly after deployment

This approach ensures a safe, reliable deployment of your enhanced file upload and storage system! 🚀 