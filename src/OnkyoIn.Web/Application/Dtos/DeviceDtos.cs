namespace OnkyoIn.Web.Application.Dtos;

/// <summary>Request to control a device by IP.</summary>
public record DeviceCommandRequest(string IpAddress);

/// <summary>Result of a device discovery.</summary>
public record DiscoverResponse(string IpAddress);
