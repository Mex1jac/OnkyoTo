using System.Net;
using System.Text;
using OnkyoIn.Web.Domain.Exceptions;
using OnkyoIn.Web.Infrastructure;

namespace OnkyoIn.Web.Tests.Infrastructure;

public class OnkyoControlHttpClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private static OnkyoControlHttpClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var http = new HttpClient(new StubHandler(responder))
        {
            BaseAddress = new Uri("https://localhost:7066"),
        };
        return new OnkyoControlHttpClient(http);
    }

    [Fact]
    public async Task DiscoverAsync_WhenProblemDetails_ThrowsWithDetail()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"title":"Device not found","detail":"No Onkyo device was found on the network.","status":404}""",
                Encoding.UTF8, "application/problem+json"),
        });

        var exception = await Assert.ThrowsAsync<ControlApiException>(() => client.DiscoverAsync());

        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("No Onkyo device was found on the network.", exception.Message);
    }

    [Fact]
    public async Task PowerOnAsync_WhenProblemDetails_ThrowsWithDetail()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(
                """{"title":"Device communication error","detail":"Could not reach the device.","status":502}""",
                Encoding.UTF8, "application/problem+json"),
        });

        var exception = await Assert.ThrowsAsync<ControlApiException>(
            () => client.PowerOnAsync("192.168.1.50"));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("Could not reach the device.", exception.Message);
    }

    [Fact]
    public async Task PowerOnAsync_WhenNonJsonError_FallsBackToStatus()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom", Encoding.UTF8, "text/plain"),
        });

        var exception = await Assert.ThrowsAsync<ControlApiException>(
            () => client.PowerOnAsync("192.168.1.50"));

        Assert.Equal(500, exception.StatusCode);
    }

    [Fact]
    public async Task PowerOnAsync_WhenSuccess_DoesNotThrow()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.PowerOnAsync("192.168.1.50");
    }

    [Fact]
    public async Task GetStateAsync_WhenSuccess_ReturnsState()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"ipAddress":"192.168.1.50","isPoweredOn":true,"volume":27,"currentInputId":"cd","currentInputName":"CD"}""",
                Encoding.UTF8, "application/json"),
        });

        var state = await client.GetStateAsync("192.168.1.50");

        Assert.Equal("192.168.1.50", state.IpAddress);
        Assert.True(state.IsPoweredOn);
        Assert.Equal(27, state.Volume);
        Assert.Equal("cd", state.CurrentInputId);
        Assert.Equal("CD", state.CurrentInputName);
    }

    [Fact]
    public async Task GetInputsAsync_WhenSuccess_ReturnsSources()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """[{"id":"cd","name":"CD","isCurrent":true},{"id":"fm","name":"FM Tuner","isCurrent":false}]""",
                Encoding.UTF8, "application/json"),
        });

        var sources = await client.GetInputsAsync("192.168.1.50");

        Assert.Equal(2, sources.Count);
        Assert.Equal("cd", sources[0].Id);
        Assert.True(sources[0].IsCurrent);
        Assert.Equal("fm", sources[1].Id);
        Assert.False(sources[1].IsCurrent);
    }

    [Fact]
    public async Task GetInputsAsync_WhenProblemDetails_ThrowsWithDetail()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(
                """{"title":"Device communication error","detail":"Could not reach the device.","status":502}""",
                Encoding.UTF8, "application/problem+json"),
        });

        var exception = await Assert.ThrowsAsync<ControlApiException>(
            () => client.GetInputsAsync("192.168.1.50"));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("Could not reach the device.", exception.Message);
    }

    [Fact]
    public async Task PowerToggleAsync_WhenSuccess_ReturnsState()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"ipAddress":"192.168.1.50","isPoweredOn":false,"volume":0,"currentInputId":null,"currentInputName":null}""",
                Encoding.UTF8, "application/json"),
        });

        var state = await client.PowerToggleAsync("192.168.1.50");

        Assert.Equal("192.168.1.50", state.IpAddress);
        Assert.False(state.IsPoweredOn);
    }

    [Fact]
    public async Task PowerToggleAsync_WhenProblemDetails_ThrowsWithDetail()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(
                """{"title":"Device communication error","detail":"Could not reach the device.","status":502}""",
                Encoding.UTF8, "application/problem+json"),
        });

        var exception = await Assert.ThrowsAsync<ControlApiException>(
            () => client.PowerToggleAsync("192.168.1.50"));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("Could not reach the device.", exception.Message);
    }

    [Fact]
    public async Task SetInputAsync_WhenSuccess_DoesNotThrow()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.SetInputAsync("192.168.1.50", "cd");
    }

    [Fact]
    public async Task SetInputAsync_WhenProblemDetails_ThrowsWithDetail()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"title":"Invalid request","detail":"Unknown input source 'hdmi-arc'.","status":400}""",
                Encoding.UTF8, "application/problem+json"),
        });

        var exception = await Assert.ThrowsAsync<ControlApiException>(
            () => client.SetInputAsync("192.168.1.50", "hdmi-arc"));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("Unknown input source 'hdmi-arc'.", exception.Message);
    }

    [Fact]
    public async Task GetStateAsync_WhenProblemDetails_ThrowsWithDetail()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(
                """{"title":"Device communication error","detail":"Could not reach the device.","status":502}""",
                Encoding.UTF8, "application/problem+json"),
        });

        var exception = await Assert.ThrowsAsync<ControlApiException>(
            () => client.GetStateAsync("192.168.1.50"));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("Could not reach the device.", exception.Message);
    }
}
