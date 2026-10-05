namespace OnkyoIn.Control.Domain.Exceptions;

/// <summary>
/// Thrown when no Onkyo device could be found on the network.
/// </summary>
public class DeviceNotFoundException : Exception
{
    public DeviceNotFoundException(string message) : base(message)
    {
    }
}
