using EventPhoto.Application.FaceSearch.Commands;
using EventPhoto.Application.FaceSearch.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Responses.FaceSearch;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Exposes face-search endpoints for guests (no auth required) and
/// download validation endpoints that check session token authorisation.
/// </summary>
[ApiController]
[Route("api/face-search")]
public sealed class FaceSearchController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns the gallery configuration for an event (mode, face search availability, etc.).
    /// Used by the React landing page to decide which UI to render.
    /// </summary>
    [HttpGet("events/{eventId:guid}/config")]
    [ProducesResponseType(typeof(ApiResponse<GuestGalleryConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGalleryConfig(Guid eventId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetGuestGalleryConfigQuery(eventId), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<GuestGalleryConfigResponse>.Ok(result.Value))
            : NotFound(ApiResponse<GuestGalleryConfigResponse>.Fail(result.Error));
    }

    /// <summary>
    /// Starts a face-search session.
    /// Guest uploads their selfie; the system detects their face, generates an embedding,
    /// performs a pgvector HNSW search, and returns a session token with matched photo count.
    /// </summary>
    [HttpPost("events/{eventId:guid}/search")]
    [Consumes("multipart/form-data", "application/octet-stream", "image/jpeg", "image/png", "image/webp")]
    [RequestSizeLimit(10 * 1024 * 1024)]  // 10 MB — hard ceiling for selfie uploads
    [ProducesResponseType(typeof(ApiResponse<FaceSearchStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> StartFaceSearch(
        Guid eventId,
        [FromQuery] float? threshold,
        CancellationToken ct)
    {
        byte[]? selfieBytes = null;

        if (Request.HasFormContentType)
        {
            Request.EnableBuffering();
            var form = await Request.ReadFormAsync(ct);
            var file = form.Files.GetFile("selfie");
            if (file is not null && file.Length > 0)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, ct);
                selfieBytes = ms.ToArray();
            }
        }

        if (selfieBytes is null || selfieBytes.Length == 0)
        {
            var contentLength = Request.ContentLength ?? 0;
            if (contentLength > 0)
            {
                Request.EnableBuffering();
                using var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms, ct);
                selfieBytes = ms.ToArray();
            }
        }

        if (selfieBytes is null || selfieBytes.Length == 0)
            return BadRequest(ApiResponse.Fail("Selfie image is required. Upload a non-empty image file."));

        var result = await mediator.Send(
            new StartFaceSearchCommand(eventId, selfieBytes, threshold), ct);

        return result.IsSuccess
            ? Ok(ApiResponse<FaceSearchStatusResponse>.Ok(result.Value))
            : BadRequest(ApiResponse<FaceSearchStatusResponse>.Fail(result.Error));
    }

    /// <summary>
    /// Returns the current status of a guest face-search session.
    /// Used for polling if SignalR is unavailable.
    /// </summary>
    [HttpGet("{sessionToken}/status")]
    [ProducesResponseType(typeof(ApiResponse<FaceSearchStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(string sessionToken, CancellationToken ct)
    {
        var result = await mediator.Send(new GetFaceSearchStatusQuery(sessionToken), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<FaceSearchStatusResponse>.Ok(result.Value))
            : NotFound(ApiResponse<FaceSearchStatusResponse>.Fail(result.Error));
    }

    /// <summary>
    /// Returns paged matched photos for a completed face-search session.
    /// The download URLs embed the session token for authorisation.
    /// </summary>
    [HttpGet("{sessionToken}/results")]
    [ProducesResponseType(typeof(ApiResponse<FaceSearchResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResults(
        string sessionToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMatchedPhotosQuery(sessionToken, page, pageSize), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<FaceSearchResultResponse>.Ok(result.Value))
            : NotFound(ApiResponse<FaceSearchResultResponse>.Fail(result.Error));
    }
}
