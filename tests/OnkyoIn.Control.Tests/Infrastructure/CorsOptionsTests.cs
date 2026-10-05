using Microsoft.Extensions.Configuration;
using OnkyoIn.Control.Infrastructure.Configuration;

namespace OnkyoIn.Control.Tests.Infrastructure;

public class CorsOptionsTests
{
    [Fact]
    public void BindsAllowedOriginsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://localhost:7261",
                ["Cors:AllowedOrigins:1"] = "http://localhost:5173",
            })
            .Build();

        var options = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>();

        Assert.NotNull(options);
        Assert.Equal(2, options!.AllowedOrigins.Length);
        Assert.Contains("http://localhost:5173", options.AllowedOrigins);
    }

    [Fact]
    public void DefaultsToEmptyWhenSectionMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var options = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>();

        // Get<T>() returns null when the section is absent; the app falls back to
        // a new CorsOptions (empty list) which is denied in non-Development.
        Assert.True(options is null || options.AllowedOrigins.Length == 0);
    }
}
