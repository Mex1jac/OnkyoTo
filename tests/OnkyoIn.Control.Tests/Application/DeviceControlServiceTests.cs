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
        public DeviceState ToggleState { get; set; } = new(false, 20);
        public DeviceInputs Inputs { get; set; } = new(
            InputSourceCatalog.Sources, InputSourceCatalog.Sources.First());
        public TunerState? Tuner { get; set; } = new TunerState("fm", 10);
        public TunerState ChangedTuner { get; set; } = new TunerState("fm", 11);
        public string? LastIp { get; private set; }
        public string? LastCommand { get; private set; }
        public string? LastInputId { get; private set; }
        public int? LastPreset { get; private set; }

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

        public Task<DeviceState> PowerToggleAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "power-toggle";
            return Task.FromResult(ToggleState);
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

        public Task<TunerState?> GetTunerAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "get-tuner";
            return Task.FromResult(Tuner);
        }

        public Task<TunerState> SetPresetAsync(string ipAddress, int preset, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastPreset = preset;
            LastCommand = "set-preset";
            return Task.FromResult(ChangedTuner);
        }

        public Task<TunerState> PresetUpAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "preset-up";
            return Task.FromResult(ChangedTuner);
        }

        public Task<TunerState> PresetDownAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "preset-down";
            return Task.FromResult(ChangedTuner);
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
    public async Task PowerToggleAsync_ReturnsNewStateFromGateway()
    {
        var gateway = new FakeGateway
        {
            ToggleState = new DeviceState(true, 30, "cd", "CD"),
        };
        var service = new DeviceControlService(gateway);

        var result = await service.PowerToggleAsync("10.0.0.9");

        Assert.Equal("10.0.0.9", result.IpAddress);
        Assert.True(result.IsPoweredOn);
        Assert.Equal(30, result.Volume);
        Assert.Equal("cd", result.CurrentInputId);
        Assert.Equal("CD", result.CurrentInputName);
        Assert.Equal("power-toggle", gateway.LastCommand);
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

    [Fact]
    public async Task GetTunerAsync_ReturnsTunerFromGateway()
    {
        var gateway = new FakeGateway { Tuner = new TunerState("am", 5) };
        var service = new DeviceControlService(gateway);

        var result = await service.GetTunerAsync("10.0.0.9");

        Assert.NotNull(result);
        Assert.Equal("am", result!.Band);
        Assert.Equal(5, result!.Preset);
        Assert.Equal("get-tuner", gateway.LastCommand);
    }

    [Fact]
    public async Task GetTunerAsync_WithoutTuner_ReturnsNull()
    {
        var gateway = new FakeGateway { Tuner = null };
        var service = new DeviceControlService(gateway);

        var result = await service.GetTunerAsync("10.0.0.9");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetPresetAsync_ForwardsIpAndPreset()
    {
        var gateway = new FakeGateway();
        var service = new DeviceControlService(gateway);

        var result = await service.SetPresetAsync("10.0.0.9", 12);

        Assert.Equal("10.0.0.9", gateway.LastIp);
        Assert.Equal(12, gateway.LastPreset);
        Assert.Equal("set-preset", gateway.LastCommand);
        Assert.Equal("fm", result.Band);
        Assert.Equal(11, result.Preset);
    }

    [Fact]
    public async Task PresetUpAsync_ForwardsIpToGateway()
    {
        var gateway = new FakeGateway();
        var service = new DeviceControlService(gateway);

        await service.PresetUpAsync("10.0.0.9");

        Assert.Equal("10.0.0.9", gateway.LastIp);
        Assert.Equal("preset-up", gateway.LastCommand);
    }

    [Fact]
    public async Task PresetDownAsync_ForwardsIpToGateway()
    {
        var gateway = new FakeGateway();
        var service = new DeviceControlService(gateway);

        await service.PresetDownAsync("10.0.0.9");

        Assert.Equal("10.0.0.9", gateway.LastIp);
        Assert.Equal("preset-down", gateway.LastCommand);
    }
}
