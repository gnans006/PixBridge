using EventPhoto.Application.Watermark.Commands;
using EventPhoto.Application.Watermark.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.Events;
using EventPhoto.Contracts.Responses.Events;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Manages the watermark configuration for a photography event.
/// Watermarks are applied in memory at download time; original images are never modified.
/// </summary>
[ApiController]
[Route("api/events/{eventId:guid}/watermark-config")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class WatermarkController(IMediator mediator, ILogger<WatermarkController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the watermark configuration for an event.
    /// When no configuration has been saved, a default disabled configuration is returned.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WatermarkConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WatermarkConfigResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWatermarkConfigQuery(eventId), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<WatermarkConfigResponse>.Ok(result.Value))
            : NotFound(ApiResponse<WatermarkConfigResponse>.Fail(result.Error));
    }

    /// <summary>
    /// Creates or updates the watermark configuration for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="request">The watermark configuration payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<WatermarkConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WatermarkConfigResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WatermarkConfigResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert(
        Guid eventId,
        [FromBody] UpsertWatermarkConfigRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpsertWatermarkConfigCommand(
                eventId,
                request.Enabled,
                request.Mode,
                request.Style,
                request.Opacity,
                request.Scale,
                request.CustomText,
                request.Template,
                request.LogoPath,
                request.IncludeStudioName,
                request.IncludeEventName,
                request.IncludeDownloadDate,
                request.ApplyOnDownload,
                request.TextColor,
                request.FontName,
                request.BackgroundOpacity,
                request.ApplyOnPreview),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(ApiResponse<WatermarkConfigResponse>.Fail(result.Error));

            logger.LogWarning(
                "UpsertWatermarkConfig failed for event {EventId}: {Error}",
                eventId, result.Error);
            return BadRequest(ApiResponse<WatermarkConfigResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<WatermarkConfigResponse>.Ok(result.Value));
    }
}
