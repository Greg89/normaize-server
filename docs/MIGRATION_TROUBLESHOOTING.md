# Migration Troubleshooting Guide

This guide helps resolve common migration issues, particularly the "Unknown column" errors that can occur after deployment.

## Common Issues

### 1. "Unknown column 'd.DataHash' in 'field list'"

**Symptoms:**
- Application starts but fails when trying to query the database
- Error messages like "Unknown column 'd.DataHash' in 'field list'"
- Missing columns that should exist based on the current model

**Root Causes:**
- Migration didn't run during deployment
- Database was created before the column was added to the model
- Manual database changes that removed columns
- Migration rollback that didn't complete properly

**Immediate Fix:**
```bash
# Run the database schema fix script
./scripts/fix-database-schema.ps1
```

**Manual Fix:**
```sql
-- Add missing DataHash column
ALTER TABLE DataSets ADD COLUMN DataHash VARCHAR(255);

-- Add other missing columns if needed
ALTER TABLE DataSets ADD COLUMN UserId LONGTEXT NOT NULL DEFAULT '';
ALTER TABLE DataSets ADD COLUMN FilePath VARCHAR(500) NOT NULL DEFAULT '';
ALTER TABLE DataSets ADD COLUMN StorageProvider VARCHAR(50) NOT NULL DEFAULT 'Local';
ALTER TABLE DataSets ADD COLUMN ProcessedData JSON;
ALTER TABLE DataSets ADD COLUMN ProcessingErrors TEXT;
ALTER TABLE DataSets ADD COLUMN UseSeparateTable BOOLEAN NOT NULL DEFAULT FALSE;
```

### 2. Migration Fails During Startup

**Symptoms:**
- Application fails to start
- Migration errors in logs
- Database connection issues

**Solutions:**

**Check Migration Status:**
```bash
# List all migrations
dotnet ef migrations list --startup-project ../Normaize.API

# Check pending migrations
dotnet ef migrations list --startup-project ../Normaize.API --verbose
```

**Force Migration:**
```bash
# Apply migrations manually
dotnet ef database update --startup-project ../Normaize.API

# If that fails, try with connection string
dotnet ef database update --startup-project ../Normaize.API --connection "Server=your_host;Database=your_db;User=your_user;Password=your_password;"
```

### 3. Database Connection Issues

**Symptoms:**
- "Cannot connect to database" errors
- Connection timeout
- Authentication failures

**Solutions:**

**Verify Connection String:**
```bash
# Check environment variables
echo $MYSQLHOST
echo $MYSQLDATABASE
echo $MYSQLUSER
echo $MYSQLPASSWORD
echo $MYSQLPORT
```

**Test Connection:**
```bash
# Test MySQL connection
mysql -h $MYSQLHOST -P $MYSQLPORT -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE -e "SELECT 1;"
```

## Prevention Strategies

### 1. Enhanced Migration Logging

The application now includes enhanced migration logging that will:
- Check database connectivity before migrations
- List pending migrations
- Verify critical columns exist after migration
- Provide detailed error messages for schema mismatches

### 2. Database Schema Verification

The `VerifyDatabaseSchemaAsync` method checks for critical columns:
- `DataHash`
- `UserId`
- `FilePath`
- `StorageProvider`

### 3. Automated Fix Scripts

**PowerShell Script:**
```bash
# Run automated fix
./scripts/fix-database-schema.ps1
```

**SQL Script:**
```bash
# Run SQL fix directly
mysql -h $MYSQLHOST -P $MYSQLPORT -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE < scripts/fix-missing-columns.sql
```

## Deployment Best Practices

### 1. Pre-Deployment Checklist

- [ ] Backup production database
- [ ] Test migrations locally
- [ ] Verify all environment variables are set
- [ ] Check database connectivity

### 2. Deployment Process

```bash
# 1. Deploy application
# 2. Monitor startup logs for migration status
# 3. If migration fails, run fix script
./scripts/fix-database-schema.ps1
# 4. Verify application functionality
```

### 3. Post-Deployment Verification

**Check Migration Status:**
```sql
-- Verify migrations table
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;

-- Check critical columns exist
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DataSets' 
AND COLUMN_NAME IN ('DataHash', 'UserId', 'FilePath', 'StorageProvider');
```

**Test Application:**
```bash
# Test health endpoint
curl http://your-app-url/health/startup

# Test data retrieval
curl http://your-app-url/api/datasets
```

## Monitoring and Alerting

### 1. Log Monitoring

Watch for these log messages:
- `"Applying database migrations..."`
- `"Database migrations applied successfully"`
- `"Critical column 'DataHash' is missing from DataSets table"`
- `"Database schema mismatch detected"`

### 2. Health Checks

The application includes health checks that can detect database issues:
```bash
curl http://your-app-url/health/startup
```

### 3. Error Patterns

Common error patterns to monitor:
- `MySqlConnector.MySqlException (0x80004005): Unknown column`
- `Cannot connect to database`
- `Migration failed`

## Emergency Procedures

### 1. Immediate Rollback

If migrations cause critical issues:
```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName --startup-project ../Normaize.API
```

### 2. Manual Schema Fix

If automated fixes don't work:
```sql
-- Connect to database and run manual fixes
USE your_database;

-- Add missing columns one by one
ALTER TABLE DataSets ADD COLUMN DataHash VARCHAR(255);
-- ... add other missing columns

-- Verify fixes
DESCRIBE DataSets;
```

### 3. Database Restore

As a last resort:
```bash
# Restore from backup
mysql -h $MYSQLHOST -P $MYSQLPORT -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE < backup.sql
```

## Troubleshooting Commands

### Database Inspection

```sql
-- Check table structure
DESCRIBE DataSets;

-- Check migration history
SELECT * FROM __EFMigrationsHistory;

-- Check for missing columns
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DataSets' 
AND COLUMN_NAME NOT IN (
    SELECT COLUMN_NAME 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DataSets' 
    AND TABLE_SCHEMA = DATABASE()
);
```

### Application Logs

```bash
# Check application logs
docker logs your-container-name

# Check specific migration logs
grep -i "migration" /var/log/your-app.log
```

### Environment Verification

```bash
# Check environment variables
env | grep MYSQL

# Test database connection
mysql -h $MYSQLHOST -P $MYSQLPORT -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE -e "SELECT VERSION();"
```

## Support

If you continue to experience issues:

1. Check the application logs for detailed error messages
2. Run the automated fix script
3. Verify database connectivity and permissions
4. Check if all environment variables are correctly set
5. Consider rolling back to a known good state

For persistent issues, collect:
- Application logs
- Database migration history
- Environment variable configuration
- Error messages and stack traces 