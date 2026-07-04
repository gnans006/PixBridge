using EventPhoto.Application.Events.Commands;
using EventPhoto.Application.Events.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.Events;
using EventPhoto.Contracts.Responses.Events;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Manages photography events, including CRUD operations and QR code retrieval.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEventRepository _eventRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventsController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    /// <param name="eventRepository">The event repository.</param>
    public EventsController(IMediator mediator, IEventRepository eventRepository)
    {
        _mediator = mediator;
        _eventRepository = eventRepository;
    }

    /// <summary>
    /// Returns all active events.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The active event list.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<EventResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var includeInactive = User.Identity?.IsAuthenticated == true;
        var result = await _mediator.Send(new GetEventsQuery(includeInactive), cancellationToken);
        return Ok(ApiResponse<List<EventResponse>>.Ok(result.Value));
    }

    /// <summary>
    /// Returns a single event by identifier.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching event details.</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<EventResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Creates a new event and generates its QR code.
    /// </summary>
    /// <param name="request">The event creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created event payload.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<EventResponse>.Fail("Authenticated user identifier is missing."));
        }

        var result = await _mediator.Send(
            new CreateEventCommand(
                request.Name,
                request.EventType,
                request.EventDate,
                request.WatchFolder,
                request.Description,
                request.VenueName,
                request.ClientName,
                userId),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<EventResponse>.Fail(result.Error));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, ApiResponse<EventResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="request">The update request payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated event payload.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateEventCommand(
                id,
                request.Name,
                request.EventType,
                request.EventDate,
                request.Description,
                request.VenueName,
                request.ClientName),
            cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(ApiResponse<EventResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Soft-deletes an event.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An empty success envelope.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEventCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse.Fail(result.Error));
        }

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Activates or deactivates an event.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="activate">A value indicating whether the event should be activated.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated event payload.</returns>
    [HttpPatch("{id:guid}/active")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(Guid id, [FromQuery] bool activate, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ToggleEventActiveCommand(id, activate), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<EventResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<EventResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Serves the QR code image for an event.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The QR code image when available.</returns>
    [HttpGet("{id:guid}/qrcode")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode(Guid id, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(id, cancellationToken);
        if (eventEntity is null || string.IsNullOrWhiteSpace(eventEntity.QrCodePath) || !System.IO.File.Exists(eventEntity.QrCodePath))
        {
            return NotFound();
        }

        Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");

        var stream = new FileStream(eventEntity.QrCodePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
        return File(stream, "image/png");
    }

    /// <summary>
    /// Regenerates the QR code for an event using the current server URL.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/qrcode/refresh")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshQrCode(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshQrCodeCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse.Fail(result.Error));
        }

        return NoContent();
    }
}
