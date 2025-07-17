using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using Normaize.Core.Constants;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiagnosticsController : ControllerBase
{
    private readonly IStructuredLoggingService _loggingService;
    private readonly IStorageConfigurationService _storageConfigService;

    public DiagnosticsController(
        IStructuredLoggingService loggingService, 
        IStorageConfigurationService storageConfigService)
    {
        _loggingService = loggingService;
        _storageConfigService = storageConfigService;
    }

    [HttpGet("storage")]
    public ActionResult<StorageDiagnosticsDto> GetStorageDiagnostics(CancellationToken cancellationToken = default)
    {
        try
        {
            _loggingService.LogUserAction("Storage diagnostics requested", new { UserId = User.Identity?.Name });
            
            var diagnostics = _storageConfigService.GetDiagnostics();
            
            _loggingService.LogUserAction("Storage diagnostics retrieved successfully", new { 
                StorageProvider = diagnostics.StorageProvider,
                S3Configured = diagnostics.S3Configured,
                Environment = diagnostics.Environment
            });
            
            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetStorageDiagnostics");
            return StatusCode(500, "Error retrieving storage diagnostics");
        }
    }

    [HttpPost("test-storage")]
    public async Task<ActionResult<StorageTestResultDto>> TestStorage(CancellationToken cancellationToken = default)
    {
        try
        {
            _loggingService.LogUserAction("Storage test requested", new { UserId = User.Identity?.Name });
            
            // Get the storage service from DI
            var storageService = HttpContext.RequestServices.GetRequiredService<IStorageService>();
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
                
                _loggingService.LogUserAction("Storage test completed successfully", new { 
                    StorageType = storageType,
                    FilePath = filePath,
                    ContentMatch = result.ContentMatch
                });
                
                return Ok(result);
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
                
                _loggingService.LogException(ex, "Storage test failed");
                
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "TestStorage");
            return StatusCode(500, "Error testing storage");
        }
    }
} 