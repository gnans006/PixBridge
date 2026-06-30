using AutoMapper;
using EventPhoto.Application.Settings.Commands;
using EventPhoto.Application.Settings.Queries;
using EventPhoto.Contracts.Common;
using EventPhoto.Contracts.Requests.Settings;
using EventPhoto.Contracts.Responses.Settings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Manages application system settings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    /// <param name="mapper">The mapper instance.</param>
    public SettingsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns all configured system settings.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The system settings collection.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SystemSettingResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllSettingsQuery(), cancellationToken);
        var mapped = _mapper.Map<List<SystemSettingResponse>>(result.Value);
        return Ok(ApiResponse<List<SystemSettingResponse>>.Ok(mapped));
    }

    /// <summary>
    /// Updates a setting value by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An empty success envelope.</returns>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateSettingCommand(key, request.Value), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse.Fail(result.Error));
        }

        return Ok(ApiResponse.Ok());
    }
}
