using OnkyoIn.Control.Application.Dtos;
using OnkyoIn.Control.Application.Services;
using OnkyoIn.Control.Domain.Entities;
using OnkyoIn.Control.Domain.Gateways;

namespace OnkyoIn.Control.Tests.Application;

public class DeviceControlServiceTests
{
    private sealed class FakeGateway : IOnkyoDeviceGateway
    {
        public string? DiscoveredIp { get; set; } = "192.168.1.50";
        public DeviceState State { get; set; } = new(true, 20);
        public DeviceInputs Inputs { get; set; } = new(
            InputSourceCatalog.Sources, InputSourceCatalog.Sources.First());
        public string? LastIp { get; private set; }
        public string? LastCommand { get; private set; }
        public string? LastInputId { get; private set; }

        public Task<string> DiscoverAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(DiscoveredIp!);

        public Task<DeviceState> GetStateAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "get-state";
            return Task.FromResult(State);
        }

        public Task<DeviceInputs> GetInputsAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "get-inputs";
            return Task.FromResult(Inputs);
        }

        public Task SetInputAsync(string ipAddress, string inputId, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastInputId = inputId;
            LastCommand = "set-input";
            return Task.CompletedTask;
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
    public async Task DiscoverAsync_ReturnsGatewayIp()
    {
        var service = new DeviceControlService(new FakeGateway { DiscoveredIp = "10.0.0.5" });

        var result = await service.DiscoverAsync();

        Assert.Equal("10.0.0.5", result.IpAddress);
    }

    [Fact]
    public async Task GetStateAsync_ReturnsStateFromGateway()
    {
        var gateway = new FakeGateway { State = new DeviceState(true, 33, "cd", "CD") };
        var service = new DeviceControlService(gateway);

        var result = await service.GetStateAsync("10.0.0.9");

        Assert.Equal("10.0.0.9", result.IpAddress);
        Assert.True(result.IsPoweredOn);
        Assert.Equal(33, result.Volume);
        Assert.Equal("cd", result.CurrentInputId);
        Assert.Equal("CD", result.CurrentInputName);
    }

    [Fact]
    public async Task GetInputsAsync_MarksOnlyCurrentSource()
    {
        var current = InputSourceCatalog.FindById("cd")!;
        var gateway = new FakeGateway
        {
            Inputs = new DeviceInputs(InputSourceCatalog.Sources, current),
        };
        var service = new DeviceControlService(gateway);

        var result = await service.GetInputsAsync("10.0.0.9");

        Assert.Equal(13, result.Count);
        Assert.True(result.Single(source => source.Id == "cd").IsCurrent);
        Assert.All(result.Where(source => source.Id != "cd"), source => Assert.False(source.IsCurrent));
    }

    [Fact]
    public async Task GetInputsAsync_WithoutCurrentSource_MarksNone()
    {
        var gateway = new FakeGateway
        {
            Inputs = new DeviceInputs(InputSourceCatalog.Sources, null),
        };
        var service = new DeviceControlService(gateway);

        var result = await service.GetInputsAsync("10.0.0.9");

        Assert.All(result, source => Assert.False(source.IsCurrent));
    }

    [Fact]
    public async Task SetInputAsync_ForwardsIpAndInputId()
    {
        var gateway = new FakeGateway();
        var service = new DeviceControlService(gateway);

        await service.SetInputAsync(new SetInputRequest("10.0.0.9", "fm"));

        Assert.Equal("10.0.0.9", gateway.LastIp);
        Assert.Equal("fm", gateway.LastInputId);
        Assert.Equal("set-input", gateway.LastCommand);
    }

    [Fact]
    public async Task PowerOnAsync_ForwardsIpToGateway()
    {
        var gateway = new FakeGateway();
        var service = new DeviceControlService(gateway);

        await service.PowerOnAsync(new DeviceCommandRequest("10.0.0.9"));

        Assert.Equal("10.0.0.9", gateway.LastIp);
        Assert.Equal("power-on", gateway.LastCommand);
    }

    [Fact]
    public async Task VolumeUpAsync_ForwardsIpToGateway()
    {
        var gateway = new FakeGateway();
        var service = new DeviceControlService(gateway);

        await service.VolumeUpAsync(new DeviceCommandRequest("10.0.0.9"));

        Assert.Equal("volume-up", gateway.LastCommand);
    }

    [Fact]
    public async Task VolumeDownAsync_ForwardsIpToGateway()
    {
        var gateway = new FakeGateway();
        var service = new DeviceControlService(gateway);

        await service.VolumeDownAsync(new DeviceCommandRequest("10.0.0.9"));

        Assert.Equal("volume-down", gateway.LastCommand);
    }
}
