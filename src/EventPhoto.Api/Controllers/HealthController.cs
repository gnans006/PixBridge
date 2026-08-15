using EventPhoto.Application.Deployment.Queries;
using EventPhoto.Contracts.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Health endpoints for uptime monitoring, container probes, and service diagnostics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class HealthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Basic liveness probe — returns immediately without hitting any dependencies.
    /// Used by load balancers, containers, and monitoring tools.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new
    {
        status    = "healthy",
        server    = "PixBridge",
        timestamp = DateTimeOffset.UtcNow,
    });

    /// <summary>
    /// Deep service health check — probes PostgreSQL, AI service, storage, and the QR pipeline.
    /// Requires <c>OwnerOnly</c> authorisation (sensitive diagnostic data).
    /// </summary>
    [HttpGet("services")]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType(typeof(ApiResponse<ServiceHealthResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetServiceHealthQuery(), cancellationToken);
        return Ok(ApiResponse<ServiceHealthResult>.Ok(result.Value!));
    }
}

