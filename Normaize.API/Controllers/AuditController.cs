using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Extensions;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

/// <summary>
/// Controller for managing audit trail operations and retrieving audit logs
/// </summary>
/// <remarks>
/// This controller provides endpoints for accessing audit logs related to dataset operations.
/// It supports filtering by dataset, user, and action type, with pagination capabilities.
/// All endpoints require authentication and return audit trail information for compliance and monitoring purposes.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController(IAuditService auditService, IStructuredLoggingService loggingService) : BaseApiController(loggingService)
{
    private readonly IAuditService _auditService = auditService;

    /// <summary>
    /// Retrieves audit logs for a specific dataset
    /// </summary>
    /// <param name="dataSetId">The unique identifier of the dataset</param>
    /// <param name="skip">Number of records to skip for pagination (default: 0)</param>
    /// <param name="take">Number of records to take for pagination (default: 50, max: 100)</param>
    /// <returns>A collection of audit log entries for the specified dataset</returns>
    /// <remarks>
    /// This endpoint retrieves all audit trail entries associated with a specific dataset.
    /// The results are paginated and ordered by timestamp in descending order.
    /// 
    /// Example usage:
    /// GET /api/audit/datasets/123?skip=0&take=25
    /// 
    /// The response includes detailed information about each audit event including
    /// user actions, timestamps, IP addresses, and any changes made to the dataset.
    /// </remarks>
    [HttpGet("datasets/{dataSetId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DataSetAuditLog>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>> GetDataSetAuditLogs(
        int dataSetId,
        [FromQuery, Range(0, int.MaxValue, ErrorMessage = "Skip must be a non-negative integer")] int skip = 0,
        [FromQuery, Range(1, 100, ErrorMessage = "Take must be between 1 and 100")] int take = 50)
    {
        try
        {
            var auditLogs = await _auditService.GetDataSetAuditLogsAsync(dataSetId, skip, take);
            return Success(auditLogs);
        }
        catch (Exception ex)
        {
            return HandleException<IEnumerable<DataSetAuditLog>>(ex, $"GetDataSetAuditLogs({dataSetId})");
        }
    }

    /// <summary>
    /// Retrieves audit logs for the currently authenticated user
    /// </summary>
    /// <param name="skip">Number of records to skip for pagination (default: 0)</param>
    /// <param name="take">Number of records to take for pagination (default: 50, max: 100)</param>
    /// <returns>A collection of audit log entries for the current user</returns>
    /// <remarks>
    /// This endpoint retrieves all audit trail entries associated with the currently authenticated user.
    /// The user ID is automatically extracted from the authentication token.
    /// 
    /// Example usage:
    /// GET /api/audit/user?skip=0&take=25
    /// 
    /// The response includes all audit events performed by the user across all datasets,
    /// providing a comprehensive activity history for compliance and monitoring purposes.
    /// </remarks>
    [HttpGet("user")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DataSetAuditLog>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>> GetUserAuditLogs(
        [FromQuery, Range(0, int.MaxValue, ErrorMessage = "Skip must be a non-negative integer")] int skip = 0,
        [FromQuery, Range(1, 100, ErrorMessage = "Take must be between 1 and 100")] int take = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            var auditLogs = await _auditService.GetUserAuditLogsAsync(userId, skip, take);
            return Success(auditLogs);
        }
        catch (Exception ex)
        {
            return HandleException<IEnumerable<DataSetAuditLog>>(ex, "GetUserAuditLogs");
        }
    }

    /// <summary>
    /// Retrieves audit logs filtered by a specific action type
    /// </summary>
    /// <param name="action">The action type to filter by (e.g., "Created", "Updated", "Deleted", "Processed")</param>
    /// <param name="skip">Number of records to skip for pagination (default: 0)</param>
    /// <param name="take">Number of records to take for pagination (default: 50, max: 100)</param>
    /// <returns>A collection of audit log entries for the specified action type</returns>
    /// <remarks>
    /// This endpoint retrieves all audit trail entries that match a specific action type.
    /// Common action types include: Created, Updated, Deleted, Processed.
    /// 
    /// Example usage:
    /// GET /api/audit/actions/Created?skip=0&take=25
    /// 
    /// The response includes all audit events of the specified type across all datasets and users,
    /// useful for monitoring specific types of operations or compliance reporting.
    /// </remarks>
    [HttpGet("actions/{action}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DataSetAuditLog>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>> GetAuditLogsByAction(
        [Required(ErrorMessage = "Action is required"), StringLength(50, ErrorMessage = "Action cannot exceed 50 characters")] string action,
        [FromQuery, Range(0, int.MaxValue, ErrorMessage = "Skip must be a non-negative integer")] int skip = 0,
        [FromQuery, Range(1, 100, ErrorMessage = "Take must be between 1 and 100")] int take = 50)
    {
        try
        {
            var auditLogs = await _auditService.GetAuditLogsByActionAsync(action, skip, take);
            return Success(auditLogs);
        }
        catch (Exception ex)
        {
            return HandleException<IEnumerable<DataSetAuditLog>>(ex, $"GetAuditLogsByAction({action})");
        }
    }

    /// <summary>
    /// Gets the current user ID from the authentication context
    /// </summary>
    /// <returns>The user ID as a string</returns>
    /// <remarks>
    /// This private method extracts the user ID from the current authentication context.
    /// It uses the ClaimsPrincipalExtensions to retrieve the user identifier from the JWT token.
    /// </remarks>
    private string GetCurrentUserId()
    {
        return User.GetUserId();
    }
}