namespace OnkyoIn.Control.Application.Dtos;

/// <summary>Request to send a command to a device by IP.</summary>
public record DeviceCommandRequest(string IpAddress);

/// <summary>Request to select an input source on a device by IP.</summary>
public record SetInputRequest(string IpAddress, string InputId);

/// <summary>Request to select a tuner preset on a device by IP.</summary>
public record TunerPresetRequest(string IpAddress, int Preset);

/// <summary>Current state of a device.</summary>
public record DeviceStateResponse(
    string IpAddress,
    bool IsPoweredOn,
    int Volume,
    string? CurrentInputId = null,
    string? CurrentInputName = null);

/// <summary>An input source, with a flag for the currently selected one.</summary>
public record InputSourceResponse(string Id, string Name, bool IsCurrent);

/// <summary>Tuner state of a device (band and preset).</summary>
public record TunerStateResponse(string? Band, int? Preset);

/// <summary>Result of a device discovery.</summary>
public record DiscoverResponse(string IpAddress);
