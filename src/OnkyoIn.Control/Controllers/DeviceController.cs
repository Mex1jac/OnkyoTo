using Microsoft.AspNetCore.Mvc;
using OnkyoIn.Control.Application.Dtos;
using OnkyoIn.Control.Application.Services;

namespace OnkyoIn.Control.Controllers;

/// <summary>
/// HTTP API that exposes device control use cases.
/// </summary>
[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly DeviceControlService _deviceControl;

    public DeviceController(DeviceControlService deviceControl)
    {
        _deviceControl = deviceControl;
    }

    /// <summary>Discovers an Onkyo device on the network and returns its IP address.</summary>
    [HttpGet("discover")]
    public async Task<ActionResult<DiscoverResponse>> Discover(CancellationToken cancellationToken)
    {
        var result = await _deviceControl.DiscoverAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Reads the current power and volume state of the device.</summary>
    [HttpGet("state")]
    public async Task<ActionResult<DeviceStateResponse>> GetState(
        [FromQuery] string ipAddress,
        CancellationToken cancellationToken)
    {
        var result = await _deviceControl.GetStateAsync(ipAddress, cancellationToken);
        return Ok(result);
    }

    /// <summary>Reads the input sources available on the device.</summary>
    [HttpGet("inputs")]
    public async Task<ActionResult<IReadOnlyList<InputSourceResponse>>> GetInputs(
        [FromQuery] string ipAddress,
        CancellationToken cancellationToken)
    {
        var result = await _deviceControl.GetInputsAsync(ipAddress, cancellationToken);
        return Ok(result);
    }

    /// <summary>Sends the power-on command to the device.</summary>
    [HttpPost("power-on")]
    public async Task<IActionResult> PowerOn(DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        await _deviceControl.PowerOnAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Selects an input source on the device.</summary>
    [HttpPost("input")]
    public async Task<IActionResult> SetInput(SetInputRequest request, CancellationToken cancellationToken)
    {
        await _deviceControl.SetInputAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Toggles the device power and returns the new state.</summary>
    [HttpPost("power/toggle")]
    public async Task<ActionResult<DeviceStateResponse>> PowerToggle(
        DeviceCommandRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _deviceControl.PowerToggleAsync(request.IpAddress, cancellationToken);
        return Ok(result);
    }

    /// <summary>Raises the device volume.</summary>
    [HttpPost("volume/up")]
    public async Task<IActionResult> VolumeUp(DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        await _deviceControl.VolumeUpAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Lowers the device volume.</summary>
    [HttpPost("volume/down")]
    public async Task<IActionResult> VolumeDown(DeviceCommandRequest request, CancellationToken cancellationToken)
    {
        await _deviceControl.VolumeDownAsync(request, cancellationToken);
        return NoContent();
    }
}
