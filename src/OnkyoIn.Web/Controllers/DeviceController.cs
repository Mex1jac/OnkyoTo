using Microsoft.AspNetCore.Mvc;
using OnkyoIn.Web.Application.Dtos;
using OnkyoIn.Web.Application.Services;

namespace OnkyoIn.Web.Controllers;

/// <summary>
/// HTTP API consumed by the React SPA. Delegates to the OnkyoIn.Control API.
/// </summary>
[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly DeviceAppService _deviceApp;

    public DeviceController(DeviceAppService deviceApp)
    {
        _deviceApp = deviceApp;
    }

    /// <summary>Discovers an Onkyo device and returns its IP address.</summary>
    [HttpGet("discover")]
    public async Task<ActionResult<DiscoverResponse>> Discover(CancellationToken cancellationToken)
    {
        var result = await _deviceApp.DiscoverAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Sends the power-on command to the device.</summary>
    [HttpPost("power-on")]
    public async Task<IActionResult> PowerOn(DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        await _deviceApp.PowerOnAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Raises the device volume.</summary>
    [HttpPost("volume/up")]
    public async Task<IActionResult> VolumeUp(DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        await _deviceApp.VolumeUpAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Lowers the device volume.</summary>
    [HttpPost("volume/down")]
    public async Task<IActionResult> VolumeDown(DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        await _deviceApp.VolumeDownAsync(request, cancellationToken);
        return NoContent();
    }
}
