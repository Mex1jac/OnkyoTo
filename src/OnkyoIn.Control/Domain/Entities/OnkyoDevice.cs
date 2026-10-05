namespace OnkyoIn.Control.Domain.Entities;

/// <summary>
/// Represents an Onkyo device on the network and its controllable state.
/// </summary>
public class OnkyoDevice
{
    public string IpAddress { get; private set; }
    public bool IsPoweredOn { get; private set; }
    public int Volume { get; private set; }

    public const int MinVolume = 0;
    public const int MaxVolume = 100;

    public OnkyoDevice(string ipAddress, bool isPoweredOn = false, int volume = 0)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address is required.", nameof(ipAddress));
        }

        IpAddress = ipAddress;
        IsPoweredOn = isPoweredOn;
        Volume = Math.Clamp(volume, MinVolume, MaxVolume);
    }

    public void PowerOn() => IsPoweredOn = true;

    public void PowerOff() => IsPoweredOn = false;

    public void VolumeUp(int step = 1) => Volume = Math.Clamp(Volume + step, MinVolume, MaxVolume);

    public void VolumeDown(int step = 1) => Volume = Math.Clamp(Volume - step, MinVolume, MaxVolume);
}
