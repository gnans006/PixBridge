using EventPhoto.Application.Events.Commands;
using EventPhoto.Application.Events.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.Events;
using EventPhoto.Contracts.Responses.Events;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Event Workspace endpoints — aggregate data and focused-update operations
/// used by the studio Event Workspace UI.
/// </summary>
[ApiController]
[Route("api/events/{eventId:guid}/workspace")]
[Authorize(Policy = "OperatorOrAbove")]
[Produces("application/json")]
public sealed class EventWorkspaceController(IMediator mediator) : ControllerBase
{
    // ── GET workspace ─────────────────────────────────────────────────────────

    /// <summary>Returns the consolidated workspace data for an event.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<EventWorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventWorkspaceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkspace(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEventWorkspaceQuery(eventId), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<EventWorkspaceResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventWorkspaceResponse>.Ok(result.Value));
    }

    // ── GET analytics ─────────────────────────────────────────────────────────

    /// <summary>Returns analytics for the analytics tab.</summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ApiResponse<EventAnalyticsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventAnalyticsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalytics(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEventAnalyticsQuery(eventId), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<EventAnalyticsResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventAnalyticsResponse>.Ok(result.Value));
    }

    // ── GET face recognition metrics ──────────────────────────────────────────

    /// <summary>Returns face recognition indexing metrics for the face recognition tab.</summary>
    [HttpGet("face-recognition/metrics")]
    [ProducesResponseType(typeof(ApiResponse<FaceRecognitionMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FaceRecognitionMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFaceMetrics(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetFaceRecognitionMetricsQuery(eventId), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<FaceRecognitionMetricsResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<FaceRecognitionMetricsResponse>.Ok(result.Value));
    }

    // ── GET storage metrics ───────────────────────────────────────────────────

    /// <summary>Returns storage metrics for the storage tab.</summary>
    [HttpGet("storage")]
    [ProducesResponseType(typeof(ApiResponse<StorageMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StorageMetricsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStorage(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStorageMetricsQuery(eventId), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<StorageMetricsResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<StorageMetricsResponse>.Ok(result.Value));
    }

    // ── PUT overview ──────────────────────────────────────────────────────────

    /// <summary>Updates the core event information from the overview tab.</summary>
    [HttpPut("overview")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOverview(
        Guid eventId,
        [FromBody] UpdateEventOverviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateEventOverviewCommand(
                eventId,
                request.Name,
                request.EventType,
                request.EventDate,
                request.Description,
                request.VenueName,
                request.ClientName),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(ApiResponse<EventResponse>.Fail(result.Error))
                : BadRequest(ApiResponse<EventResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventResponse>.Ok(result.Value));
    }

    // ── PUT gallery settings ──────────────────────────────────────────────────

    /// <summary>Updates the gallery access settings from the gallery tab.</summary>
    [HttpPut("gallery-settings")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGallerySettings(
        Guid eventId,
        [FromBody] UpdateGallerySettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateGallerySettingsCommand(
                eventId,
                request.AllowGalleryBrowsing,
                request.AllowFaceSearch,
                request.RestrictDownloadsToMatchedPhotos,
                request.GalleryRecentCount),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(ApiResponse<EventResponse>.Fail(result.Error))
                : BadRequest(ApiResponse<EventResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventResponse>.Ok(result.Value));
    }

    // ── PUT face recognition settings ─────────────────────────────────────────

    /// <summary>Updates face recognition settings from the face recognition tab.</summary>
    [HttpPut("face-recognition")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFaceRecognition(
        Guid eventId,
        [FromBody] UpdateFaceRecognitionSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateFaceRecognitionSettingsCommand(
                eventId,
                request.EnableFaceRecognition,
                request.FaceMatchThreshold,
                request.AllowFaceSearch),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(ApiResponse<EventResponse>.Fail(result.Error))
                : BadRequest(ApiResponse<EventResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventResponse>.Ok(result.Value));
    }

    // ── POST face index rebuild ───────────────────────────────────────────────

    /// <summary>
    /// Re-queues event photos for face indexing (background operation).
    /// By default only photos with no existing embeddings are queued (smart mode).
    /// Pass <c>?force=true</c> to re-index every photo (use after model upgrades).
    /// </summary>
    [HttpPost("face-recognition/rebuild")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RebuildFaceIndex(
        Guid eventId,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new RebuildFaceIndexCommand(eventId, force), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(ApiResponse<int>.Fail(result.Error))
                : BadRequest(ApiResponse<int>.Fail(result.Error));
        }

        return Ok(ApiResponse<int>.Ok(result.Value));
    }
}
