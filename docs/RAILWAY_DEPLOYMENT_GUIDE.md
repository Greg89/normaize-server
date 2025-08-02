# Railway Deployment Guide

This guide explains how to deploy the Normaize application to Railway with comprehensive health checks and fail-fast behavior.

## Overview

The application is configured to fail fast in production environments if the database is not healthy. This ensures that deployments only succeed when the application is fully functional.

## Health Check Configuration

### Docker Health Check
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/health/database || exit 1
```

### Railway Configuration
```json
{
  "deploy": {
    "healthcheckPath": "/api/healthmonitoring/readiness",
    "healthcheckTimeout": 300,
    "restartPolicyType": "on_failure",
    "healthcheckInterval": 30,
    "healthcheckRetries": 3
  }
}
```

## Health Check Endpoints

### Comprehensive Health Check (`/api/healthmonitoring/health`)
Returns detailed health information for all system components:

```json
{
  "status": "healthy",
  "components": {
    "database": "healthy",
    "storage": "healthy",
    "external_services": "healthy"
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "duration": 214.8,
  "message": "All systems healthy",
  "correlationId": "abc123"
}
```

### Readiness Check (`/api/healthmonitoring/readiness`)
Returns readiness status for traffic serving:

```json
{
  "status": "ready",
  "timestamp": "2024-01-15T10:30:00Z",
  "message": "Application ready to serve traffic"
}
```

### Liveness Check (`/api/healthmonitoring/liveness`)
Returns basic liveness status:

```json
{
  "status": "alive",
  "timestamp": "2024-01-15T10:30:00Z",
  "message": "Application is alive and responding"
}
```

### Basic Health Check (`/api/health`)
Returns basic application status:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "service": "Normaize API",
  "version": "1.0.0",
  "environment": "Production"
}
```

## Startup Health Verification

The application performs comprehensive health checks during startup:

### 1. Database Connectivity
- Verifies connection to MySQL database
- Checks connection string validity

### 2. Migration Status
- Applies pending migrations
- Verifies migration history
- Checks for schema mismatches

### 3. Schema Validation
- Verifies critical columns exist (`DataHash`, `UserId`, `FilePath`, `StorageProvider`)
- Checks for required tables (`DataSetRows`)
- Validates column types and constraints

### 4. Environment Variables
- Verifies all required environment variables are set
- Checks database connection parameters

## Fail-Fast Behavior

### Production/Staging Environments
- **Migration Failures**: Application will not start if migrations fail
- **Health Check Failures**: Application will not start if health checks fail
- **Database Issues**: Application will not start if database is unhealthy

### Development Environment
- **Graceful Degradation**: Application continues with warnings
- **Detailed Logging**: All issues are logged for debugging
- **Manual Intervention**: Allows manual fixes during development

## Deployment Process

### 1. Pre-Deployment Checklist
```bash
# Verify environment variables
echo $MYSQLHOST
echo $MYSQLDATABASE
echo $MYSQLUSER
echo $MYSQLPASSWORD
echo $MYSQLPORT

# Test database connection
mysql -h $MYSQLHOST -P $MYSQLPORT -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE -e "SELECT 1;"
```

### 2. Deploy to Railway
```bash
# Deploy using Railway CLI
railway up

# Or deploy via GitHub integration
git push origin main
```

### 3. Monitor Deployment
```bash
# Check deployment logs
railway logs

# Monitor health checks
railway status

# Check health endpoint
curl https://your-app.railway.app/health/database
```

## Troubleshooting Deployment Failures

### Common Issues and Solutions

#### 1. Migration Failures
**Symptoms**: Application fails to start with migration errors
```bash
# Check migration status
railway logs | grep -i migration

# Run manual migration if needed
railway run dotnet ef database update --startup-project ../Normaize.API
```

#### 2. Missing Columns
**Symptoms**: "Unknown column" errors in logs
```bash
# Run automated fix script
railway run ./scripts/fix-database-schema.ps1

# Or run SQL directly
railway run mysql -h $MYSQLHOST -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE < scripts/fix-missing-columns.sql
```

#### 3. Database Connection Issues
**Symptoms**: "Cannot connect to database" errors
```bash
# Verify environment variables
railway variables

# Test connection
railway run mysql -h $MYSQLHOST -P $MYSQLPORT -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE -e "SELECT VERSION();"
```

#### 4. Health Check Timeouts
**Symptoms**: Railway reports health check failures
```bash
# Check application logs
railway logs

# Verify health endpoint is responding
curl https://your-app.railway.app/health/database

# Check if application is starting properly
railway logs | grep -i "starting"
```

## Environment Variables

### Required Variables
```bash
MYSQLHOST=your-mysql-host
MYSQLDATABASE=your-database-name
MYSQLUSER=your-username
MYSQLPASSWORD=your-password
MYSQLPORT=3306
ASPNETCORE_ENVIRONMENT=Production
```

### Optional Variables
```bash
SEQ_URL=your-seq-url
SEQ_API_KEY=your-seq-api-key
AUTH0_ISSUER=your-auth0-issuer
AUTH0_AUDIENCE=your-auth0-audience
STORAGE_PROVIDER=local
```

## Monitoring and Alerting

### Railway Monitoring
- **Health Checks**: Automatic health check monitoring
- **Logs**: Structured logging with Serilog
- **Metrics**: Application performance metrics

### External Monitoring
```bash
# Health check monitoring
curl -f https://your-app.railway.app/health/database

# Basic health check
curl -f https://your-app.railway.app/health/basic
```

### Log Analysis
```bash
# Check for errors
railway logs | grep -i error

# Check for warnings
railway logs | grep -i warning

# Check migration status
railway logs | grep -i migration
```

## Rollback Procedures

### Emergency Rollback
```bash
# Rollback to previous deployment
railway rollback

# Or rollback to specific deployment
railway rollback <deployment-id>
```

### Database Rollback
```bash
# Rollback migrations
railway run dotnet ef database update PreviousMigrationName --startup-project ../Normaize.API

# Restore from backup
railway run mysql -h $MYSQLHOST -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE < backup.sql
```

## Best Practices

### 1. Always Test Locally
```bash
# Test migrations locally
dotnet ef database update --startup-project ../Normaize.API

# Test health checks locally
curl http://localhost:5000/health/database
```

### 2. Monitor Deployments
- Watch Railway logs during deployment
- Verify health checks pass
- Test critical functionality after deployment

### 3. Keep Backups
```bash
# Create database backup before deployment
mysqldump -h $MYSQLHOST -u $MYSQLUSER -p$MYSQLPASSWORD $MYSQLDATABASE > backup_$(date +%Y%m%d_%H%M%S).sql
```

### 4. Use Staging Environment
- Deploy to staging first
- Test thoroughly before production
- Use same configuration as production

## Storage Configuration

The application supports two storage providers:

### 1. In-Memory Storage (Default)
- **Use case**: Development, testing, fallback
- **Configuration**: `STORAGE_PROVIDER=memory` (or leave unset)
- **Behavior**: Files are stored in memory and lost on application restart

### 2. S3/MinIO Storage (Production)
- **Use case**: Beta and production environments
- **Configuration**: `STORAGE_PROVIDER=s3` with AWS credentials

### Setting Up MinIO on Railway

1. **Add MinIO Service to Railway**
   - Go to your Railway project
   - Click "New Service" → "Database" → "MinIO"
   - Railway will provision a MinIO instance

2. **Configure Environment Variables**
   ```bash
   # Storage Configuration
   STORAGE_PROVIDER=s3
   AWS_ACCESS_KEY_ID=your-minio-access-key
   AWS_SECRET_ACCESS_KEY=your-minio-secret-key
   AWS_REGION=us-east-1
   AWS_S3_BUCKET=normaize-uploads
   AWS_SERVICE_URL=https://your-minio-endpoint.railway.app
   ```

3. **File Organization Structure**
   Files are automatically organized by environment:
   ```
   normaize-uploads/
   ├── development/          # Development environment files
   │   └── 2024/01/15/
   │       └── [files]
   ├── beta/                 # Beta/Staging environment files
   │   └── 2024/01/15/
   │       └── [files]
   └── production/           # Production environment files
       └── 2024/01/15/
           └── [files]
   ```

4. **Environment Mapping**
   - `ASPNETCORE_ENVIRONMENT=Production` → `production/` folder
   - `ASPNETCORE_ENVIRONMENT=Staging` → `beta/` folder
   - `ASPNETCORE_ENVIRONMENT=Beta` → `beta/` folder
   - `ASPNETCORE_ENVIRONMENT=Development` → `development/` folder

### Testing Storage Configuration

Use the provided test script to verify your storage setup:
```bash
# Test environment folder structure
./scripts/test-environment-folders.ps1

# Test S3 configuration
./scripts/test-s3-config.ps1
```

### Troubleshooting Storage Issues

#### 1. Files Not Appearing in S3/MinIO
- Check that `STORAGE_PROVIDER=s3` is set
- Verify AWS credentials are correct
- Ensure `AWS_SERVICE_URL` points to your MinIO endpoint
- Check application logs for storage service initialization

#### 2. Wrong Environment Folder
- Verify `ASPNETCORE_ENVIRONMENT` is set correctly
- Check that the environment mapping logic is working
- Review storage service logs for folder creation

#### 3. Permission Issues
- Ensure S3 bucket or MinIO bucket exists and is accessible
- Verify access key has proper permissions
- Check MinIO service status in Railway dashboard

#### 4. Fallback to In-Memory Storage
- If S3 credentials are missing or invalid, the application will fall back to in-memory storage
- Check logs for "Falling back to memory storage" messages
- Verify all required S3 environment variables are set

## Summary

The Railway deployment is configured with comprehensive health checks and fail-fast behavior to ensure:

1. **Reliability**: Only healthy deployments succeed
2. **Visibility**: Clear logging and monitoring
3. **Recovery**: Automated and manual fix procedures
4. **Prevention**: Proactive health monitoring

This approach prevents the "Unknown column" errors and similar issues by ensuring the application only starts when the database is fully healthy and properly configured. 