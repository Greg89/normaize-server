# MinIO Migration Summary

This document summarizes the changes made to support MinIO storage in the Normaize application.

## Changes Made

### 1. New MinIO Storage Service
- **File**: `Normaize.Data/Services/MinioStorageService.cs`
- **Purpose**: Implements `IStorageService` interface for MinIO/S3-compatible storage
- **Features**:
  - Automatic bucket creation
  - Date-based file organization (`yyyy/MM/dd/`)
  - Proper content type detection
  - Comprehensive error handling and logging
  - SSL/TLS support

### 2. Updated Dependencies
- **File**: `Normaize.Data/Normaize.Data.csproj`
- **Change**: Added MinIO NuGet package (`Minio` version `8.0.0`)

### 3. Updated Service Registration
- **File**: `Normaize.API/Program.cs`
- **Changes**:
  - Added MinIO case to storage provider switch
  - Added credential validation for MinIO
  - Graceful fallback to memory storage if credentials missing
  - Updated all storage services to use Data layer implementations

### 4. Updated File Upload Service
- **File**: `Normaize.Core/Services/FileUploadService.cs`
- **Change**: Updated `StorageProvider` detection to include MinIO URLs (`minio://`)

### 5. Moved All Storage Services to Data Layer
- **Files Moved**:
  - `Normaize.API/Services/MinioStorageService.cs` → `Normaize.Data/Services/MinioStorageService.cs`
  - `Normaize.API/Services/SftpStorageService.cs` → `Normaize.Data/Services/SftpStorageService.cs`
  - `Normaize.API/Services/LocalStorageService.cs` → `Normaize.Data/Services/LocalStorageService.cs`
  - `Normaize.API/Services/InMemoryStorageService.cs` → `Normaize.Data/Services/InMemoryStorageService.cs`
- **Purpose**: Proper clean architecture separation of concerns

### 6. Updated Environment Configuration
- **File**: `env.template`
- **Changes**: Added MinIO environment variables:
  - `MINIO_ENDPOINT`
  - `MINIO_ACCESS_KEY`
  - `MINIO_SECRET_KEY`
  - `MINIO_BUCKET_NAME`
  - `MINIO_USE_SSL`

### 7. New Test Script
- **File**: `scripts/test-minio-config.ps1`
- **Purpose**: Tests MinIO configuration logic and fallback behavior

### 8. New Documentation
- **File**: `docs/MINIO_RAILWAY_SETUP.md`
- **Purpose**: Comprehensive setup guide for MinIO on Railway

## Environment Variables

### Required for MinIO
```env
STORAGE_PROVIDER=minio
MINIO_ENDPOINT=your-minio-endpoint.railway.app
MINIO_ACCESS_KEY=your-minio-access-key
MINIO_SECRET_KEY=your-minio-secret-key
```

### Optional for MinIO
```env
MINIO_BUCKET_NAME=normaize-uploads
MINIO_USE_SSL=true
```

## File URL Format

Files stored in MinIO use the format:
```
minio://bucket-name/yyyy/MM/dd/guid_filename.ext
```

## Migration Steps

### 1. Deploy Code Changes
```bash
git add .
git commit -m "Add MinIO storage support"
git push
```

### 2. Create MinIO Service on Railway
- Go to Railway dashboard
- Create new MinIO service
- Note the provided environment variables

### 3. Update Application Environment
- Set `STORAGE_PROVIDER=minio`
- Add MinIO environment variables
- Deploy the application

### 4. Test Configuration
```powershell
.\scripts\test-minio-config.ps1
```

## Backward Compatibility

- **Existing SFTP files**: Will continue to work
- **New uploads**: Will go to MinIO (if configured) or fallback to memory
- **No data migration required**: Files remain in their original storage

## Testing

### Configuration Test
```powershell
.\scripts\test-minio-config.ps1
```

### Manual Testing
1. Upload a file through the API
2. Check logs for MinIO messages
3. Verify file appears in MinIO bucket
4. Test file retrieval

## Log Messages

### Successful MinIO Operations
```
[INF] MinIO Storage Service initialized with Endpoint: xxx, Bucket: xxx, SSL: True
[INF] MinIO bucket already exists: normaize-uploads
[INF] File uploaded successfully to MinIO: 2024/01/15/guid_filename.csv
```

### Fallback Scenarios
```
[WRN] MinIO storage requested but credentials not configured. Falling back to memory storage.
[INF] Using in-memory storage service for Development
```

## Benefits of MinIO

1. **S3 Compatibility**: Standard S3 API for easy integration
2. **Scalability**: Handles large files and high throughput
3. **Cost Effective**: Pay-per-use pricing on Railway
4. **Managed Service**: No server maintenance required
5. **Built-in Security**: SSL/TLS, access controls, and audit logs

## Next Steps

1. **Deploy to Railway** with MinIO configuration
2. **Test file uploads** with various file types
3. **Monitor performance** and adjust as needed
4. **Consider migrating** existing SFTP files if desired
5. **Set up monitoring** for storage usage and costs

## Support

- **Railway Documentation**: [Railway Docs](https://docs.railway.app/)
- **MinIO Documentation**: [MinIO Docs](https://docs.min.io/)
- **Application Logs**: Check Railway deployment logs for detailed information 