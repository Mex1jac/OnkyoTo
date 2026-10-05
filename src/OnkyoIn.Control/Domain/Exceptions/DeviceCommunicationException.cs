namespace OnkyoIn.Control.Domain.Exceptions;

/// <summary>
/// Thrown when the application cannot communicate with an Onkyo device
/// (connection refused, timeout, network error, etc.).
/// </summary>
public class DeviceCommunicationException : Exception
{
    public DeviceCommunicationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
