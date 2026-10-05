using OnkyoIn.Control.Domain.Entities;

namespace OnkyoIn.Control.Tests.Domain;

public class OnkyoDeviceTests
{
    [Fact]
    public void Constructor_WithEmptyIp_Throws()
    {
        Assert.Throws<ArgumentException>(() => new OnkyoDevice(""));
    }

    [Fact]
    public void PowerOn_SetsIsPoweredOnTrue()
    {
        var device = new OnkyoDevice("192.168.1.100");

        device.PowerOn();

        Assert.True(device.IsPoweredOn);
    }

    [Fact]
    public void VolumeUp_IncreasesVolume()
    {
        var device = new OnkyoDevice("192.168.1.100", volume: 10);

        device.VolumeUp();

        Assert.Equal(11, device.Volume);
    }

    [Fact]
    public void VolumeDown_DecreasesVolume()
    {
        var device = new OnkyoDevice("192.168.1.100", volume: 10);

        device.VolumeDown();

        Assert.Equal(9, device.Volume);
    }

    [Fact]
    public void VolumeUp_DoesNotExceedMaxVolume()
    {
        var device = new OnkyoDevice("192.168.1.100", volume: OnkyoDevice.MaxVolume);

        device.VolumeUp();

        Assert.Equal(OnkyoDevice.MaxVolume, device.Volume);
    }

    [Fact]
    public void VolumeDown_DoesNotGoBelowMinVolume()
    {
        var device = new OnkyoDevice("192.168.1.100", volume: OnkyoDevice.MinVolume);

        device.VolumeDown();

        Assert.Equal(OnkyoDevice.MinVolume, device.Volume);
    }
}
