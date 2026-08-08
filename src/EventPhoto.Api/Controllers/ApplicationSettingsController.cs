using EventPhoto.Application.ApplicationSettings.Commands;
using EventPhoto.Application.ApplicationSettings.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.Settings;
using EventPhoto.Contracts.Responses.Settings;
using EventPhoto.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Manages application-wide settings: studio identity, public URL, network info, and defaults.
/// Route: <c>/api/settings/application</c>
/// </summary>
[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class ApplicationSettingsController(IMediator mediator) : ControllerBase
{
    // ── GET /api/settings/application ────────────────────────────────────────

    /// <summary>Returns the current application settings.</summary>
    [HttpGet("application")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationSettingsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetApplicationSettingsQuery(), cancellationToken);
        var dto = result.Value!;
        var response = new ApplicationSettingsResponse(
            dto.Id,
            dto.StudioName,
            dto.ServerName,
            dto.PublicBaseUrl,
            dto.ServerPort,
            dto.DefaultEventGalleryMode.ToString(),
            dto.EnableWatermarkByDefault,
            dto.EnableFaceRecognitionByDefault,
            dto.CreatedAt,
            dto.UpdatedAt);

        return Ok(ApiResponse<ApplicationSettingsResponse>.Ok(response));
    }

    // ── PUT /api/settings/application ────────────────────────────────────────

    /// <summary>Updates the application settings.</summary>
    [HttpPut("application")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateApplicationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<GalleryMode>(request.DefaultEventGalleryMode, ignoreCase: true, out var galleryMode))
        {
            return BadRequest(ApiResponse.Fail(
                $"Invalid gallery mode '{request.DefaultEventGalleryMode}'. Valid values: GalleryOnly, FaceSearchOnly, Hybrid."));
        }

        var command = new UpdateApplicationSettingsCommand(
            request.StudioName,
            request.ServerName,
            request.PublicBaseUrl,
            request.ServerPort,
            galleryMode,
            request.EnableWatermarkByDefault,
            request.EnableFaceRecognitionByDefault);

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok())
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    // ── GET /api/settings/network-info ───────────────────────────────────────

    /// <summary>Returns real-time LAN network information for the current server.</summary>
    [HttpGet("network-info")]
    [ProducesResponseType(typeof(ApiResponse<NetworkInformationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNetworkInfo(
        [FromQuery] int port = 5000,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCurrentNetworkInformationQuery(port), cancellationToken);
        var dto = result.Value!;
        var response = new NetworkInformationResponse(
            dto.HostName,
            dto.MachineName,
            dto.PrimaryIpAddress,
            dto.Port,
            dto.AllIpAddresses,
            dto.AccessibleLanUrl,
            dto.IsLanReachable);

        return Ok(ApiResponse<NetworkInformationResponse>.Ok(response));
    }

    // ── POST /api/settings/test-public-url ───────────────────────────────────

    /// <summary>Tests whether a URL is reachable from the server.</summary>
    [HttpPost("test-public-url")]
    [ProducesResponseType(typeof(ApiResponse<TestPublicUrlResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestPublicUrl(
        [FromBody] TestPublicUrlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ValidatePublicBaseUrlCommand(request.Url), cancellationToken);
        var v = result.Value!;
        var response = new TestPublicUrlResponse(v.IsReachable, v.StatusCode, v.ResponseTimeMs, v.ErrorMessage);
        return Ok(ApiResponse<TestPublicUrlResponse>.Ok(response));
    }
}
