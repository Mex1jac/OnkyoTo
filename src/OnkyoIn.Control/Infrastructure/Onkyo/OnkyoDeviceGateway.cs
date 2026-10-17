using Microsoft.Extensions.Options;
using Onkyo.eISCP;
using Onkyo.eISCP.Commands;
using OnkyoIn.Control.Domain.Entities;
using OnkyoIn.Control.Domain.Exceptions;
using OnkyoIn.Control.Domain.Gateways;
using OnkyoIn.Control.Infrastructure.Configuration;

namespace OnkyoIn.Control.Infrastructure.Onkyo;

/// <summary>
/// Infrastructure adapter that talks to real Onkyo devices using the
/// Onkyo.eISCP library. Implements the <see cref="IOnkyoDeviceGateway"/> port.
/// Translates low-level library/network errors into domain exceptions.
/// </summary>
public class OnkyoDeviceGateway : IOnkyoDeviceGateway
{
    private readonly int _port;
    private readonly int _powerOnTimeoutMs;

    /// <summary>
    /// Constructor used by dependency injection: reads the eISCP port
    /// and the power-on timeout from configuration ("Onkyo" section).
    /// </summary>
    public OnkyoDeviceGateway(IOptions<OnkyoOptions> options)
        : this(options.Value)
    {
    }

    public OnkyoDeviceGateway(OnkyoOptions options)
    {
        _port = options.Port;
        _powerOnTimeoutMs = options.PowerOnTimeoutMs;
    }

    public OnkyoDeviceGateway()
        : this(new OnkyoOptions())
    {
    }

    /// <summary>
    /// Creates the gateway. The port is injectable so tests can
    /// point it at a fake eISCP receiver.
    /// </summary>
    public OnkyoDeviceGateway(int port)
        : this(new OnkyoOptions { Port = port })
    {
    }

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
        var (power, volume, input) = await SendAsync(ipAddress, async receiver =>
        {
            var powerStatus = await receiver.GetPowerStatusAsync(Zone.Main);
            var volumeStatus = await receiver.GetVolumeAsync(Zone.Main);
            var inputStatus = await TryGetInputAsync(receiver);
            return (powerStatus, volumeStatus, inputStatus);
        });

        // The library returns -1 for the volume when the value is "N/A".
        var volumeLevel = volume.VolumeLevel < 0 ? 0 : volume.VolumeLevel;
        var currentInput = InputSourceCatalog.FindBySliCode(input?.RawData);

        return new DeviceState(
            power.SystemOn,
            volumeLevel,
            currentInput?.Id,
            currentInput?.Name);
    }

    public async Task<DeviceInputs> GetInputsAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var current = await SendAsync(
            ipAddress,
            receiver => receiver.GetInputAsync(Zone.Main));

        var sources = new List<InputSource>(InputSourceCatalog.Sources);
        var currentSource = InputSourceCatalog.FindBySliCode(current.RawData);

        // The receiver reported an input that is not part of the known
        // catalog (e.g. an HDMI input). Include it so the UI can still
        // show which source is active.
        if (currentSource is null && !string.IsNullOrEmpty(current.RawData))
        {
            currentSource = new InputSource(
                $"unknown-{current.RawData}",
                $"Unknown ({current.RawData})",
                current.RawData);
            sources.Add(currentSource);
        }

        return new DeviceInputs(sources, currentSource);
    }

    public Task SetInputAsync(string ipAddress, string inputId, CancellationToken cancellationToken = default)
    {
        var source = InputSourceCatalog.FindById(inputId)
            ?? throw new ArgumentException($"Unknown input source '{inputId}'.", nameof(inputId));

        return SendAsync(ipAddress, async receiver =>
        {
            try
            {
                await receiver.SendCommandAsync<Input>(
                    new Input(Zone.Main) { RawData = source.SliCode });
            }
            catch (TimeoutException)
            {
                // Some receivers acknowledge the switch with a delay; the
                // real input is verified below before surfacing an error
                // (same policy as power-on).
            }

            var current = await receiver.GetInputAsync(Zone.Main);
            if (!string.Equals(current.RawData, source.SliCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new DeviceCommunicationException(
                    $"The device did not switch to '{source.Name}'.");
            }
        });
    }

    public Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, async receiver =>
        {
            await PowerOnExecutor.ExecuteAsync(
                sendPowerOn: () => receiver.SendCommandAsync(
                    new Power(Zone.Main) { SystemOn = true },
                    _powerOnTimeoutMs),
                isPoweredOn: async () =>
                    (await receiver.GetPowerStatusAsync(Zone.Main))?.SystemOn == true);

            return true;
        });

    public async Task<DeviceState> PowerToggleAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        await SendAsync(ipAddress, async receiver =>
        {
            var powerStatus = await receiver.GetPowerStatusAsync(Zone.Main);

            if (powerStatus?.SystemOn == true)
            {
                // Receivers acknowledge standby immediately.
                await receiver.SendCommandAsync(
                    new Power(Zone.Main) { SystemOn = false },
                    _powerOnTimeoutMs);
            }
            else
            {
                // Power-on can take seconds to be acknowledged; verify
                // the real state before surfacing an error.
                await PowerOnExecutor.ExecuteAsync(
                    sendPowerOn: () => receiver.SendCommandAsync(
                        new Power(Zone.Main) { SystemOn = true },
                        _powerOnTimeoutMs),
                    isPoweredOn: async () =>
                        (await receiver.GetPowerStatusAsync(Zone.Main))?.SystemOn == true);
            }

            return true;
        });

        // Return the fresh state so the UI reflects the change.
        return await GetStateAsync(ipAddress, cancellationToken);
    }

    public Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, async receiver =>
        {
            // The library's SetVolumeUpAsync builds "MVL00" (its Volume
            // command always renders the absolute level and ignores
            // RawData), which mutes the receiver instead of stepping
            // up. Send the raw eISCP command until the library is fixed.
            await receiver.SendCommandAsync("MVLUP");
            return true;
        });

    public Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, async receiver =>
        {
            // Same library issue as volume-up: send the raw command.
            await receiver.SendCommandAsync("MVLDOWN");
            return true;
        });

    public async Task<TunerState?> GetTunerAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var (band, preset) = await SendAsync<(string? Band, int? Preset)>(
            ipAddress,
            async receiver =>
            {
                var inputStatus = await receiver.GetInputAsync(Zone.Main);
                var current = InputSourceCatalog.FindBySliCode(inputStatus.RawData);

                // The tuner state only applies when the active input is AM or FM.
                if (current?.Id is not ("am" or "fm"))
                {
                    return (Band: null, Preset: null);
                }

                return (Band: current.Id, Preset: await TryGetPresetAsync(receiver));
            });

        return band is null ? null : new TunerState(band, preset);
    }

    public Task<TunerState> SetPresetAsync(string ipAddress, int preset, CancellationToken cancellationToken = default)
    {
        if (preset is < 1 or > 40)
        {
            throw new ArgumentException(
                "The preset must be between 1 and 40.", nameof(preset));
        }

        return ChangePresetAsync(ipAddress, $"PRS{preset:X2}", expectedPreset: preset);
    }

    public Task<TunerState> PresetUpAsync(string ipAddress, CancellationToken cancellationToken = default)
        => ChangePresetAsync(ipAddress, "PRSUP", expectedPreset: null);

    public Task<TunerState> PresetDownAsync(string ipAddress, CancellationToken cancellationToken = default)
        => ChangePresetAsync(ipAddress, "PRSDOWN", expectedPreset: null);

    /// <summary>
    /// Sends a tuner preset command (raw eISCP, the library has no
    /// tuner commands) and verifies the result by querying the
    /// current preset before returning.
    /// </summary>
    private async Task<TunerState> ChangePresetAsync(
        string ipAddress, string command, int? expectedPreset)
    {
        var (band, preset) = await SendAsync(
            ipAddress,
            async receiver =>
            {
                await receiver.SendCommandAsync(command);

                var inputStatus = await receiver.GetInputAsync(Zone.Main);
                var current = InputSourceCatalog.FindBySliCode(inputStatus.RawData);
                var tunerPreset = await TryGetPresetAsync(receiver);
                return (Band: current?.Id, Preset: tunerPreset);
            });

        if (band is not ("am" or "fm"))
        {
            throw new DeviceCommunicationException(
                "The device is not on the AM or FM tuner.");
        }

        if (preset is null || (expectedPreset is not null && preset != expectedPreset))
        {
            throw new DeviceCommunicationException(
                "The device did not switch to the requested preset.");
        }

        return new TunerState(band, preset);
    }

    /// <summary>
    /// Reads the current tuner preset. The preset is optional
    /// information: receivers may not answer while switching.
    /// </summary>
    private static async Task<int?> TryGetPresetAsync(Receiver receiver)
    {
        try
        {
            var response = await receiver.SendCommandAsync("PRSQSTN");
            return ParsePreset(response?.RawData);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// The receiver answers PRSQSTN with the preset number in
    /// hexadecimal (RawData "0A" = preset 10).
    /// </summary>
    private static int? ParsePreset(string? rawData)
    {
        if (string.IsNullOrEmpty(rawData))
        {
            return null;
        }

        return int.TryParse(
            rawData,
            System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture,
            out var preset)
            ? preset
            : null;
    }

    // Receivers in standby often do not answer input queries; the
    // input is optional information in the state response.
    private static async Task<Input?> TryGetInputAsync(Receiver receiver)
    {
        try
        {
            return await receiver.GetInputAsync(Zone.Main);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private Task SendAsync(string ipAddress, Func<Receiver, Task> command)
        => SendAsync(ipAddress, async receiver => { await command(receiver); return true; });

    private async Task<T> SendAsync<T>(string ipAddress, Func<Receiver, Task<T>> action)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("The device IP address is required.", nameof(ipAddress));
        }

        try
        {
            using var receiver = new Receiver();
            await receiver.ConnectAsync(ipAddress, _port);
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
