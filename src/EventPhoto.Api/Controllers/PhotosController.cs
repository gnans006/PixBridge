using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Application.Photos.Commands;
using EventPhoto.Application.Photos.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Responses.Photos;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Provides gallery browsing, thumbnail serving, downloads, and administrative photo operations.
/// Download access is enforced when <c>RestrictDownloadsToMatchedPhotos</c> is enabled on the event.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PhotosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPhotoRepository _photoRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IPhotoMatchRepository _photoMatchRepository;
    private readonly IGuestFaceSessionRepository _sessionRepository;
    private readonly IPhotoNotificationService _photoNotificationService;

    /// <summary>Initializes a new instance of the <see cref="PhotosController"/> class.</summary>
    public PhotosController(
        IMediator mediator,
        IPhotoRepository photoRepository,
        IEventRepository eventRepository,
        IPhotoMatchRepository photoMatchRepository,
        IGuestFaceSessionRepository sessionRepository,
        IPhotoNotificationService photoNotificationService)
    {
        _mediator = mediator;
        _photoRepository = photoRepository;
        _eventRepository = eventRepository;
        _photoMatchRepository = photoMatchRepository;
        _sessionRepository = sessionRepository;
        _photoNotificationService = photoNotificationService;
    }

    /// <summary>Returns a paged list of photos for an event.</summary>
    [HttpGet("event/{eventId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PhotoResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(
        Guid eventId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPhotosByEventQuery(eventId, page, pageSize), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<PagedResult<PhotoResponse>>.Ok(result.Value))
            : NotFound(ApiResponse.Fail(result.Error));
    }

    /// <summary>Returns metadata for a single photo.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPhotoByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<PhotoResponse>.Ok(result.Value))
            : NotFound(ApiResponse.Fail(result.Error));
    }

    /// <summary>Returns the thumbnail image for a photo.</summary>
    [HttpGet("{id:guid}/thumbnail")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken cancellationToken)
    {
        var photo = await _photoRepository.GetByIdAsync(id, cancellationToken);
        if (photo is null)
            return NotFound(ApiResponse.Fail($"Photo '{id}' was not found."));

        if (!System.IO.File.Exists(photo.ThumbnailPath))
            return NotFound(ApiResponse.Fail("Thumbnail not yet available."));

        var bytes = await System.IO.File.ReadAllBytesAsync(photo.ThumbnailPath, cancellationToken);
        return File(bytes, photo.MimeType ?? "image/jpeg");
    }

    /// <summary>
    /// Downloads the full-resolution photo.
    /// When <c>RestrictDownloadsToMatchedPhotos=true</c>, a valid <c>sessionToken</c>
    /// query parameter is required and the photo must appear in the session's matched results.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [EnableRateLimiting("downloads")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid id,
        [FromQuery] string? sessionToken,
        CancellationToken cancellationToken)
    {
        var photo = await _photoRepository.GetByIdAsync(id, cancellationToken);
        if (photo is null)
            return NotFound(ApiResponse.Fail($"Photo '{id}' was not found."));

        // Enforce face-search download restriction when configured on the event.
        var eventEntity = await _eventRepository.GetByIdAsync(photo.EventId, cancellationToken);
        if (eventEntity is not null && eventEntity.RestrictDownloadsToMatchedPhotos)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse.Fail("A session token is required to download photos for this event."));

            var session = await _sessionRepository.GetByTokenAsync(sessionToken, cancellationToken);
            if (session is null || session.IsExpired)
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse.Fail("Session not found or has expired."));

            var isMatched = await _photoMatchRepository.IsPhotoMatchedInSessionAsync(session.Id, id, cancellationToken);
            if (!isMatched)
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse.Fail("You can only download photos you appear in."));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _mediator.Send(
            new DownloadPhotoQuery(id, ipAddress, userAgent),
            cancellationToken);

        if (result.IsFailure)
            return NotFound(ApiResponse.Fail(result.Error));

        return File(result.Value.Data, result.Value.MimeType, result.Value.FileName);
    }

    /// <summary>Deletes a photo and its associated files.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existingPhoto = await _photoRepository.GetByIdAsync(id, cancellationToken);
        if (existingPhoto is null)
            return NotFound(ApiResponse.Fail($"Photo '{id}' was not found."));

        var result = await _mediator.Send(new DeletePhotoCommand(id), cancellationToken);
        if (result.IsFailure)
            return NotFound(ApiResponse.Fail(result.Error));

        await _photoNotificationService.NotifyPhotoDeletedAsync(existingPhoto.EventId, existingPhoto.Id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
