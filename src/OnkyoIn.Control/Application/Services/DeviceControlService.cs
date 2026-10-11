using OnkyoIn.Control.Application.Dtos;
using OnkyoIn.Control.Domain.Entities;
using OnkyoIn.Control.Domain.Gateways;

namespace OnkyoIn.Control.Application.Services;

/// <summary>
/// Application service (use cases) that coordinates device control commands.
/// </summary>
public class DeviceControlService
{
    private readonly IOnkyoDeviceGateway _gateway;

    public DeviceControlService(IOnkyoDeviceGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DiscoverResponse> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var ipAddress = await _gateway.DiscoverAsync(cancellationToken);
        return new DiscoverResponse(ipAddress);
    }

    public async Task<DeviceStateResponse> GetStateAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var state = await _gateway.GetStateAsync(ipAddress, cancellationToken);
        return new DeviceStateResponse(
            ipAddress, state.IsPoweredOn, state.Volume, state.CurrentInputId, state.CurrentInputName);
    }

    public async Task<IReadOnlyList<InputSourceResponse>> GetInputsAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var inputs = await _gateway.GetInputsAsync(ipAddress, cancellationToken);
        return inputs.Sources
            .Select(source => new InputSourceResponse(source.Id, source.Name, inputs.Current?.Id == source.Id))
            .ToList();
    }

    public Task PowerOnAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _gateway.PowerOnAsync(request.IpAddress, cancellationToken);

    public Task SetInputAsync(SetInputRequest request, CancellationToken cancellationToken = default)
        => _gateway.SetInputAsync(request.IpAddress, request.InputId, cancellationToken);

    public Task VolumeUpAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _gateway.VolumeUpAsync(request.IpAddress, cancellationToken);

    public Task VolumeDownAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _gateway.VolumeDownAsync(request.IpAddress, cancellationToken);
}
