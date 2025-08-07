using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for managing dataset lifecycle operations including restore, reset, and retention policies.
/// </summary>
public interface IDataSetLifecycleService
{
    /// <summary>
    /// Enhanced restore operation with configurable restore type.
    /// </summary>
    Task<OperationResultDto> RestoreDataSetEnhancedAsync(int id, DataSetRestoreDto restoreDto, string userId);

    /// <summary>
    /// Reset dataset to original state by reprocessing the original file.
    /// </summary>
    Task<OperationResultDto> ResetDataSetAsync(int id, DataSetResetDto resetDto, string userId);

    /// <summary>
    /// Update retention policy for a dataset.
    /// </summary>
    Task<OperationResultDto> UpdateRetentionPolicyAsync(int id, DataSetRetentionDto retentionDto, string userId);

    /// <summary>
    /// Get retention status for a dataset.
    /// </summary>
    Task<DataSetRetentionStatusDto?> GetRetentionStatusAsync(int id, string userId);

    /// <summary>
    /// Simple restore operation (legacy support).
    /// </summary>
    Task<bool> RestoreDataSetAsync(int id, string userId);

    /// <summary>
    /// Hard delete operation (legacy support).
    /// </summary>
    Task<bool> HardDeleteDataSetAsync(int id, string userId);
}