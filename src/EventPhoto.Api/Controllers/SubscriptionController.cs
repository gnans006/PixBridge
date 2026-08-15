using EventPhoto.Application.Subscription.Commands;
using EventPhoto.Application.Subscription.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.Subscription;
using EventPhoto.Contracts.Responses.Subscription;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Studio subscription / license management endpoints.
/// All endpoints require <c>OwnerOnly</c> authorisation.
/// </summary>
[ApiController]
[Route("api/subscription")]
[Authorize(Policy = "OwnerOnly")]
[Produces("application/json")]
public sealed class SubscriptionController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns the current subscription state.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionResponse>), 200)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetSubscriptionQuery(), ct);
        return Ok(ApiResponse<SubscriptionResponse>.Ok(ToResponse(result.Value!)));
    }

    /// <summary>Activates the subscription with a purchased license key.</summary>
    [HttpPost("activate")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionResponse>), 200)]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateSubscriptionRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ActivateSubscriptionCommand(
            request.LicenseKey,
            request.StudioEmail,
            request.Plan,
            request.ExpiresAt), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<string>.Fail(result.Error!));

        // Return fresh state after activation
        var current = await mediator.Send(new GetSubscriptionQuery(), ct);
        return Ok(ApiResponse<SubscriptionResponse>.Ok(ToResponse(current.Value!)));
    }

    // ── Mapping ──────────────────────────────────────────────────────────────

    private static SubscriptionResponse ToResponse(Domain.Entities.Subscription s) => new(
        s.Plan.ToString(),
        s.State.ToString(),
        s.LicenseKey,
        s.StudioEmail,
        s.ActivatedAt,
        s.ExpiresAt,
        s.GracePeriodEndsAt,
        s.MaxEvents,
        s.MaxUsersPerStudio,
        s.IsOperational,
        s.GracePeriodDaysRemaining,
        s.Notes);
}
