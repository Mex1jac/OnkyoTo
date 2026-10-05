namespace OnkyoIn.Web.Domain.Gateways;

/// <summary>
/// Port that abstracts calls to the OnkyoIn.Control HTTP API.
/// Implemented in the Infrastructure layer with HttpClient.
/// </summary>
public interface IOnkyoControlClient
{
    Task<string> DiscoverAsync(CancellationToken cancellationToken = default);

    Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default);
}
