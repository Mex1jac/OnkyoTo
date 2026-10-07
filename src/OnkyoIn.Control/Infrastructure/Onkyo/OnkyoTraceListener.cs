using System.Diagnostics;

namespace OnkyoIn.Control.Infrastructure.Onkyo;

/// <summary>
/// Trace listener for the third-party Onkyo.eISCP library.
///
/// The library reports internal socket hiccups (for example, the read loop
/// noticing a disposed connection) through <see cref="Trace.Fail(string)"/>.
/// The default trace listener treats that as a fatal assertion and terminates
/// the whole process, which would crash the API on ordinary disconnects.
///
/// This listener downgrades those reports to log entries so a transient socket
/// issue cannot take down the hosting process.
/// </summary>
public sealed class OnkyoTraceListener : TraceListener
{
    private readonly ILogger _logger;

    public OnkyoTraceListener(ILogger logger)
    {
        _logger = logger;
    }

    public override void Write(string? message) => _logger.LogDebug("eISCP: {Message}", message);

    public override void WriteLine(string? message) => _logger.LogDebug("eISCP: {Message}", message);

    public override void Fail(string? message) =>
        _logger.LogWarning("Onkyo.eISCP reported a recoverable failure: {Message}", message);

    public override void Fail(string? message, string? detailMessage) =>
        _logger.LogWarning(
            "Onkyo.eISCP reported a recoverable failure: {Message} | {Detail}",
            message, detailMessage);
}
