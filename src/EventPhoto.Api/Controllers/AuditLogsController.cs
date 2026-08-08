using EventPhoto.Contracts.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>Platform audit log endpoints.</summary>
[ApiController]
[Route("api/platform/audit")]
[Authorize(Policy = "OwnerOnly")]
[Produces("application/json")]
public sealed class AuditLogsController(IAuditLogRepository auditLogRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var (items, total) = await auditLogRepository.GetPagedAsync(page, pageSize, entityType, action, cancellationToken);

        var result = new
        {
            items = items.Select(a => new
            {
                a.Id, a.ActorName, a.EntityType, a.EntityId,
                a.Action, a.Description, a.Timestamp, a.UserId
            }),
            total
        };

        return Ok(ApiResponse<object>.Ok(result));
    }
}
