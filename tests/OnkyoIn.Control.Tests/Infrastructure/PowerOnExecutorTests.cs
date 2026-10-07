using OnkyoIn.Control.Infrastructure.Onkyo;

namespace OnkyoIn.Control.Tests.Infrastructure;

public class PowerOnExecutorTests
{
    [Fact]
    public async Task WhenCommandSucceeds_DoesNotCheckState()
    {
        var stateChecked = false;

        await PowerOnExecutor.ExecuteAsync(
            sendPowerOn: () => Task.CompletedTask,
            isPoweredOn: () => { stateChecked = true; return Task.FromResult(false); });

        Assert.False(stateChecked);
    }

    [Fact]
    public async Task WhenTimeoutButDeviceIsOn_DoesNotThrow()
    {
        // Simulates a receiver that powered on but acknowledged too late.
        await PowerOnExecutor.ExecuteAsync(
            sendPowerOn: () => throw new TimeoutException("ack too late"),
            isPoweredOn: () => Task.FromResult(true));
    }

    [Fact]
    public async Task WhenTimeoutAndDeviceIsOff_Rethrows()
    {
        await Assert.ThrowsAsync<TimeoutException>(() =>
            PowerOnExecutor.ExecuteAsync(
                sendPowerOn: () => throw new TimeoutException("ack too late"),
                isPoweredOn: () => Task.FromResult(false)));
    }

    [Fact]
    public async Task WhenNonTimeoutError_PropagatesWithoutCheckingState()
    {
        var stateChecked = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PowerOnExecutor.ExecuteAsync(
                sendPowerOn: () => throw new InvalidOperationException("boom"),
                isPoweredOn: () => { stateChecked = true; return Task.FromResult(true); }));

        Assert.False(stateChecked);
    }
}
