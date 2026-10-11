namespace OnkyoIn.Control.Application.Dtos;

/// <summary>Request to send a command to a device by IP.</summary>
public record DeviceCommandRequest(string IpAddress);

/// <summary>Request to select an input source on a device by IP.</summary>
public record SetInputRequest(string IpAddress, string InputId);

/// <summary>Current state of a device.</summary>
public record DeviceStateResponse(
    string IpAddress,
    bool IsPoweredOn,
    int Volume,
    string? CurrentInputId = null,
    string? CurrentInputName = null);

/// <summary>An input source, with a flag for the currently selected one.</summary>
public record InputSourceResponse(string Id, string Name, bool IsCurrent);

/// <summary>Result of a device discovery.</summary>
public record DiscoverResponse(string IpAddress);
