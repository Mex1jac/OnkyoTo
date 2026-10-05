namespace OnkyoIn.Web.Domain.Gateways;

/// <summary>
/// Port that abstracts calls to the OnkyoIn.Control HTTP API.
/// Implemented in the Infrastructure layer with HttpClient.
/// </summary>
public interface IOnkyoControlClient
{
    Task<string> DiscoverAsync(CancellationToken cancellationToken = default);

    Task<DeviceState> GetStateAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default);
}

/// <summary>Read-only snapshot of a device's state as returned by the control API.</summary>
public record DeviceState(string IpAddress, bool IsPoweredOn, int Volume);
