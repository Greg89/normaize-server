# MinIO on Railway Setup Guide

This guide will help you set up MinIO storage for Normaize using Railway's MinIO service.

## Overview

MinIO is an S3-compatible object storage service that provides high-performance, scalable storage. Railway offers MinIO as a managed service, making it easy to deploy and manage.

## Prerequisites

- Railway account
- Normaize application already deployed on Railway
- Basic understanding of object storage concepts

## Step 1: Create MinIO Service on Railway

### Option A: Using Railway Dashboard

1. **Log into Railway** and navigate to your project
2. **Click "New Service"** → **"Database"**
3. **Select "MinIO"** from the database options
4. **Click "Deploy"** to create the service

### Option B: Using Railway CLI

```bash
# Install Railway CLI if you haven't already
npm install -g @railway/cli

# Login to Railway
railway login

# Create MinIO service
railway service create --name minio --type minio
```

## Step 2: Configure MinIO Service

Once the MinIO service is created, Railway will automatically provide environment variables. You'll need to:

1. **Navigate to your MinIO service** in the Railway dashboard
2. **Go to the "Variables" tab**
3. **Note down the following variables:**
   - `MINIO_ENDPOINT`
   - `MINIO_ACCESS_KEY`
   - `MINIO_SECRET_KEY`
   - `MINIO_BUCKET_NAME` (optional, defaults to "normaize-uploads")

## Step 3: Configure Normaize Application

### Update Environment Variables

In your Normaize application service on Railway:

1. **Go to the "Variables" tab**
2. **Add the following environment variables:**

```env
STORAGE_PROVIDER=minio
MINIO_ENDPOINT=your-minio-endpoint.railway.app
MINIO_ACCESS_KEY=your-minio-access-key
MINIO_SECRET_KEY=your-minio-secret-key
MINIO_BUCKET_NAME=normaize-uploads
MINIO_USE_SSL=true
```

### Example Configuration

```env
# Storage Configuration
STORAGE_PROVIDER=minio

# MinIO Configuration (Railway will provide these)
MINIO_ENDPOINT=minio-production.up.railway.app
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin123
MINIO_BUCKET_NAME=normaize-uploads
MINIO_USE_SSL=true
```

## Step 4: Deploy and Test

### Deploy the Application

1. **Commit and push your changes** to trigger a new deployment
2. **Monitor the deployment logs** to ensure MinIO service is properly configured
3. **Check the application logs** for MinIO initialization messages

### Test the Configuration

You can test the MinIO configuration using the provided script:

```powershell
# Run the MinIO configuration test
.\scripts\test-minio-config.ps1
```

### Test File Upload

1. **Upload a test file** through your application
2. **Check the logs** for MinIO upload messages
3. **Verify the file** appears in your MinIO bucket

## Step 5: Access MinIO Console (Optional)

Railway provides a web console for MinIO:

1. **Go to your MinIO service** in Railway dashboard
2. **Click "Deployments"** → **"Latest"**
3. **Find the "MinIO Console" URL** in the deployment logs
4. **Access the console** using the provided credentials

## Configuration Details

### Environment Variables

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `STORAGE_PROVIDER` | Storage provider type | Yes | `memory` |
| `MINIO_ENDPOINT` | MinIO server endpoint | Yes | - |
| `MINIO_ACCESS_KEY` | MinIO access key | Yes | - |
| `MINIO_SECRET_KEY` | MinIO secret key | Yes | - |
| `MINIO_BUCKET_NAME` | Bucket name for uploads | No | `normaize-uploads` |
| `MINIO_USE_SSL` | Use SSL for connections | No | `true` |

### File Organization

Files are organized in MinIO with the following structure:
```
normaize-uploads/
├── 2024/
│   ├── 01/
│   │   ├── 15/
│   │   │   ├── guid_filename1.csv
│   │   │   └── guid_filename2.xlsx
│   │   └── 16/
│   │       └── guid_filename3.json
│   └── 02/
│       └── 01/
│           └── guid_filename4.xml
```

### URL Format

Files are referenced using the format:
```
minio://bucket-name/yyyy/MM/dd/guid_filename.ext
```

## Security Considerations

### Access Control

- **MinIO credentials** are automatically managed by Railway
- **Bucket access** is restricted to your application
- **SSL/TLS** is enabled by default for secure connections

### Best Practices

1. **Never commit credentials** to version control
2. **Use Railway's built-in secret management**
3. **Regularly rotate access keys** if needed
4. **Monitor access logs** for unusual activity

## Troubleshooting

### Common Issues

#### 1. Connection Errors

**Symptoms:** Application fails to connect to MinIO
**Solutions:**
- Verify `MINIO_ENDPOINT` is correct
- Check `MINIO_USE_SSL` setting
- Ensure credentials are properly set

#### 2. Bucket Creation Errors

**Symptoms:** Application fails to create bucket
**Solutions:**
- Check MinIO service status in Railway
- Verify access key permissions
- Review application logs for specific errors

#### 3. File Upload Failures

**Symptoms:** Files fail to upload
**Solutions:**
- Check file size limits
- Verify bucket exists
- Review MinIO service logs

### Debug Commands

```bash
# Check MinIO service status
railway service logs minio

# Check application logs
railway service logs normaize-api

# Test MinIO connection
curl -I https://your-minio-endpoint.railway.app
```

### Log Analysis

Look for these log messages in your application:

```
[INF] MinIO Storage Service initialized with Endpoint: xxx, Bucket: xxx, SSL: True
[INF] MinIO bucket already exists: normaize-uploads
[INF] File uploaded successfully to MinIO: 2024/01/15/guid_filename.csv
```

## Cost Considerations

### Railway MinIO Pricing

- **Free tier**: Limited storage and bandwidth
- **Paid plans**: Based on storage usage and bandwidth
- **No setup fees**: Pay only for what you use

### Optimization Tips

1. **Use appropriate file sizes** for your use case
2. **Implement file compression** for large datasets
3. **Consider lifecycle policies** for old files
4. **Monitor usage** through Railway dashboard

## Migration from SFTP

If you're migrating from SFTP to MinIO:

1. **Update environment variables** as shown above
2. **Deploy the new configuration**
3. **Existing files** will remain in SFTP storage
4. **New uploads** will go to MinIO
5. **Consider migrating existing files** if needed

## Support

### Railway Support

- **Documentation**: [Railway Docs](https://docs.railway.app/)
- **Community**: [Railway Discord](https://discord.gg/railway)
- **Support**: Available through Railway dashboard

### MinIO Resources

- **Documentation**: [MinIO Docs](https://docs.min.io/)
- **API Reference**: [MinIO API](https://docs.min.io/docs/minio-client-api-reference.html)
- **Community**: [MinIO GitHub](https://github.com/minio/minio)

## Next Steps

After setting up MinIO:

1. **Test file uploads** with various file types
2. **Monitor performance** and adjust as needed
3. **Set up monitoring** for storage usage
4. **Consider backup strategies** for critical data
5. **Implement file lifecycle policies** if needed

This setup provides a scalable, reliable file storage solution for Normaize using Railway's managed MinIO service. 