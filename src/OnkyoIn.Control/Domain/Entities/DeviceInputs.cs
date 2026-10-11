namespace OnkyoIn.Control.Domain.Entities;

/// <summary>
/// The input sources a device supports and the one currently selected.
/// </summary>
/// <param name="Sources">All known input sources.</param>
/// <param name="Current">The currently selected source, or <c>null</c> if unknown.</param>
public record DeviceInputs(IReadOnlyList<InputSource> Sources, InputSource? Current);
