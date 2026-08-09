using EventPhoto.Application.AiDiscovery.Commands;
using EventPhoto.Application.AiDiscovery.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Responses.AiDiscovery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Studio-facing AI Discovery Engine management endpoints.
/// Provides overview metrics, processing queue inspection, dead-letter management,
/// analytics, and per-event AI health data for the AI Studio dashboard.
///
/// All endpoints require admin authentication.
/// </summary>
[ApiController]
[Route("api/ai-studio")]
[Authorize]
public sealed class AiStudioController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns the aggregated overview metrics for the AI Studio header panel.
    /// Includes queue depth, success rate, pipeline health, and 24-hour search stats.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<AiStudioOverviewResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAiStudioOverviewQuery(), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<AiStudioOverviewResponse>.Ok(result.Value))
            : StatusCode(500, ApiResponse.Fail(result.Error));
    }

    /// <summary>
    /// Returns the paged processing queue showing pending, queued, and in-progress jobs.
    /// </summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(ApiResponse<ProcessingQueueResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProcessingQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetProcessingQueueQuery(page, pageSize), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<ProcessingQueueResponse>.Ok(result.Value))
            : StatusCode(500, ApiResponse.Fail(result.Error));
    }

    /// <summary>
    /// Returns the paged dead-letter queue for operator review of permanently failed jobs.
    /// </summary>
    [HttpGet("dead-letter")]
    [ProducesResponseType(typeof(ApiResponse<DeadLetterQueueResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeadLetterQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetDeadLetterQueueQuery(page, pageSize), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<DeadLetterQueueResponse>.Ok(result.Value))
            : StatusCode(500, ApiResponse.Fail(result.Error));
    }

    /// <summary>
    /// Promotes a dead-lettered job back to Pending for retry.
    /// Resets retry count and exponential back-off schedule.
    /// </summary>
    [HttpPost("dead-letter/{jobId:guid}/retry")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryDeadLetterJob(Guid jobId, CancellationToken ct)
    {
        var result = await mediator.Send(new RetryDeadLetterJobCommand(jobId), ct);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok())
            : NotFound(ApiResponse.Fail(result.Error));
    }

    /// <summary>
    /// Permanently ignores a dead-lettered job so it no longer appears in the queue.
    /// </summary>
    [HttpPost("dead-letter/{jobId:guid}/ignore")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IgnoreDeadLetterJob(Guid jobId, CancellationToken ct)
    {
        var result = await mediator.Send(new IgnoreDeadLetterJobCommand(jobId), ct);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok())
            : NotFound(ApiResponse.Fail(result.Error));
    }

    /// <summary>
    /// Returns per-event AI health metrics including index completion, search success rate, and queue status.
    /// </summary>
    [HttpGet("event-health")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EventAiHealthResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventHealth(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetEventAiHealthQuery(page, pageSize), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<IReadOnlyList<EventAiHealthResponse>>.Ok(result.Value))
            : StatusCode(500, ApiResponse.Fail(result.Error));
    }

    /// <summary>
    /// Returns AI search analytics: success rate, average duration, top events, and hourly volume.
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ApiResponse<AiAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] int windowHours = 24,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAiAnalyticsQuery(windowHours), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<AiAnalyticsResponse>.Ok(result.Value))
            : StatusCode(500, ApiResponse.Fail(result.Error));
    }
}
