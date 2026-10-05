using Onkyo.eISCP;
using Onkyo.eISCP.Commands;
using OnkyoIn.Control.Domain.Exceptions;
using OnkyoIn.Control.Domain.Gateways;

namespace OnkyoIn.Control.Infrastructure.Onkyo;

/// <summary>
/// Infrastructure adapter that talks to real Onkyo devices using the
/// Onkyo.eISCP library. Implements the <see cref="IOnkyoDeviceGateway"/> port.
/// Translates low-level library/network errors into domain exceptions.
/// </summary>
public class OnkyoDeviceGateway : IOnkyoDeviceGateway
{
    private const int DefaultPort = 60128;

    public async Task<string> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        List<ReceiverInfo> receivers;
        try
        {
            receivers = await ISCPConnection.DiscoverAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DeviceCommunicationException(
                "Failed to scan the network for Onkyo devices.", ex);
        }

        var first = receivers.FirstOrDefault();
        if (first is null)
        {
            throw new DeviceNotFoundException(
                "No Onkyo device was found on the network.");
        }

        return first.IPAddress.ToString();
    }

    public Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, receiver => receiver.PowerOnAsync(Zone.Main));

    public Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, receiver => receiver.SetVolumeUpAsync(Zone.Main));

    public Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, receiver => receiver.SetVolumeDownAsync(Zone.Main));

    private static async Task SendAsync(string ipAddress, Func<Receiver, Task> command)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("The device IP address is required.", nameof(ipAddress));
        }

        try
        {
            using var receiver = new Receiver();
            await receiver.ConnectAsync(ipAddress, DefaultPort);
            await command(receiver);
        }
        catch (TimeoutException ex)
        {
            throw new DeviceCommunicationException(
                $"The device at {ipAddress} did not respond in time.", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
                                     and not DeviceCommunicationException)
        {
            throw new DeviceCommunicationException(
                $"Could not communicate with the device at {ipAddress}.", ex);
        }
    }
}
