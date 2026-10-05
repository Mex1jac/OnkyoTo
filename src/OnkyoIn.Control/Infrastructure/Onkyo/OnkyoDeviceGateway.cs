using Onkyo.eISCP;
using Onkyo.eISCP.Commands;
using OnkyoIn.Control.Domain.Entities;
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

    public async Task<DeviceState> GetStateAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var (power, volume) = await SendAsync(ipAddress, async receiver =>
        {
            var powerStatus = await receiver.GetPowerStatusAsync(Zone.Main);
            var volumeStatus = await receiver.GetVolumeAsync(Zone.Main);
            return (powerStatus, volumeStatus);
        });

        // The library returns -1 for the volume when the value is "N/A".
        var volumeLevel = volume.VolumeLevel < 0 ? 0 : volume.VolumeLevel;

        return new DeviceState(power.SystemOn, volumeLevel);
    }

    public Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, async receiver => { await receiver.PowerOnAsync(Zone.Main); return true; });

    public Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, async receiver => { await receiver.SetVolumeUpAsync(Zone.Main); return true; });

    public Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, async receiver => { await receiver.SetVolumeDownAsync(Zone.Main); return true; });

    private static Task SendAsync(string ipAddress, Func<Receiver, Task> command)
        => SendAsync(ipAddress, async receiver => { await command(receiver); return true; });

    private static async Task<T> SendAsync<T>(string ipAddress, Func<Receiver, Task<T>> action)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("The device IP address is required.", nameof(ipAddress));
        }

        try
        {
            using var receiver = new Receiver();
            await receiver.ConnectAsync(ipAddress, DefaultPort);
            return await action(receiver);
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
