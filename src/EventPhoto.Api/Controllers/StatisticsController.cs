using EventPhoto.Application.Statistics.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Responses.Statistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Provides dashboard and per-event statistics for administrators.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class StatisticsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatisticsController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    public StatisticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns aggregate statistics for the admin dashboard.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The dashboard statistics.</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
        return Ok(ApiResponse<DashboardStatsResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Returns detailed statistics for a specific event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The event statistics payload.</returns>
    [HttpGet("events/{eventId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EventStatisticsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventStatisticsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EventStats(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEventStatisticsQuery(eventId), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<EventStatisticsResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventStatisticsResponse>.Ok(result.Value));
    }

    /// <summary>Returns the consolidated KPI overview for the studio command centre.</summary>
    [HttpGet("dashboard/overview")]
    [ProducesResponseType(typeof(ApiResponse<DashboardOverviewResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardOverviewQuery(), cancellationToken);
        return Ok(ApiResponse<DashboardOverviewResponse>.Ok(result.Value));
    }

    /// <summary>Returns the most active event for the spotlight section.</summary>
    [HttpGet("dashboard/spotlight")]
    [ProducesResponseType(typeof(ApiResponse<EventSpotlightResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Spotlight(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEventSpotlightQuery(), cancellationToken);
        return Ok(ApiResponse<EventSpotlightResponse?>.Ok(result.Value));
    }

    /// <summary>Returns recent download activity for the dashboard timeline.</summary>
    [HttpGet("activity/recent")]
    [ProducesResponseType(typeof(ApiResponse<List<RecentActivityItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecentActivity(
        [FromQuery] int count = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetRecentActivityQuery(count), cancellationToken);
        return Ok(ApiResponse<List<RecentActivityItemResponse>>.Ok(result.Value));
    }

    /// <summary>Returns live storage analytics per event folder.</summary>
    [HttpGet("storage")]
    [ProducesResponseType(typeof(ApiResponse<StorageAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Storage(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStorageAnalyticsQuery(), cancellationToken);
        return Ok(ApiResponse<StorageAnalyticsResponse>.Ok(result.Value));
    }

    /// <summary>Returns face recognition analytics.</summary>
    [HttpGet("face-recognition")]
    [ProducesResponseType(typeof(ApiResponse<FaceAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FaceRecognition(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFaceAnalyticsQuery(), cancellationToken);
        return Ok(ApiResponse<FaceAnalyticsResponse>.Ok(result.Value));
    }

    /// <summary>Returns watermark analytics.</summary>
    [HttpGet("watermark")]
    [ProducesResponseType(typeof(ApiResponse<WatermarkAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Watermark(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWatermarkAnalyticsQuery(), cancellationToken);
        return Ok(ApiResponse<WatermarkAnalyticsResponse>.Ok(result.Value));
    }
}
