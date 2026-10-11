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

    /// <summary>Reads the input sources available on a device and marks the currently selected one.</summary>
    Task<DeviceInputs> GetInputsAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Selects an input source on a device by its identifier.</summary>
    Task SetInputAsync(string ipAddress, string inputId, CancellationToken cancellationToken = default);

    Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Toggles the power state of a device and returns the new state.</summary>
    Task<DeviceState> PowerToggleAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default);
}
