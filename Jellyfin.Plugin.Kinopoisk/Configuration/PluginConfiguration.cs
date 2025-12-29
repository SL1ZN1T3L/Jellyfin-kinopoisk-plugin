using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Kinopoisk.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ApiToken = string.Empty;
        CacheDurationMinutes = 60;
        MaxRequestsPerSecond = 5;
        EnableRateLimiting = true;
        PreferRussianMetadata = true;
    }

    /// <summary>
    /// Gets or sets the Kinopoisk API token.
    /// Get your token at https://kinopoiskapiunofficial.tech
    /// </summary>
    public string ApiToken { get; set; }

    /// <summary>
    /// Gets or sets the cache duration in minutes.
    /// </summary>
    public int CacheDurationMinutes { get; set; }

    /// <summary>
    /// Gets or sets the maximum requests per second.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether rate limiting is enabled.
    /// </summary>
    public bool EnableRateLimiting { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to prefer Russian metadata.
    /// </summary>
    public bool PreferRussianMetadata { get; set; }
}
