using OnkyoIn.Web.Application.Dtos;

namespace OnkyoIn.Web.Domain.Gateways;

/// <summary>
/// Port that abstracts calls to the OnkyoIn.Control HTTP API.
/// Implemented in the Infrastructure layer with HttpClient.
/// </summary>
public interface IOnkyoControlClient
{
    Task<string> DiscoverAsync(CancellationToken cancellationToken = default);

    Task<DeviceState> GetStateAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InputSourceResponse>> GetInputsAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task<DeviceState> PowerToggleAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task SetInputAsync(string ipAddress, string inputId, CancellationToken cancellationToken = default);

    Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task<TunerState?> GetTunerAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task<TunerState> SetPresetAsync(string ipAddress, int preset, CancellationToken cancellationToken = default);

    Task<TunerState> PresetUpAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task<TunerState> PresetDownAsync(string ipAddress, CancellationToken cancellationToken = default);
}

/// <summary>Read-only snapshot of a device's state as returned by the control API.</summary>
public record DeviceState(
    string IpAddress,
    bool IsPoweredOn,
    int Volume,
    string? CurrentInputId = null,
    string? CurrentInputName = null);

/// <summary>Read-only snapshot of a device's tuner state as returned by the control API.</summary>
public record TunerState(string? Band, int? Preset);
