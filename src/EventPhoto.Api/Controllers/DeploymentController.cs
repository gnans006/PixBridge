using EventPhoto.Application.Deployment.Commands;
using EventPhoto.Application.Deployment.Queries;
using EventPhoto.Contracts.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Deployment Centre endpoints — deployment mode detection, service health,
/// and QR code mass-regeneration.
/// All endpoints require <c>OwnerOnly</c> authorisation.
/// </summary>
[ApiController]
[Route("api/deployment")]
[Authorize(Policy = "OwnerOnly")]
[Produces("application/json")]
public sealed class DeploymentController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns the current deployment status: mode, network identity, proxy detection,
    /// and a QR code health summary.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<DeploymentStatusResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        // Forward request headers so the service can detect reverse proxies
        var headers = Request.Headers
            .Select(h => new KeyValuePair<string, string>(h.Key, h.Value.ToString()));

        var result = await mediator.Send(new GetDeploymentStatusQuery(headers), cancellationToken);
        return Ok(ApiResponse<DeploymentStatusResult>.Ok(result.Value!));
    }

    /// <summary>
    /// Returns a real-time health check for all PixBridge infrastructure components:
    /// PostgreSQL, AI face-recognition service, local storage, and the QR pipeline.
    /// </summary>
    [HttpGet("services")]
    [ProducesResponseType(typeof(ApiResponse<ServiceHealthResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServiceHealth(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetServiceHealthQuery(), cancellationToken);
        return Ok(ApiResponse<ServiceHealthResult>.Ok(result.Value!));
    }

    /// <summary>
    /// Regenerates QR code PNG files for all non-deleted events using the current
    /// <c>PublicBaseUrl</c>. Returns the count of successfully regenerated codes.
    /// </summary>
    [HttpPost("regenerate-qr")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegenerateAllQr(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegenerateAllQrCodesCommand(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<int>.Ok(result.Value))
            : BadRequest(ApiResponse<int>.Fail(result.Error!));
    }
}
