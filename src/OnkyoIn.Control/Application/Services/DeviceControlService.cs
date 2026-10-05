using OnkyoIn.Control.Application.Dtos;
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

    public Task PowerOnAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _gateway.PowerOnAsync(request.IpAddress, cancellationToken);

    public Task VolumeUpAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _gateway.VolumeUpAsync(request.IpAddress, cancellationToken);

    public Task VolumeDownAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _gateway.VolumeDownAsync(request.IpAddress, cancellationToken);
}
