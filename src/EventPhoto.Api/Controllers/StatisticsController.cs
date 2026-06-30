using EventPhoto.Application.Statistics.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Responses.Statistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
