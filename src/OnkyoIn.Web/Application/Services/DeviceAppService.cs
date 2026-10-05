using OnkyoIn.Web.Application.Dtos;
using OnkyoIn.Web.Domain.Gateways;

namespace OnkyoIn.Web.Application.Services;

/// <summary>
/// Application service (use cases) that the web frontend uses.
/// Delegates to the OnkyoIn.Control API through the gateway port.
/// </summary>
public class DeviceAppService
{
    private readonly IOnkyoControlClient _controlClient;

    public DeviceAppService(IOnkyoControlClient controlClient)
    {
        _controlClient = controlClient;
    }

    public async Task<DiscoverResponse> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var ipAddress = await _controlClient.DiscoverAsync(cancellationToken);
        return new DiscoverResponse(ipAddress);
    }

    public Task<DeviceState> GetStateAsync(string ipAddress, CancellationToken cancellationToken = default)
        => _controlClient.GetStateAsync(ipAddress, cancellationToken);

    public Task PowerOnAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _controlClient.PowerOnAsync(request.IpAddress, cancellationToken);

    public Task VolumeUpAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _controlClient.VolumeUpAsync(request.IpAddress, cancellationToken);

    public Task VolumeDownAsync(DeviceCommandRequest request, CancellationToken cancellationToken = default)
        => _controlClient.VolumeDownAsync(request.IpAddress, cancellationToken);
}
