using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Application.Photos.Commands;
using EventPhoto.Application.Photos.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Responses.Photos;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Provides gallery browsing, thumbnail serving, downloads, and administrative photo operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PhotosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPhotoRepository _photoRepository;
    private readonly IDownloadLogRepository _downloadLogRepository;
    private readonly IPhotoNotificationService _photoNotificationService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotosController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    /// <param name="photoRepository">The photo repository.</param>
    /// <param name="downloadLogRepository">The download log repository.</param>
    /// <param name="photoNotificationService">The photo notification service.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public PhotosController(
        IMediator mediator,
        IPhotoRepository photoRepository,
        IDownloadLogRepository downloadLogRepository,
        IPhotoNotificationService photoNotificationService,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _photoRepository = photoRepository;
        _downloadLogRepository = downloadLogRepository;
        _photoNotificationService = photoNotificationService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Returns a paginated list of photos for an event gallery.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="page">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paginated gallery payload.</returns>
    [HttpGet("event/{eventId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PhotoResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PhotoResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByEvent(
        Guid eventId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPhotosByEventQuery(eventId, page, pageSize), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<PagedResult<PhotoResponse>>.Fail(result.Error));
        }

        return Ok(ApiResponse<PagedResult<PhotoResponse>>.Ok(result.Value));
    }

    /// <summary>
    /// Returns metadata for a single photo.
    /// </summary>
    /// <param name="id">The photo identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching photo metadata.</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhotoResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPhotoByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<PhotoResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<PhotoResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Serves the generated thumbnail for a photo.
    /// </summary>
    /// <param name="id">The photo identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The thumbnail image.</returns>
    [HttpGet("{id:guid}/thumbnail")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken cancellationToken)
    {
        var photo = await _photoRepository.GetByIdAsync(id, cancellationToken);
        if (photo is null || string.IsNullOrWhiteSpace(photo.ThumbnailPath) || !System.IO.File.Exists(photo.ThumbnailPath))
        {
            return NotFound();
        }

        var stream = new FileStream(photo.ThumbnailPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
        return File(stream, "image/jpeg");
    }

    /// <summary>
    /// Downloads the original full-resolution photo and records the download event.
    /// </summary>
    /// <param name="id">The photo identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The original photo file.</returns>
    [HttpGet("{id:guid}/download")]
    [AllowAnonymous]
    [EnableRateLimiting("downloads")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var photo = await _photoRepository.GetByIdAsync(id, cancellationToken);
        if (photo is null || !System.IO.File.Exists(photo.OriginalPath))
        {
            return NotFound();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var downloadLog = DownloadLog.Create(photo.Id, photo.EventId, ipAddress, userAgent);

        await _downloadLogRepository.AddAsync(downloadLog, cancellationToken);
        photo.RecordDownload();
        await _photoRepository.UpdateAsync(photo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var stream = new FileStream(photo.OriginalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
        return File(stream, photo.MimeType, photo.FileName);
    }

    /// <summary>
    /// Soft-deletes a photo and broadcasts the deletion event.
    /// </summary>
    /// <param name="id">The photo identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An empty success envelope.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existingPhoto = await _photoRepository.GetByIdAsync(id, cancellationToken);
        if (existingPhoto is null)
        {
            return NotFound(ApiResponse.Fail($"Photo '{id}' was not found."));
        }

        var result = await _mediator.Send(new DeletePhotoCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse.Fail(result.Error));
        }

        await _photoNotificationService.NotifyPhotoDeletedAsync(existingPhoto.EventId, existingPhoto.Id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
