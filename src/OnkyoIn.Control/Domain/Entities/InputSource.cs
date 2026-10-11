namespace OnkyoIn.Control.Domain.Entities;

/// <summary>
/// An input source ("source") that can be selected on an Onkyo device.
/// </summary>
/// <param name="Id">Stable identifier used by the API.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="SliCode">Two-digit hexadecimal suffix of the eISCP <c>SLI</c> command.</param>
public record InputSource(string Id, string Name, string SliCode);

/// <summary>
/// Catalog of the input sources of an Onkyo TX-NR6100, using the raw
/// eISCP codes from the receiver's command reference.
///
/// eISCP has no command to list the available inputs, so the known
/// catalog is exposed by the API and the currently selected input is
/// queried separately with <c>SLIQSTN</c>.
/// </summary>
public static class InputSourceCatalog
{
    public static IReadOnlyList<InputSource> Sources { get; } = new[]
    {
        new InputSource("bd-dvd", "BD/DVD", "10"),
        new InputSource("cbl-sat", "CBL/SAT", "01"),
        new InputSource("game", "GAME", "02"),
        new InputSource("aux", "AUX (Front)", "03"),
        new InputSource("pc", "PC", "05"),
        new InputSource("strm-box", "STRM BOX", "11"),
        new InputSource("tv", "TV", "12"),
        new InputSource("phono", "PHONO", "22"),
        new InputSource("cd", "CD", "23"),
        new InputSource("am", "AM Tuner", "25"),
        new InputSource("fm", "FM Tuner", "24"),
        new InputSource("network", "NET (Network/AirPlay)", "2B"),
        new InputSource("bluetooth", "Bluetooth", "2E"),
    };

    /// <summary>Finds a source by its API identifier (case-insensitive).</summary>
    public static InputSource? FindById(string? id)
        => Sources.FirstOrDefault(source =>
            string.Equals(source.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds a source by its eISCP SLI code (case-insensitive).</summary>
    public static InputSource? FindBySliCode(string? sliCode)
        => Sources.FirstOrDefault(source =>
            string.Equals(source.SliCode, sliCode, StringComparison.OrdinalIgnoreCase));
}
