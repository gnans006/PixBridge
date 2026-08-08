using EventPhoto.Application.StudioUsers.Commands;
using EventPhoto.Application.StudioUsers.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.Users;
using EventPhoto.Contracts.Responses.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Manages studio user accounts (listing, creation, editing, activation, password reset).
/// </summary>
[ApiController]
[Route("api/studio/users")]
[Authorize(Policy = "OwnerOnly")]
[Produces("application/json")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StudioUserResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllStudioUsersQuery(), cancellationToken);
        return Ok(ApiResponse<List<StudioUserResponse>>.Ok(result.Value));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StudioUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudioUserResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStudioUserByIdQuery(id), cancellationToken);
        if (result.IsFailure) return NotFound(ApiResponse<StudioUserResponse>.Fail(result.Error));
        return Ok(ApiResponse<StudioUserResponse>.Ok(result.Value));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StudioUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<StudioUserResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStudioUserRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateStudioUserCommand(
            request.FullName, request.Username, request.Email, request.Phone,
            request.Role, request.Password), cancellationToken);

        if (result.IsFailure) return BadRequest(ApiResponse<StudioUserResponse>.Fail(result.Error));
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, ApiResponse<StudioUserResponse>.Ok(result.Value));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StudioUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudioUserResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudioUserRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateStudioUserCommand(
            id, request.FullName, request.Email, request.Phone, request.Role), cancellationToken);

        if (result.IsFailure) return BadRequest(ApiResponse<StudioUserResponse>.Fail(result.Error));
        return Ok(ApiResponse<StudioUserResponse>.Ok(result.Value));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivateStudioUserCommand(id), cancellationToken);
        if (result.IsFailure) return BadRequest(ApiResponse.Fail(result.Error));
        return Ok(ApiResponse.Ok());
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ActivateStudioUserCommand(id), cancellationToken);
        if (result.IsFailure) return BadRequest(ApiResponse.Fail(result.Error));
        return Ok(ApiResponse.Ok());
    }

    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(ApiResponse.Fail("Passwords do not match."));

        var result = await mediator.Send(new ResetStudioUserPasswordCommand(id, request.NewPassword), cancellationToken);
        if (result.IsFailure) return BadRequest(ApiResponse.Fail(result.Error));
        return Ok(ApiResponse.Ok());
    }
}
