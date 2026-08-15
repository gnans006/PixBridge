using EventPhoto.Application.GuestUpload.Commands;
using EventPhoto.Application.GuestUpload.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.GuestUpload;
using EventPhoto.Contracts.Responses.GuestUpload;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Guest photo upload endpoints.
/// Session management and moderation require authentication;
/// the public submission endpoint is anonymous.
/// </summary>
[ApiController]
[Route("api/events/{eventId:guid}/guest-uploads")]
[Produces("application/json")]
public sealed class GuestUploadController(IMediator mediator) : ControllerBase
{
    // ── Session Management (authenticated) ───────────────────────────────────

    /// <summary>Returns all upload sessions for an event.</summary>
    [HttpGet("sessions")]
    [Authorize(Policy = "ManagerOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GuestUploadSessionResponse>>), 200)]
    public async Task<IActionResult> GetSessions(Guid eventId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetGuestUploadSessionsQuery(eventId), ct);
        var response = result.Value!.Select(ToSessionResponse).ToList();
        return Ok(ApiResponse<IReadOnlyList<GuestUploadSessionResponse>>.Ok(response));
    }

    /// <summary>Creates a new guest upload session for the event.</summary>
    [HttpPost("sessions")]
    [Authorize(Policy = "ManagerOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<GuestUploadSessionResponse>), 200)]
    public async Task<IActionResult> CreateSession(
        Guid eventId,
        [FromBody] CreateGuestUploadSessionRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateGuestUploadSessionCommand(eventId, request.Title), ct);
        return Ok(ApiResponse<GuestUploadSessionResponse>.Ok(ToSessionResponse(result.Value!)));
    }

    /// <summary>Closes an active session.</summary>
    [HttpPost("sessions/{sessionId:guid}/close")]
    [Authorize(Policy = "ManagerOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> CloseSession(Guid eventId, Guid sessionId, CancellationToken ct)
    {
        var result = await mediator.Send(new CloseGuestUploadSessionCommand(sessionId), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<string>.Ok("Session closed."))
            : BadRequest(ApiResponse<string>.Fail(result.Error!));
    }

    // ── Moderation (authenticated) ────────────────────────────────────────────

    /// <summary>Returns all guest uploads for an event, optionally filtered by status.</summary>
    [HttpGet]
    [Authorize(Policy = "ManagerOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GuestUploadItemResponse>>), 200)]
    public async Task<IActionResult> GetUploads(
        Guid eventId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetGuestUploadsQuery(eventId, status), ct);
        var response = result.Value!.Select(ToUploadResponse).ToList();
        return Ok(ApiResponse<IReadOnlyList<GuestUploadItemResponse>>.Ok(response));
    }

    /// <summary>Approves or rejects a guest upload.</summary>
    [HttpPatch("{uploadId:guid}")]
    [Authorize(Policy = "ManagerOrOwner")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> Moderate(
        Guid eventId,
        Guid uploadId,
        [FromBody] ModerateGuestUploadRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ModerateGuestUploadCommand(uploadId, request.Approve, request.RejectionReason), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<string>.Ok(request.Approve ? "Approved." : "Rejected."))
            : BadRequest(ApiResponse<string>.Fail(result.Error!));
    }

    // ── Public Guest Submission ───────────────────────────────────────────────

    /// <summary>
    /// Accepts a file upload from a guest using a session code.
    /// No authentication required — the session code is the access control.
    /// </summary>
    [HttpPost("submit/{sessionCode}")]
    [AllowAnonymous]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB limit
    [ProducesResponseType(typeof(ApiResponse<GuestUploadItemResponse>), 200)]
    public async Task<IActionResult> Submit(
        Guid eventId,
        string sessionCode,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("No file provided."));

        // Validate MIME type — only images accepted
        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/heic" };
        if (!allowed.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(ApiResponse<string>.Fail("Only image files (JPEG, PNG, WEBP, HEIC) are accepted."));

        // Sanitise filename
        var ext       = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeName  = $"{Guid.NewGuid():N}{ext}";

        // Derive storage path relative to event guest-uploads folder
        var baseDir   = Path.Combine(AppContext.BaseDirectory, "guest-uploads", eventId.ToString(), sessionCode);
        Directory.CreateDirectory(baseDir);
        var storedPath = Path.Combine(baseDir, safeName);

        await using (var stream = System.IO.File.Create(storedPath))
            await file.CopyToAsync(stream, ct);

        var result = await mediator.Send(new SubmitGuestUploadCommand(
            sessionCode,
            file.FileName,
            storedPath,
            file.Length,
            file.ContentType), ct);

        if (!result.IsSuccess)
        {
            // Clean up file if DB write failed
            if (System.IO.File.Exists(storedPath))
                System.IO.File.Delete(storedPath);
            return BadRequest(ApiResponse<string>.Fail(result.Error!));
        }

        return Ok(ApiResponse<GuestUploadItemResponse>.Ok(ToUploadResponse(result.Value!)));
    }

    // ── Mapping helpers ────────────────────────────────────────────────────────

    private static GuestUploadSessionResponse ToSessionResponse(
        Domain.Entities.GuestUploadSession s) => new(
            s.Id, s.EventId, s.SessionCode, s.Title, s.PhotoCount,
            s.Status.ToString(), s.CreatedAt, s.ClosedAt);

    private static GuestUploadItemResponse ToUploadResponse(
        Domain.Entities.GuestUpload u) => new(
            u.Id, u.EventId, u.SessionId, u.OriginalFileName,
            u.StoredPath, u.ThumbnailPath, u.FileSizeBytes,
            u.ContentType, u.UploadedAt, u.ModerationStatus.ToString(),
            u.RejectionReason);
}
