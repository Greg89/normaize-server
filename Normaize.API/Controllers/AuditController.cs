using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Extensions;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController(IAuditService auditService, IStructuredLoggingService loggingService) : ControllerBase
{
    private readonly IAuditService _auditService = auditService;
    private readonly IStructuredLoggingService _loggingService = loggingService;

    [HttpGet("datasets/{dataSetId}")]
    public async Task<ActionResult<IEnumerable<DataSetAuditLog>>> GetDataSetAuditLogs(
        int dataSetId, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 50)
    {
        try
        {
            var auditLogs = await _auditService.GetDataSetAuditLogsAsync(dataSetId, skip, take);
            return Ok(auditLogs);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetDataSetAuditLogs({dataSetId})");
            return StatusCode(500, "Error retrieving audit logs");
        }
    }

    [HttpGet("user")]
    public async Task<ActionResult<IEnumerable<DataSetAuditLog>>> GetUserAuditLogs(
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            var auditLogs = await _auditService.GetUserAuditLogsAsync(userId, skip, take);
            return Ok(auditLogs);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetUserAuditLogs");
            return StatusCode(500, "Error retrieving user audit logs");
        }
    }

    [HttpGet("actions/{action}")]
    public async Task<ActionResult<IEnumerable<DataSetAuditLog>>> GetAuditLogsByAction(
        string action,
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 50)
    {
        try
        {
            var auditLogs = await _auditService.GetAuditLogsByActionAsync(action, skip, take);
            return Ok(auditLogs);
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetAuditLogsByAction({action})");
            return StatusCode(500, "Error retrieving audit logs by action");
        }
    }

    private string GetCurrentUserId()
    {
        return User.GetUserId();
    }
} 