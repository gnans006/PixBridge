using EventPhoto.Application.Auth.Commands;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.Auth;
using EventPhoto.Contracts.Responses.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Handles administrator authentication and profile endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Authenticates an administrator and returns a JWT access token.
    /// </summary>
    /// <param name="request">The login request payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A login response wrapped in the standard API envelope.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Username, request.Password), cancellationToken);
        if (result.IsFailure)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail(result.Error));
        }

        return Ok(ApiResponse<LoginResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Changes the authenticated administrator password.
    /// </summary>
    /// <param name="request">The password change request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An empty success envelope when the password change succeeds.</returns>
    [HttpPost("change-password")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Fail("Authenticated user identifier is missing."));
        }

        var result = await _mediator.Send(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmNewPassword),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ApiResponse.Fail(result.Error));
        }

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Returns the currently authenticated user profile.
    /// </summary>
    /// <returns>The caller profile details.</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            username = User.FindFirstValue(ClaimTypes.Name),
            role = User.FindFirstValue(ClaimTypes.Role)
        }));
    }
}
