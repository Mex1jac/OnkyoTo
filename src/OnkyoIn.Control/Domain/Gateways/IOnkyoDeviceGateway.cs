using OnkyoIn.Control.Domain.Entities;

namespace OnkyoIn.Control.Domain.Gateways;

/// <summary>
/// Port that abstracts communication with an Onkyo device.
/// Implemented in the Infrastructure layer.
/// </summary>
public interface IOnkyoDeviceGateway
{
    /// <summary>Discovers an Onkyo device on the local network and returns its IP address.</summary>
    Task<string> DiscoverAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the current power and volume state of a device.</summary>
    Task<DeviceState> GetStateAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default);
}
