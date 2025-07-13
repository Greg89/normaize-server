# Storage Service Simplification

## Overview

The storage services have been simplified to reduce complexity and maintenance overhead. The application now supports only two storage providers:

1. **In-Memory Storage** (default/fallback)
2. **S3/MinIO Storage** (production)

## Removed Storage Services

### SFTP Storage Service
- **File**: `Normaize.Data/Services/SftpStorageService.cs`
- **Reason**: Replaced by S3/MinIO for better reliability and easier management
- **Migration**: Existing SFTP users should migrate to S3/MinIO

### Local Storage Service
- **File**: `Normaize.Data/Services/LocalStorageService.cs`
- **Reason**: Not suitable for containerized environments like Railway
- **Migration**: Use in-memory for development, S3 for production

## Current Storage Options

### 1. In-Memory Storage (`STORAGE_PROVIDER=memory`)
- **Use case**: Development, testing, fallback
- **Behavior**: Files stored in memory, lost on restart
- **Configuration**: Default if no provider specified

### 2. S3/MinIO Storage (`STORAGE_PROVIDER=s3`)
- **Use case**: Beta and production environments
- **Behavior**: Files stored in S3-compatible storage with environment folders
- **Configuration**: Requires AWS credentials and optional MinIO endpoint

## Environment Variables

### Required for S3
```bash
STORAGE_PROVIDER=s3
AWS_ACCESS_KEY_ID=your-access-key
AWS_SECRET_ACCESS_KEY=your-secret-key
```

### Optional for S3
```bash
AWS_REGION=us-east-1                    # Default: us-east-1
AWS_S3_BUCKET=normaize-uploads          # Default: normaize-uploads
AWS_SERVICE_URL=https://minio-endpoint  # For MinIO compatibility
```

## File Organization

Files are automatically organized by environment:
```
normaize-uploads/
├── development/          # Development environment
├── beta/                 # Beta/Staging environment
└── production/           # Production environment
```

## Migration Guide

### From SFTP
1. Set up MinIO on Railway or AWS S3
2. Configure S3 environment variables
3. Set `STORAGE_PROVIDER=s3`
4. Existing files will need manual migration if required

### From Local Storage
1. For development: Use in-memory storage (default)
2. For production: Set up S3/MinIO storage
3. No file migration needed (local files remain on disk)

## Testing

Use the provided test scripts:
```bash
# Test storage configuration
./scripts/test-s3-config.ps1

# Test environment folder structure
./scripts/test-environment-folders.ps1
```

## Benefits of Simplification

1. **Reduced Complexity**: Fewer storage providers to maintain
2. **Better Reliability**: S3/MinIO is more reliable than SFTP
3. **Easier Deployment**: No SSH key management required
4. **Container-Friendly**: No local file system dependencies
5. **Better Scalability**: S3-compatible storage scales automatically

## Fallback Behavior

If S3 credentials are missing or invalid:
1. Application logs a warning
2. Falls back to in-memory storage
3. Continues to function (files lost on restart)

This ensures the application remains functional even with configuration issues. 