using System.Net;
using System.Net.Http.Json;
using OnkyoIn.Web.Domain.Exceptions;
using OnkyoIn.Web.Domain.Gateways;

namespace OnkyoIn.Web.Infrastructure;

/// <summary>
/// Infrastructure adapter that calls the OnkyoIn.Control HTTP API.
/// Turns error responses (ProblemDetails) and network failures into
/// <see cref="ControlApiException"/>.
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
        var response = await SendAsync(
            () => _httpClient.GetAsync("api/device/discover", cancellationToken),
            cancellationToken);

        var payload = await ReadAsync<ControlDiscoverResponse>(response, cancellationToken);

        return payload?.IpAddress
            ?? throw new ControlApiException(
                (int)response.StatusCode,
                "The control API did not return an IP address.");
    }

    public Task PowerOnAsync(string ipAddress, CancellationToken cancellationToken = default)
        => PostAsync("api/device/power-on", ipAddress, cancellationToken);

    public Task VolumeUpAsync(string ipAddress, CancellationToken cancellationToken = default)
        => PostAsync("api/device/volume/up", ipAddress, cancellationToken);

    public Task VolumeDownAsync(string ipAddress, CancellationToken cancellationToken = default)
        => PostAsync("api/device/volume/down", ipAddress, cancellationToken);

    private async Task PostAsync(string path, string ipAddress, CancellationToken cancellationToken)
    {
        var response = await SendAsync(
            () => _httpClient.PostAsJsonAsync(path, new { ipAddress }, cancellationToken),
            cancellationToken);

        // Any 2xx/3xx response means the command was accepted.
        if (!response.IsSuccessStatusCode)
        {
            await ThrowProblemAsync(response, cancellationToken);
        }
    }

    private static async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            return await send();
        }
        catch (HttpRequestException ex)
        {
            throw new ControlApiException(
                (int)HttpStatusCode.BadGateway,
                "Could not reach the control service. Is OnkyoIn.Control running?", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ControlApiException(
                (int)HttpStatusCode.GatewayTimeout,
                "The control service took too long to respond.", ex);
        }
    }

    private static async Task<T?> ReadAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowProblemAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private static async Task ThrowProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        ProblemResponse? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(cancellationToken);
        }
        catch
        {
            // Body was not JSON; fall back to the status code only.
        }

        var message = problem?.Detail
            ?? problem?.Title
            ?? $"The control service returned {(int)response.StatusCode} {response.ReasonPhrase}.";

        throw new ControlApiException((int)response.StatusCode, message);
    }

    private sealed record ControlDiscoverResponse(string IpAddress);

    private sealed record ProblemResponse(string? Title, string? Detail, int? Status);
}
