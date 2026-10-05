namespace OnkyoIn.Control.Domain.Entities;

/// <summary>
/// Read-only snapshot of an Onkyo device's state.
/// </summary>
public record DeviceState(bool IsPoweredOn, int Volume);
