using System.Net.Http.Json;
using OnkyoIn.Web.Domain.Gateways;

namespace OnkyoIn.Web.Infrastructure;

/// <summary>
/// Infrastructure adapter that calls the OnkyoIn.Control HTTP API.
/// </summary>
public class OnkyoControlHttpClient : IOnkyoControlClient
{
    private readonly HttpClient _httpClient;

    public OnkyoControlHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ControlDiscoverResponse>(
            "api/device/discover", cancellationToken);

        return response?.IpAddress
            ?? throw new InvalidOperationException("The control API did not return an IP address.");
    }

    public Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default)
        => PostAsync("api/device/power-on", ipAddress, cancellationToken);

    public Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default)
        => PostAsync("api/device/volume/up", ipAddress, cancellationToken);

    public Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default)
        => PostAsync("api/device/volume/down", ipAddress, cancellationToken);

    private async Task PostAsync(string path, string ipAddress, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            path, new { ipAddress }, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private sealed record ControlDiscoverResponse(string IpAddress);
}
