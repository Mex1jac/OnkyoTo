namespace OnkyoIn.Web.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed CORS settings bound from the "Cors" configuration section.
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Origins allowed to call this API from a browser.</summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
