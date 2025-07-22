using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace Normaize.Data.Services;

public class S3StorageService : IStorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        var accessKey = configuration["AWS_ACCESS_KEY_ID"] ?? throw new ArgumentException("AWS_ACCESS_KEY_ID configuration is required");
        var secretKey = configuration["AWS_SECRET_ACCESS_KEY"] ?? throw new ArgumentException("AWS_SECRET_ACCESS_KEY configuration is required");
        var region = configuration["AWS_REGION"] ?? "us-east-1";
        var serviceUrl = configuration["AWS_SERVICE_URL"]; // For MinIO or other S3-compatible services
        
        _bucketName = configuration["AWS_S3_BUCKET"] ?? "normaize-uploads";
        _logger = logger;

        // Create S3 client
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };

        // If using MinIO or other S3-compatible service, set the service URL
        if (!string.IsNullOrEmpty(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
            config.ForcePathStyle = true; // Required for MinIO
        }

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);

        _logger.LogInformation("S3 Storage Service initialized with Bucket: {Bucket}, Region: {Region}, ServiceURL: {ServiceURL}", 
            _bucketName, region, serviceUrl ?? "AWS S3");
        
        // Debug logging
        _logger.LogInformation("DEBUG: S3 Client Configuration - AccessKey: {AccessKey}, SecretKey: {SecretKey}, ServiceURL: {ServiceURL}", 
            string.Concat(accessKey.AsSpan(0, Math.Min(8, accessKey.Length)), "..."), 
            string.Concat(secretKey.AsSpan(0, Math.Min(8, secretKey.Length)), "..."), 
            serviceUrl ?? "Not set");

        // Ensure bucket exists
        EnsureBucketExistsAsync().Wait();
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            
            if (!bucketExists)
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                };

                await _s3Client.PutBucketAsync(putBucketRequest);
                _logger.LogInformation("Created S3 bucket: {BucketName}", _bucketName);
            }
            else
            {
                _logger.LogInformation("S3 bucket already exists: {BucketName}", _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring S3 bucket exists: {BucketName}", _bucketName);
            throw new InvalidOperationException($"Failed to ensure S3 bucket '{_bucketName}' exists", ex);
        }
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        var fileName = $"{Guid.NewGuid()}_{fileRequest.FileName}";
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        
        // Get environment for folder structure
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLowerInvariant() ?? "development";
        
        // Map environment names to folder names
        var environmentFolder = environment switch
        {
            "production" => "production",
            "staging" => "beta",
            "beta" => "beta",
            "development" => "development",
            _ => "development"
        };
        
        // Create object key with environment folder: environment/date/filename
        var objectKey = $"{environmentFolder}/{datePath}/{fileName}";
        
        _logger.LogInformation("Attempting to upload file {FileName} to S3 object {ObjectKey} in environment {Environment}", 
            fileRequest.FileName, objectKey, environmentFolder);
        
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = fileRequest.FileStream,
                ContentType = GetContentType(fileRequest.FileName)
            };

            await _s3Client.PutObjectAsync(putRequest);
            
            _logger.LogInformation("File uploaded successfully to S3: {ObjectKey} in environment {Environment}", objectKey, environmentFolder);
            _logger.LogInformation("DEBUG: File URL returned: s3://{Bucket}/{ObjectKey}", _bucketName, objectKey);
            return $"s3://{_bucketName}/{objectKey}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {ObjectKey}", objectKey);
            throw new InvalidOperationException($"Failed to upload file to S3: {objectKey}", ex);
        }
    }

    public async Task<Stream> GetFileAsync(string filePath)
    {
        var objectKey = ExtractObjectKeyFromUrl(filePath);
        
        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectAsync(getRequest);
            
            // Copy to memory stream to avoid disposal issues
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"File not found in S3: {objectKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from S3: {ObjectKey}", objectKey);
            throw new InvalidOperationException($"Failed to download file from S3: {objectKey}", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        var objectKey = ExtractObjectKeyFromUrl(filePath);
        
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
            _logger.LogInformation("File deleted from S3: {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {ObjectKey}", objectKey);
            throw new InvalidOperationException($"Failed to delete file from S3: {objectKey}", ex);
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        var objectKey = ExtractObjectKeyFromUrl(filePath);
        
        try
        {
            var headRequest = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            await _s3Client.GetObjectMetadataAsync(headRequest);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence in S3: {ObjectKey}", objectKey);
            return false;
        }
    }

    private static string ExtractObjectKeyFromUrl(string filePath)
    {
        if (filePath.StartsWith("s3://"))
        {
            var uri = new Uri(filePath);
            return uri.AbsolutePath.TrimStart('/');
        }
        return filePath;
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".parquet" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }
} 