using Onkyo.eISCP;
using Onkyo.eISCP.Commands;
using OnkyoIn.Control.Domain.Gateways;

namespace OnkyoIn.Control.Infrastructure.Onkyo;

/// <summary>
/// Infrastructure adapter that talks to real Onkyo devices using the
/// Onkyo.eISCP library. Implements the <see cref="IOnkyoDeviceGateway"/> port.
/// </summary>
public class OnkyoDeviceGateway : IOnkyoDeviceGateway
{
    private const int DefaultPort = 60128;

    public async Task<string> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var receivers = await ISCPConnection.DiscoverAsync();

        var first = receivers.FirstOrDefault()
            ?? throw new InvalidOperationException("No Onkyo device was found on the network.");

        return first.IPAddress.ToString();
    }

    public async Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        using var receiver = new Receiver();
        await receiver.ConnectAsync(ipAddress, DefaultPort);
        await receiver.PowerOnAsync(Zone.Main);
    }

    public async Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        using var receiver = new Receiver();
        await receiver.ConnectAsync(ipAddress, DefaultPort);
        await receiver.SetVolumeUpAsync(Zone.Main);
    }

    public async Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        using var receiver = new Receiver();
        await receiver.ConnectAsync(ipAddress, DefaultPort);
        await receiver.SetVolumeDownAsync(Zone.Main);
    }
}
