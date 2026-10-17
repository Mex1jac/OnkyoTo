using System.Net;
using System.Net.Sockets;
using System.Text;
using OnkyoIn.Control.Domain.Entities;
using OnkyoIn.Control.Domain.Exceptions;
using OnkyoIn.Control.Infrastructure.Onkyo;

namespace OnkyoIn.Control.Tests.Infrastructure;

public class OnkyoDeviceGatewayTunerTests
{
    [Fact]
    public async Task SetPresetAsync_SendsPrsCommandAndVerifies()
    {
        var commands = await RunAgainstFakeReceiverAsync(
            inputCode: "24",
            preset: 10,
            action: gateway => gateway.SetPresetAsync("127.0.0.1", 10));

        Assert.Contains("PRS0A", commands);
        Assert.Contains("SLIQSTN", commands);
        Assert.Contains("PRSQSTN", commands);
    }

    [Fact]
    public async Task SetPresetAsync_WithFmInput_ReturnsFmBandAndPreset()
    {
        TunerState? tuner = null;
        await RunAgainstFakeReceiverAsync(
            inputCode: "24",
            preset: 10,
            action: async gateway => tuner = await gateway.SetPresetAsync("127.0.0.1", 10));

        Assert.NotNull(tuner);
        Assert.Equal("fm", tuner!.Band);
        Assert.Equal(10, tuner!.Preset);
    }

    [Fact]
    public async Task SetPresetAsync_WithAmInput_ReturnsAmBand()
    {
        TunerState? tuner = null;
        await RunAgainstFakeReceiverAsync(
            inputCode: "25",
            preset: 10,
            action: async gateway => tuner = await gateway.SetPresetAsync("127.0.0.1", 10));

        Assert.Equal("am", tuner!.Band);
        Assert.Equal(10, tuner!.Preset);
    }

    [Fact]
    public async Task SetPresetAsync_WithPresetBelowRange_ThrowsArgumentException()
    {
        var gateway = new OnkyoDeviceGateway();

        await Assert.ThrowsAsync<ArgumentException>(
            () => gateway.SetPresetAsync("127.0.0.1", 0));
    }

    [Fact]
    public async Task SetPresetAsync_WithPresetAboveRange_ThrowsArgumentException()
    {
        var gateway = new OnkyoDeviceGateway();

        await Assert.ThrowsAsync<ArgumentException>(
            () => gateway.SetPresetAsync("127.0.0.1", 41));
    }

    [Fact]
    public async Task SetPresetAsync_WithUnreachableIp_ThrowsDeviceCommunicationException()
    {
        var gateway = new OnkyoDeviceGateway();

        // Localhost has no eISCP listener, so the connection is refused immediately.
        var exception = await Assert.ThrowsAsync<DeviceCommunicationException>(
            () => gateway.SetPresetAsync("127.0.0.1", 10));

        Assert.Contains("127.0.0.1", exception.Message);
    }

    [Fact]
    public async Task PresetUpAsync_SendsPrsUpCommand()
    {
        var commands = await RunAgainstFakeReceiverAsync(
            inputCode: "24",
            preset: 10,
            action: gateway => gateway.PresetUpAsync("127.0.0.1"));

        Assert.Contains("PRSUP", commands);
        Assert.Contains("PRSQSTN", commands);
    }

    [Fact]
    public async Task PresetDownAsync_SendsPrsDownCommand()
    {
        var commands = await RunAgainstFakeReceiverAsync(
            inputCode: "24",
            preset: 10,
            action: gateway => gateway.PresetDownAsync("127.0.0.1"));

        Assert.Contains("PRSDOWN", commands);
        Assert.Contains("PRSQSTN", commands);
    }

    [Fact]
    public async Task GetTunerAsync_WithFmInput_ReturnsBandAndPreset()
    {
        TunerState? tuner = null;
        await RunAgainstFakeReceiverAsync(
            inputCode: "24",
            preset: 10,
            action: async gateway => tuner = await gateway.GetTunerAsync("127.0.0.1"));

        Assert.NotNull(tuner);
        Assert.Equal("fm", tuner!.Band);
        Assert.Equal(10, tuner!.Preset);
    }

    [Fact]
    public async Task GetTunerAsync_WithNonTunerInput_ReturnsNull()
    {
        TunerState? tuner = new TunerState("fm", 1);
        await RunAgainstFakeReceiverAsync(
            inputCode: "10",
            preset: 10,
            action: async gateway => tuner = await gateway.GetTunerAsync("127.0.0.1"));

        Assert.Null(tuner);
    }

    [Fact]
    public async Task GetTunerAsync_WithEmptyIp_ThrowsArgumentException()
    {
        var gateway = new OnkyoDeviceGateway();

        await Assert.ThrowsAsync<ArgumentException>(
            () => gateway.GetTunerAsync("  "));
    }

    /// <summary>
    /// Starts a fake eISCP receiver on an ephemeral port, runs the
    /// action against it and returns every raw command it received.
    /// The fake answers SLIQSTN with the given input and every PRS
    /// command with the given preset, so the gateway's verification
    /// queries succeed.
    /// </summary>
    private static async Task<IReadOnlyList<string>> RunAgainstFakeReceiverAsync(
        string inputCode,
        int preset,
        Func<OnkyoDeviceGateway, Task> action)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint!).Port;

        var receivedCommands = new List<string>();
        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

            // The gateway sends several commands on the same connection;
            // answer each one until the client disconnects.
            var buffer = new byte[256];
            while (true)
            {
                int read;
                try
                {
                    read = await stream.ReadAsync(buffer);
                }
                catch (IOException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (read == 0)
                {
                    break;
                }

                var command = ExtractCommand(Encoding.ASCII.GetString(buffer, 0, read));
                if (command is null)
                {
                    continue;
                }

                receivedCommands.Add(command);

                var reply = command == "SLIQSTN"
                    ? $"SLI{inputCode}"
                    : $"PRS{preset:X2}";
                await stream.WriteAsync(BuildReply(reply));
            }
        });

        var gateway = new OnkyoDeviceGateway(port);
        await action(gateway);
        await serverTask;

        return receivedCommands;
    }

    // Reads the command ("SLIQSTN", "PRS0A", ...) from a raw eISCP frame.
    private static string? ExtractCommand(string frame)
    {
        const int headerSize = 16;
        if (frame.Length <= headerSize + 2)
        {
            return null;
        }

        return frame
            .Substring(headerSize + 2) // skip the 16-byte header and "!1"
            .TrimEnd((char)0x0d, (char)0x0a, (char)0x1a);
    }

    // Builds a minimal eISCP frame: 16-byte header + "!1" + payload.
    private static byte[] BuildReply(string payload)
    {
        var payloadBytes = Encoding.ASCII.GetBytes($"!1{payload}");
        var header = new byte[]
        {
            0x49, 0x53, 0x43, 0x50, // "ISCP"
            0x00, 0x00, 0x00, 0x10,
            0x00, 0x00, 0x00, (byte)payloadBytes.Length,
            0x01, 0x00, 0x00, 0x00,
        };
        return header.Concat(payloadBytes).ToArray();
    }
}
