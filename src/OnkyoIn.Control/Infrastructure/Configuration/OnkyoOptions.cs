namespace OnkyoIn.Control.Infrastructure.Configuration;

/// <summary>
/// Runtime settings for talking to Onkyo devices, bound from the "Onkyo"
/// section of appsettings.json. Values can be overridden per environment
/// (or via environment variables such as Onkyo__PowerOnTimeoutMs)
/// without recompiling the application.
/// </summary>
public class OnkyoOptions
{
    public const string SectionName = "Onkyo";

    /// <summary>
    /// TCP port of the device's eISCP interface.
    /// </summary>
    public int Port { get; set; } = 60128;

    /// <summary>
    /// Time to wait for the power-on acknowledgement. Receivers coming out of
    /// standby can take well over the library's 2000 ms default before they
    /// report <c>PWR01</c> (measured ~2.5 s on a real device), which used to
    /// surface as a timeout even though the device did power on.
    /// </summary>
    public int PowerOnTimeoutMs { get; set; } = 6000;
}
