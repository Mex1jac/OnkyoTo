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
        public DeviceState ToggleState { get; set; } = new("192.168.1.50", false, 20);
        public IReadOnlyList<InputSourceResponse> Inputs { get; set; } =
            Array.Empty<InputSourceResponse>();
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

        public Task<IReadOnlyList<InputSourceResponse>> GetInputsAsync(
            string ipAddress, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastCommand = "get-inputs";
            return Task.FromResult(Inputs);
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

        public Task SetInputAsync(string ipAddress, string inputId, CancellationToken cancellationToken = default)
        {
            LastIp = ipAddress;
            LastInputId = inputId;
            LastCommand = "set-input";
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
    public async Task GetStateAsync_ReturnsCurrentInput()
    {
        var client = new FakeControlClient
        {
            State = new DeviceState("10.0.0.9", true, 20, "cd", "CD"),
        };
        var service = new DeviceAppService(client);

        var result = await service.GetStateAsync("10.0.0.9");

        Assert.Equal("cd", result.CurrentInputId);
        Assert.Equal("CD", result.CurrentInputName);
    }

    [Fact]
    public async Task GetInputsAsync_ReturnsSourcesFromControlApi()
    {
        var sources = new[]
        {
            new InputSourceResponse("cd", "CD", true),
            new InputSourceResponse("fm", "FM Tuner", false),
        };
        var service = new DeviceAppService(new FakeControlClient { Inputs = sources });

        var result = await service.GetInputsAsync("10.0.0.9");

        Assert.Equal(2, result.Count);
        Assert.Equal("cd", result[0].Id);
        Assert.True(result[0].IsCurrent);
        Assert.False(result[1].IsCurrent);
    }

    [Fact]
    public async Task SetInputAsync_ForwardsIpAndInputId()
    {
        var client = new FakeControlClient();
        var service = new DeviceAppService(client);

        await service.SetInputAsync(new SetInputRequest("10.0.0.9", "network"));

        Assert.Equal("10.0.0.9", client.LastIp);
        Assert.Equal("network", client.LastInputId);
        Assert.Equal("set-input", client.LastCommand);
    }

    [Fact]
    public async Task PowerToggleAsync_ReturnsNewStateFromControlApi()
    {
        var client = new FakeControlClient
        {
            ToggleState = new DeviceState("10.0.0.9", true, 25, "tv", "TV"),
        };
        var service = new DeviceAppService(client);

        var result = await service.PowerToggleAsync("10.0.0.9");

        Assert.Equal("10.0.0.9", result.IpAddress);
        Assert.True(result.IsPoweredOn);
        Assert.Equal(25, result.Volume);
        Assert.Equal("tv", result.CurrentInputId);
        Assert.Equal("power-toggle", client.LastCommand);
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

    [Fact]
    public async Task GetTunerAsync_ReturnsTunerFromControlApi()
    {
        var client = new FakeControlClient { Tuner = new TunerState("am", 5) };
        var service = new DeviceAppService(client);

        var result = await service.GetTunerAsync("10.0.0.9");

        Assert.NotNull(result);
        Assert.Equal("am", result!.Band);
        Assert.Equal(5, result!.Preset);
        Assert.Equal("get-tuner", client.LastCommand);
    }

    [Fact]
    public async Task GetTunerAsync_WithoutTuner_ReturnsNull()
    {
        var client = new FakeControlClient { Tuner = null };
        var service = new DeviceAppService(client);

        var result = await service.GetTunerAsync("10.0.0.9");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetPresetAsync_ForwardsIpAndPreset()
    {
        var client = new FakeControlClient();
        var service = new DeviceAppService(client);

        var result = await service.SetPresetAsync("10.0.0.9", 12);

        Assert.Equal("10.0.0.9", client.LastIp);
        Assert.Equal(12, client.LastPreset);
        Assert.Equal("set-preset", client.LastCommand);
        Assert.Equal("fm", result.Band);
        Assert.Equal(11, result.Preset);
    }

    [Fact]
    public async Task PresetUpAsync_ForwardsIpToControlApi()
    {
        var client = new FakeControlClient();
        var service = new DeviceAppService(client);

        await service.PresetUpAsync("10.0.0.9");

        Assert.Equal("10.0.0.9", client.LastIp);
        Assert.Equal("preset-up", client.LastCommand);
    }

    [Fact]
    public async Task PresetDownAsync_ForwardsIpToControlApi()
    {
        var client = new FakeControlClient();
        var service = new DeviceAppService(client);

        await service.PresetDownAsync("10.0.0.9");

        Assert.Equal("10.0.0.9", client.LastIp);
        Assert.Equal("preset-down", client.LastCommand);
    }
}
