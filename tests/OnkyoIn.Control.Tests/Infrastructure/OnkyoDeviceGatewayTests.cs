using OnkyoIn.Control.Domain.Exceptions;
using OnkyoIn.Control.Infrastructure.Onkyo;

namespace OnkyoIn.Control.Tests.Infrastructure;

public class OnkyoDeviceGatewayTests
{
    [Fact]
    public async Task PowerOnAsync_WithEmptyIp_ThrowsArgumentException()
    {
        var gateway = new OnkyoDeviceGateway();

        await Assert.ThrowsAsync<ArgumentException>(
            () => gateway.PowerOnAsync("  "));
    }

    [Fact]
    public async Task PowerOnAsync_WithUnreachableIp_ThrowsDeviceCommunicationException()
    {
        var gateway = new OnkyoDeviceGateway();

        // Localhost has no eISCP listener, so the connection is refused immediately.
        var exception = await Assert.ThrowsAsync<DeviceCommunicationException>(
            () => gateway.PowerOnAsync("127.0.0.1"));

        Assert.Contains("127.0.0.1", exception.Message);
    }

    [Fact]
    public async Task VolumeUpAsync_WithUnreachableIp_ThrowsDeviceCommunicationException()
    {
        var gateway = new OnkyoDeviceGateway();

        await Assert.ThrowsAsync<DeviceCommunicationException>(
            () => gateway.VolumeUpAsync("127.0.0.1"));
    }
}
