namespace OnkyoIn.Control.Domain.Entities;

/// <summary>
/// Read-only snapshot of a device's tuner state.
/// </summary>
/// <param name="Band">Active tuner band ("am" or "fm"), if the input is a tuner.</param>
/// <param name="Preset">Current preset number (1-40), if known.</param>
public record TunerState(string? Band, int? Preset);
