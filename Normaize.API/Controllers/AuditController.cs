using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Extensions;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController(IAuditService auditService, IStructuredLoggingService loggingService) : BaseApiController(loggingService)
{
    private readonly IAuditService _auditService = auditService;

    [HttpGet("datasets/{dataSetId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>> GetDataSetAuditLogs(
        int dataSetId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
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

    [HttpGet("user")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>> GetUserAuditLogs(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
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

    [HttpGet("actions/{action}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>> GetAuditLogsByAction(
        string action,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
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

    private string GetCurrentUserId()
    {
        return User.GetUserId();
    }
}