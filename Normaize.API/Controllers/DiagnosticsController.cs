using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

/// <summary>
/// Controller for diagnostic operations and storage health monitoring
/// </summary>
/// <remarks>
/// This controller provides endpoints for diagnosing storage configuration,
/// testing storage connectivity, and monitoring system health. It supports
/// both S3 and local storage providers with comprehensive testing capabilities.
/// All endpoints require authentication and provide detailed logging for
/// troubleshooting and monitoring purposes.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiagnosticsController(
    IStructuredLoggingService loggingService,
    IStorageConfigurationService storageConfigService
) : BaseApiController(loggingService)
{
    /// <summary>
    /// Retrieves comprehensive storage configuration diagnostics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>
    /// Storage diagnostics information including provider type, configuration status,
    /// and environment details
    /// </returns>
    /// <remarks>
    /// This endpoint provides detailed information about the current storage configuration,
    /// including the active storage provider, S3 configuration status, and environment
    /// details. It's useful for troubleshooting storage issues and verifying configuration.
    /// 
    /// The response includes:
    /// - Current storage provider (S3, Local, Memory, Azure)
    /// - S3 configuration status and bucket information
    /// - Environment details
    /// - Configuration validation results
    /// </remarks>
    /// <response code="200">Storage diagnostics retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during diagnostics retrieval</response>
    [HttpGet("storage")]
    [ProducesResponseType(typeof(ApiResponse<StorageDiagnosticsDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<StorageDiagnosticsDto>>> GetStorageDiagnostics(CancellationToken cancellationToken = default)
    {
        try
        {
            _loggingService?.LogUserAction("Storage diagnostics requested", new { UserId = User?.Identity?.Name ?? "unknown" });

            var diagnostics = await Task.Run(storageConfigService.GetDiagnostics, cancellationToken);

            _loggingService?.LogUserAction("Storage diagnostics retrieved successfully", new
            {
                diagnostics.StorageProvider,
                diagnostics.S3Configured,
                diagnostics.Environment
            });

            return Success(diagnostics);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Request was cancelled");
        }
        catch (Exception ex)
        {
            return HandleException<StorageDiagnosticsDto>(ex, "GetStorageDiagnostics");
        }
    }

    /// <summary>
    /// Performs comprehensive storage connectivity and functionality tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>
    /// Detailed test results including file operations, connectivity status,
    /// and any encountered errors
    /// </returns>
    /// <remarks>
    /// This endpoint performs a comprehensive test of the storage service by:
    /// 1. Creating a test file with unique content
    /// 2. Saving the file to storage
    /// 3. Verifying the file exists
    /// 4. Retrieving and validating file content
    /// 5. Cleaning up by deleting the test file
    /// 
    /// The test verifies all CRUD operations (Create, Read, Update, Delete)
    /// and provides detailed results for troubleshooting storage issues.
    /// 
    /// Test results include:
    /// - Storage type being tested
    /// - Test result status (SUCCESS/FAILED)
    /// - File path used for testing
    /// - Content verification results
    /// - Error details if any operation fails
    /// </remarks>
    /// <response code="200">Storage test completed with results</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during storage testing</response>
    [HttpPost("test-storage")]
    [ProducesResponseType(typeof(ApiResponse<StorageTestResultDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<StorageTestResultDto>>> TestStorage(CancellationToken cancellationToken = default)
    {
        try
        {
            _loggingService?.LogUserAction("Storage test requested", new { UserId = User?.Identity?.Name ?? "unknown" });

            // Get the storage service from DI
            var storageService = HttpContext?.RequestServices.GetRequiredService<IStorageService>()
                ?? throw new InvalidOperationException("HttpContext is not available");
            var storageType = storageService.GetType().Name;

            // Test file operations
            var testFileName = $"test_{Guid.NewGuid()}.txt";
            var testContent = "This is a test file to verify storage connectivity.";

            using var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));

            var fileRequest = new FileUploadRequest
            {
                FileName = testFileName,
                ContentType = "text/plain",
                FileSize = testContent.Length,
                FileStream = testStream
            };

            try
            {
                // Test save
                var filePath = await storageService.SaveFileAsync(fileRequest);

                // Test exists
                var exists = await storageService.FileExistsAsync(filePath);

                // Test get
                using var retrievedStream = await storageService.GetFileAsync(filePath);
                using var reader = new StreamReader(retrievedStream);
                var retrievedContent = await reader.ReadToEndAsync(cancellationToken);

                // Test delete
                await storageService.DeleteFileAsync(filePath);

                var result = new StorageTestResultDto
                {
                    StorageType = storageType,
                    TestResult = "SUCCESS",
                    FilePath = filePath,
                    Exists = exists,
                    ContentMatch = retrievedContent == testContent,
                    Message = "Storage service is working correctly"
                };

                _loggingService?.LogUserAction("Storage test completed successfully", new
                {
                    StorageType = storageType,
                    FilePath = filePath,
                    result.ContentMatch
                });

                return Success(result);
            }
            catch (Exception ex)
            {
                var result = new StorageTestResultDto
                {
                    StorageType = storageType,
                    TestResult = "FAILED",
                    Error = ex.Message,
                    Message = "Storage service test failed"
                };

                _loggingService?.LogException(ex, "Storage test failed");

                return Success(result);
            }
        }
        catch (Exception ex)
        {
            return HandleException<StorageTestResultDto>(ex, "TestStorage");
        }
    }
}