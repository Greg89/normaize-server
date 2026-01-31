using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// S3-compatible file storage service implementation for DDD architecture
/// Supports both AWS S3 and S3-compatible services like MinIO
/// </summary>
public class S3FileStorageService : IFileStorageService, IDisposable
{
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3FileStorageService> _logger;
    private bool _disposed;

    public S3FileStorageService(
        IConfiguration configuration,
        ILogger<S3FileStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(configuration);

        var accessKey = configuration["AWS_ACCESS_KEY_ID"]
            ?? throw new ArgumentException("AWS_ACCESS_KEY_ID configuration is required");
        var secretKey = configuration["AWS_SECRET_ACCESS_KEY"]
            ?? throw new ArgumentException("AWS_SECRET_ACCESS_KEY configuration is required");
        var region = configuration["AWS_REGION"] ?? "us-east-1";
        var serviceUrl = configuration["AWS_SERVICE_URL"]; // For MinIO

        _bucketName = configuration["AWS_S3_BUCKET"] ?? "normaize-uploads";

        // Create S3 client configuration
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };

        // If using MinIO or other S3-compatible service
        if (!string.IsNullOrEmpty(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
            config.ForcePathStyle = true; // Required for MinIO
        }

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);

        _logger.LogInformation("S3 File Storage initialized - Bucket: {Bucket}, Region: {Region}",
            _bucketName, region);

        // Ensure bucket exists
        Task.Run(async () => await EnsureBucketExistsAsync()).Wait();
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");

        // Get environment for folder structure
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLowerInvariant() ?? "development";
        var environmentFolder = MapEnvironmentFolder(environment);

        // URL encode userId to handle special characters like | in Auth0 IDs
        var encodedUserId = Uri.EscapeDataString(userId);

        // Create object key: environment/encodedUserId/date/filename
        var objectKey = $"{environmentFolder}/{encodedUserId}/{datePath}/{uniqueFileName}";

        _logger.LogInformation("Uploading file {FileName} to S3: {ObjectKey}", fileName, objectKey);

        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = GetContentType(fileName)
            };

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            _logger.LogInformation("File uploaded successfully to S3: {ObjectKey}", objectKey);

            // Return S3 URL format
            return $"s3://{_bucketName}/{objectKey}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {ObjectKey}", objectKey);
            throw new InvalidOperationException($"Failed to upload file to S3: {objectKey}", ex);
        }
    }

    public async Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var objectKey = ExtractObjectKeyFromUrl(filePath);

        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);

            // Copy to memory stream to avoid disposal issues
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found in S3: {ObjectKey}", objectKey);
            throw new FileNotFoundException($"File not found in S3: {objectKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from S3: {ObjectKey}", objectKey);
            throw new InvalidOperationException($"Failed to download file from S3: {objectKey}", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var objectKey = ExtractObjectKeyFromUrl(filePath);

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            _logger.LogInformation("File deleted from S3: {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {ObjectKey}", objectKey);
            throw new InvalidOperationException($"Failed to delete file from S3: {objectKey}", ex);
        }
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var objectKey = ExtractObjectKeyFromUrl(filePath);

        try
        {
            var headRequest = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            await _s3Client.GetObjectMetadataAsync(headRequest, cancellationToken);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring S3 bucket exists: {BucketName}", _bucketName);
            throw new InvalidOperationException($"Failed to ensure S3 bucket '{_bucketName}' exists", ex);
        }
    }

    private static string ExtractObjectKeyFromUrl(string filePath)
    {
        if (filePath.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
        {
            // Remove s3://bucketname/ prefix manually to avoid Uri encoding issues
            var pathAfterScheme = filePath.Substring("s3://".Length);
            var firstSlashIndex = pathAfterScheme.IndexOf('/');
            
            if (firstSlashIndex >= 0)
            {
                // Return everything after the bucket name
                return pathAfterScheme.Substring(firstSlashIndex + 1);
            }
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

    private static string MapEnvironmentFolder(string environment)
    {
        return environment switch
        {
            "production" => "production",
            "staging" => "beta",
            "beta" => "beta",
            "development" => "development",
            _ => "development"
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _s3Client?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
