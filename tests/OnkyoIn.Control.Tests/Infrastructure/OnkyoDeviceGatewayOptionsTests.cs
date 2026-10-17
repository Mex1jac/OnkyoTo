using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using OnkyoIn.Control.Domain.Exceptions;
using OnkyoIn.Control.Infrastructure.Configuration;
using OnkyoIn.Control.Infrastructure.Onkyo;

namespace OnkyoIn.Control.Tests.Infrastructure;

public class OnkyoDeviceGatewayOptionsTests
{
    [Fact]
    public void OnkyoOptions_DefaultsMatchTheRealDevice()
    {
        var options = new OnkyoOptions();

        // Standard eISCP port; the power-on timeout covers the ~2.5 s
        // measured on a real device coming out of standby.
        Assert.Equal(60128, options.Port);
        Assert.Equal(6000, options.PowerOnTimeoutMs);
    }

    [Fact]
    public async Task Gateway_ReadsThePortFromOptions()
    {
        var (port, receivedCommands, serverTask) = StartStateFakeAsync();

        // Same as the (int port) constructor, but through the options
        // object the application binds from configuration.
        var gateway = new OnkyoDeviceGateway(
            Options.Create(new OnkyoOptions { Port = port }));

        await gateway.GetStateAsync("127.0.0.1");
        await serverTask;

        Assert.Contains("PWRQSTN", receivedCommands);
    }

    [Fact]
    public async Task ShortPowerOnTimeout_FailsFastWhenDeviceNeverAcknowledges()
    {
        var (port, _, serverTask) = StartStateFakeAsync(replyPowerOn: false);

        var gateway = new OnkyoDeviceGateway(Options.Create(
            new OnkyoOptions { Port = port, PowerOnTimeoutMs = 300 }));

        var watch = Stopwatch.StartNew();
        await Assert.ThrowsAsync<DeviceCommunicationException>(
            () => gateway.PowerToggleAsync("127.0.0.1"));
        watch.Stop();

        await serverTask;

        // With the 6000 ms default this would take at least 6 s; the
        // configured 300 ms timeout must be honoured instead.
        Assert.True(
            watch.ElapsedMilliseconds < 5000,
            $"Power toggle took {watch.ElapsedMilliseconds} ms; the configured timeout was not applied.");
    }

    /// <summary>
    /// Starts a fake eISCP receiver on an ephemeral port that reports a
    /// powered-on device (volume 21, input BD/DVD). When
    /// <paramref name="replyPowerOn"/> is false it stays in standby, and when
    /// the gateway sends the power-on command it never acknowledges it.
    /// Returns the port, the commands received and the server task.
    /// </summary>
    private static (int Port, List<string> ReceivedCommands, Task ServerTask) StartStateFakeAsync(
        bool replyPowerOn = true)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint!).Port;

        var receivedCommands = new List<string>();
        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

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

                var reply = command switch
                {
                    "PWRQSTN" => replyPowerOn ? "PWR01" : "PWR00",
                    "MVLQSTN" => "MVL15",
                    "SLIQSTN" => "SLI10",
                    "PWR01" => null, // never acknowledged: the timeout applies
                    _ => null,
                };

                if (reply is not null)
                {
                    await stream.WriteAsync(BuildReply(reply));
                }
            }

            listener.Stop();
        });

        return (port, receivedCommands, serverTask);
    }

    // Reads the command ("PWRQSTN", "PWR01", ...) from a raw eISCP frame.
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
