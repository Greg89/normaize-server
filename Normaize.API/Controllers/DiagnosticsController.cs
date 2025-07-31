using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiagnosticsController(
    IStructuredLoggingService loggingService,
    IStorageConfigurationService _storageConfigService
) : BaseApiController(loggingService)
{

    [HttpGet("storage")]
    public async Task<ActionResult<ApiResponse<StorageDiagnosticsDto>>> GetStorageDiagnostics(CancellationToken cancellationToken = default)
    {
        try
        {
            _loggingService?.LogUserAction("Storage diagnostics requested", new { UserId = User?.Identity?.Name ?? "unknown" });

            var diagnostics = await Task.Run(_storageConfigService.GetDiagnostics, cancellationToken);

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

    [HttpPost("test-storage")]
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