using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using Normaize.Core.Constants;
using Normaize.API.Services;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiagnosticsController : ControllerBase
{
    private readonly IStructuredLoggingService _loggingService;
    private readonly IAppConfigurationService _configService;

    public DiagnosticsController(IStructuredLoggingService loggingService, IAppConfigurationService configService)
    {
        _loggingService = loggingService;
        _configService = configService;
    }

    [HttpGet("storage")]
    public ActionResult<StorageDiagnosticsDto> GetStorageDiagnostics()
    {
        try
        {
            var storageProvider = _configService.Get("STORAGE_PROVIDER") ?? "default";
            var s3Bucket = _configService.Get("AWS_S3_BUCKET");
            var s3AccessKey = _configService.Get("AWS_ACCESS_KEY_ID");
            var s3SecretKey = _configService.Get("AWS_SECRET_ACCESS_KEY");
            var s3ServiceUrl = _configService.Get("AWS_SERVICE_URL");
            var environment = _configService.Get("ASPNETCORE_ENVIRONMENT") ?? AppConstants.ConfigStatus.NOT_SET;
            
            var diagnostics = new StorageDiagnosticsDto
            {
                StorageProvider = storageProvider,
                S3Configured = !string.IsNullOrEmpty(s3Bucket) && !string.IsNullOrEmpty(s3AccessKey) && !string.IsNullOrEmpty(s3SecretKey),
                S3Bucket = !string.IsNullOrEmpty(s3Bucket) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET,
                S3AccessKey = !string.IsNullOrEmpty(s3AccessKey) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET,
                S3SecretKey = !string.IsNullOrEmpty(s3SecretKey) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET,
                S3ServiceUrl = !string.IsNullOrEmpty(s3ServiceUrl) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET,
                Environment = environment
            };
            
            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetStorageDiagnostics");
            return StatusCode(500, "Error retrieving storage diagnostics");
        }
    }

    [HttpPost("test-storage")]
    public async Task<ActionResult<StorageTestResultDto>> TestStorage()
    {
        try
        {            
            // Get the storage service from DI
            var storageService = HttpContext.RequestServices.GetRequiredService<IStorageService>();
            var storageType = storageService.GetType().Name;
            
            // Test file operations
            var testFileName = $"test_{Guid.NewGuid()}.txt";
            var testContent = "This is a test file to verify storage connectivity.";
            var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));
            
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
                var retrievedContent = await reader.ReadToEndAsync();
                
                // Test delete
                await storageService.DeleteFileAsync(filePath);
                
                return Ok(new StorageTestResultDto
                {
                    StorageType = storageType,
                    TestResult = "SUCCESS",
                    FilePath = filePath,
                    Exists = exists,
                    ContentMatch = retrievedContent == testContent,
                    Message = "Storage service is working correctly"
                });
            }
            catch (Exception ex)
            {
                return Ok(new StorageTestResultDto
                {
                    StorageType = storageType,
                    TestResult = "FAILED",
                    Error = ex.Message,
                    Message = "Storage service test failed"
                });
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "TestStorage");
            return StatusCode(500, "Error testing storage");
        }
    }
} 