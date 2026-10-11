namespace OnkyoIn.Control.Domain.Entities;

/// <summary>
/// Read-only snapshot of an Onkyo device's state.
/// </summary>
/// <param name="IsPoweredOn">Whether the device is powered on.</param>
/// <param name="Volume">Current master volume level (0-100).</param>
/// <param name="CurrentInputId">Identifier of the selected input source, if known.</param>
/// <param name="CurrentInputName">Name of the selected input source, if known.</param>
public record DeviceState(
    bool IsPoweredOn,
    int Volume,
    string? CurrentInputId = null,
    string? CurrentInputName = null);
