using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Application.Queries.FileUpload;

/// <summary>
/// Handler for checking if a file exists
/// </summary>
public class CheckFileExistsQueryHandler : IRequestHandler<CheckFileExistsQuery, CheckFileExistsResult>
{
    private readonly IFileStorageService _storageService;
    private readonly ILogger<CheckFileExistsQueryHandler> _logger;

    public CheckFileExistsQueryHandler(
        IFileStorageService storageService,
        ILogger<CheckFileExistsQueryHandler> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CheckFileExistsResult> Handle(CheckFileExistsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var stream = await _storageService.GetFileAsync(request.FilePath, cancellationToken);

            if (stream != null)
            {
                await stream.DisposeAsync();
                return new CheckFileExistsResult(Exists: true);
            }

            return new CheckFileExistsResult(Exists: false);
        }
        catch (FileNotFoundException)
        {
            return new CheckFileExistsResult(Exists: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists: {FilePath}", request.FilePath);
            return new CheckFileExistsResult(Exists: false, Error: ex.Message);
        }
    }
}
