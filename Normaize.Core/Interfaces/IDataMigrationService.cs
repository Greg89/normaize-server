namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for handling data migrations and format standardization
/// </summary>
public interface IDataMigrationService
{
    /// <summary>
    /// Standardizes the PreviewData format for all datasets
    /// </summary>
    /// <returns>Number of datasets that were standardized</returns>
    Task<int> StandardizePreviewDataFormatAsync();
}