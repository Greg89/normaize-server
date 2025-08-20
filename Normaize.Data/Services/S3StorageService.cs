using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Constants;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace Normaize.Data.Services;

/// <summary>
/// S3-compatible storage service implementation for file operations.
/// Supports both AWS S3 and S3-compatible services like MinIO.
/// </summary>
/// <remarks>
/// This service provides file storage capabilities using S3-compatible APIs.
/// It automatically creates buckets if they don't exist and organizes files
/// by environment and date for better organization.
/// </remarks>
public class S3StorageService : IStorageService, IDisposable
{
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3StorageService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration service to retrieve AWS settings.</param>
    /// <param name="logger">The logger service for recording operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="logger"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required AWS configuration is missing.</exception>
    public S3StorageService(IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var accessKey = configuration[AppConstants.EnvironmentVariables.AWS_ACCESS_KEY_ID]
            ?? throw new ArgumentException("AWS_ACCESS_KEY_ID configuration is required");
        var secretKey = configuration[AppConstants.EnvironmentVariables.AWS_SECRET_ACCESS_KEY]
            ?? throw new ArgumentException("AWS_SECRET_ACCESS_KEY configuration is required");
        var region = configuration["AWS_REGION"] ?? "us-east-1";
        var serviceUrl = configuration["AWS_SERVICE_URL"]; // For MinIO or other S3-compatible services

        _bucketName = configuration["AWS_S3_BUCKET"] ?? "normaize-uploads";

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

        // Ensure bucket exists - use Task.Run to avoid blocking constructor
        Task.Run(async () => await EnsureBucketExistsAsync()).Wait();
    }

    #region IStorageService Implementation

    /// <summary>
    /// Saves a file to S3 storage with environment-based organization.
    /// </summary>
    /// <param name="fileRequest">The file upload request containing file information and stream.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the S3 URL of the uploaded file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileRequest"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileRequest"/> contains invalid data.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file upload operation fails.</exception>
    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        ValidateFileUploadRequest(fileRequest);

        var fileName = $"{Guid.NewGuid()}_{fileRequest.FileName}";
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");

        // Get environment for folder structure
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLowerInvariant() ?? AppConstants.Environment.DEVELOPMENT;

        // Map environment names to folder names
        var environmentFolder = GetEnvironmentFolder(environment);

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

    /// <summary>
    /// Retrieves a file from S3 storage as a stream.
    /// </summary>
    /// <param name="filePath">The S3 URL or object key of the file to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found in S3.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file retrieval operation fails.</exception>
    public async Task<Stream> GetFileAsync(string filePath)
    {
        ValidateFilePath(filePath);

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

    /// <summary>
    /// Deletes a file from S3 storage.
    /// </summary>
    /// <param name="filePath">The S3 URL or object key of the file to delete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file deletion operation fails.</exception>
    public async Task DeleteFileAsync(string filePath)
    {
        ValidateFilePath(filePath);

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

    /// <summary>
    /// Checks if a file exists in S3 storage.
    /// </summary>
    /// <param name="filePath">The S3 URL or object key of the file to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the file exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
    public async Task<bool> FileExistsAsync(string filePath)
    {
        ValidateFilePath(filePath);

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

    #endregion

    #region Private Methods

    /// <summary>
    /// Ensures the S3 bucket exists, creating it if necessary.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when bucket creation fails.</exception>
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

    /// <summary>
    /// Extracts the object key from an S3 URL or returns the path as-is.
    /// </summary>
    /// <param name="filePath">The S3 URL or file path.</param>
    /// <returns>The extracted object key or the original path.</returns>
    private static string ExtractObjectKeyFromUrl(string filePath)
    {
        if (filePath.StartsWith("s3://"))
        {
            var uri = new Uri(filePath);
            return uri.AbsolutePath.TrimStart('/');
        }
        return filePath;
    }

    /// <summary>
    /// Determines the MIME content type based on file extension.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The MIME content type for the file.</returns>
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

    /// <summary>
    /// Maps environment names to folder names for S3 organization.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <returns>The corresponding folder name.</returns>
    private static string GetEnvironmentFolder(string environment)
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

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates the file upload request parameters.
    /// </summary>
    /// <param name="fileRequest">The file upload request to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileRequest"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required properties are missing or invalid.</exception>
    private static void ValidateFileUploadRequest(FileUploadRequest fileRequest)
    {
        if (fileRequest == null)
            throw new ArgumentNullException(nameof(fileRequest));

        if (string.IsNullOrWhiteSpace(fileRequest.FileName))
            throw new ArgumentException("FileName is required", nameof(fileRequest));

        if (fileRequest.FileStream == null)
            throw new ArgumentException("FileStream is required", nameof(fileRequest));
    }

    /// <summary>
    /// Validates the file path parameter.
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
    private static void ValidateFilePath(string filePath)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of the S3 client and other managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the S3 client and other managed resources.
    /// </summary>
    /// <param name="disposing">True if called from Dispose, false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _s3Client?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}