
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Queries;
using MyApp.Shared.DTOs;

namespace MyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RemoteControlController : ControllerBase
{
    private readonly GetRemoteControlStatusQueryHandler _queryHandler;

    public RemoteControlController(GetRemoteControlStatusQueryHandler queryHandler)
    {
        _queryHandler = queryHandler;
    }

    /// <summary>
    /// Get current remote control status
    /// </summary>
    [HttpGet("status")]
    public ActionResult<RemoteControlDto> GetStatus()
    {
        var query = new GetRemoteControlStatusQuery();
        var result = _queryHandler.Handle(query);

        if (result == null)
            return NotFound(new { message = "Remote control not available" });

        return Ok(result);
    }

    /// <summary>
    /// Health check
    /// </summary>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "running", timestamp = DateTime.UtcNow });
    }
}