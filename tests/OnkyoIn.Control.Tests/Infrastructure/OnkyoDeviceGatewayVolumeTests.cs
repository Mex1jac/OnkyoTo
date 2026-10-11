using System.Net;
using System.Net.Sockets;
using System.Text;
using OnkyoIn.Control.Infrastructure.Onkyo;

namespace OnkyoIn.Control.Tests.Infrastructure;

public class OnkyoDeviceGatewayVolumeTests
{
    [Fact]
    public async Task VolumeUpAsync_SendsMvlUpCommand()
    {
        var command = await RunAgainstFakeReceiverAsync(
            replyPayload: "MVL29",
            action: gateway => gateway.VolumeUpAsync("127.0.0.1"));

        Assert.Contains("MVLUP", command);
    }

    [Fact]
    public async Task VolumeDownAsync_SendsMvlDownCommand()
    {
        var command = await RunAgainstFakeReceiverAsync(
            replyPayload: "MVL28",
            action: gateway => gateway.VolumeDownAsync("127.0.0.1"));

        Assert.Contains("MVLDOWN", command);
    }

    /// <summary>
    /// Starts a fake eISCP receiver on an ephemeral port, runs the
    /// action against it and returns the raw command it received.
    /// </summary>
    private static async Task<string> RunAgainstFakeReceiverAsync(
        string replyPayload,
        Func<OnkyoDeviceGateway, Task> action)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint!).Port;

        var receivedCommand = new TaskCompletionSource<string>();
        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

            var buffer = new byte[256];
            var read = await stream.ReadAsync(buffer);
            receivedCommand.SetResult(Encoding.ASCII.GetString(buffer, 0, read));

            await stream.WriteAsync(BuildReply(replyPayload));
        });

        var gateway = new OnkyoDeviceGateway(port);
        await action(gateway);
        await serverTask;

        return await receivedCommand.Task;
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
