namespace OnkyoIn.Control.Application.Dtos;

/// <summary>Request to send a command to a device by IP.</summary>
public record DeviceCommandRequest(string IpAddress);

/// <summary>Current state of a device.</summary>
public record DeviceStateResponse(string IpAddress, bool IsPoweredOn, int Volume);

/// <summary>Result of a device discovery.</summary>
public record DiscoverResponse(string IpAddress);
