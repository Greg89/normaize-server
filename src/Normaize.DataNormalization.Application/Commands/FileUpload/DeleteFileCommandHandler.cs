using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Application.Commands.FileUpload;

/// <summary>
/// Handler for deleting files
/// </summary>
public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, DeleteFileResult>
{
    private readonly IFileStorageService _storageService;
    private readonly ILogger<DeleteFileCommandHandler> _logger;

    public DeleteFileCommandHandler(
        IFileStorageService storageService,
        ILogger<DeleteFileCommandHandler> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeleteFileResult> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting file for user {UserId}: {FilePath}", request.UserId, request.FilePath);

        try
        {
            await _storageService.DeleteFileAsync(request.FilePath, cancellationToken);
            
            _logger.LogInformation("File deleted successfully: {FilePath}", request.FilePath);
            return new DeleteFileResult(Success: true);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("File not found during deletion: {FilePath}", request.FilePath);
            // Don't treat missing file as an error (already deleted)
            return new DeleteFileResult(Success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath}", request.FilePath);
            return new DeleteFileResult(Success: false, Error: $"Delete failed: {ex.Message}");
        }
    }
}
