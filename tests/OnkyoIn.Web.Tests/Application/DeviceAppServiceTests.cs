using OnkyoIn.Web.Application.Dtos;
using OnkyoIn.Web.Application.Services;
using OnkyoIn.Web.Domain.Gateways;

namespace OnkyoIn.Web.Tests.Application;

public class DeviceAppServiceTests
{
    private sealed class FakeControlClient : IOnkyoControlClient
    {
        public string? DiscoveredIp { get; set; } = "192.168.1.50";
        public DeviceState State { get; set; } = new("192.168.1.50", true, 20);
        public string? LastIp { get; private set; }
        public string? LastCommand { get; private set; }

        public Task<string> DiscoverAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(DiscoveredIp!);

        public Task<DeviceState> GetStateAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "get-state";
            return Task.FromResult(State);
        }

        public Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "power-on";
            return Task.CompletedTask;
        }

        public Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "volume-up";
            return Task.CompletedTask;
        }

        public Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "volume-down";
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsClientIp()
    {
        var service = new DeviceAppService(new FakeControlClient { DiscoveredIp = "10.0.0.7" });

        var result = await service.DiscoverAsync();

        Assert.Equal("10.0.0.7", result.IpAddress);
    }

    [Fact]
    public async Task GetStateAsync_ReturnsStateFromControlApi()
    {
        var client = new FakeControlClient { State = new DeviceState("10.0.0.9", false, 42) };
        var service = new DeviceAppService(client);

        var result = await service.GetStateAsync("10.0.0.9");

        Assert.Equal("10.0.0.9", result.IpAddress);
        Assert.False(result.IsPoweredOn);
        Assert.Equal(42, result.Volume);
    }

    [Fact]
    public async Task PowerOnAsync_ForwardsIpToControlApi()
    {
        var client = new FakeControlClient();
        var service = new DeviceAppService(client);

        await service.PowerOnAsync(new DeviceCommandRequest("10.0.0.9"));

        Assert.Equal("10.0.0.9", client.LastIp);
        Assert.Equal("power-on", client.LastCommand);
    }

    [Fact]
    public async Task VolumeUpAsync_ForwardsIpToControlApi()
    {
        var client = new FakeControlClient();
        var service = new DeviceAppService(client);

        await service.VolumeUpAsync(new DeviceCommandRequest("10.0.0.9"));

        Assert.Equal("volume-up", client.LastCommand);
    }

    [Fact]
    public async Task VolumeDownAsync_ForwardsIpToControlApi()
    {
        var client = new FakeControlClient();
        var service = new DeviceAppService(client);

        await service.VolumeDownAsync(new DeviceCommandRequest("10.0.0.9"));

        Assert.Equal("volume-down", client.LastCommand);
    }
}
