using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPhoto.Api.Controllers;

/// <summary>
/// Basic health endpoint for uptime and container checks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Returns the current service health state.
    /// </summary>
    /// <returns>A simple health payload.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        server = "PixBridge",
        timestamp = DateTimeOffset.UtcNow
    });
}
