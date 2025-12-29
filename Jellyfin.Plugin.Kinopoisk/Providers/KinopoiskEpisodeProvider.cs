using System.Globalization;
using System.Net.Http;
using Jellyfin.Plugin.Kinopoisk.Api;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Episode metadata provider for Kinopoisk.
/// </summary>
public class KinopoiskEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
{
    private readonly KinopoiskApiClient _apiClient;
    private readonly ILogger<KinopoiskEpisodeProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskEpisodeProvider"/> class.
    /// </summary>
    public KinopoiskEpisodeProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<KinopoiskEpisodeProvider> logger,
        ILogger<KinopoiskApiClient> apiLogger)
    {
        _apiClient = new KinopoiskApiClient(httpClientFactory, cache, apiLogger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => Plugin.PluginName;

    /// <inheritdoc />
    public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Episode>();

        // Get series Kinopoisk ID
        if (!info.SeriesProviderIds.TryGetValue(Plugin.ProviderId, out var seriesIdString) || 
            !int.TryParse(seriesIdString, out var seriesId))
        {
            _logger.LogDebug("No Kinopoisk series ID found for episode");
            return result;
        }

        var seasonNumber = info.ParentIndexNumber;
        var episodeNumber = info.IndexNumber;

        if (!seasonNumber.HasValue || !episodeNumber.HasValue)
        {
            _logger.LogDebug("Missing season or episode number");
            return result;
        }

        var seasons = await _apiClient.GetSeasonsAsync(seriesId, cancellationToken).ConfigureAwait(false);
        if (seasons?.Items == null)
        {
            _logger.LogDebug("No seasons found for series {Id}", seriesId);
            return result;
        }

        var season = seasons.Items.FirstOrDefault(s => s.Number == seasonNumber.Value);
        if (season?.Episodes == null)
        {
            _logger.LogDebug("Season {SeasonNumber} not found", seasonNumber);
            return result;
        }

        var episode = season.Episodes.FirstOrDefault(e => e.EpisodeNumber == episodeNumber.Value);
        if (episode == null)
        {
            _logger.LogDebug("Episode {EpisodeNumber} not found in season {SeasonNumber}", episodeNumber, seasonNumber);
            return result;
        }

        var preferRussian = Plugin.Instance?.Configuration.PreferRussianMetadata ?? true;

        var episodeResult = new Episode
        {
            Name = episode.GetName(preferRussian),
            Overview = episode.Synopsis,
            IndexNumber = episode.EpisodeNumber,
            ParentIndexNumber = episode.SeasonNumber
        };

        // Parse release date
        if (!string.IsNullOrEmpty(episode.ReleaseDate) && DateTime.TryParse(episode.ReleaseDate, out var releaseDate))
        {
            episodeResult.PremiereDate = releaseDate;
            episodeResult.ProductionYear = releaseDate.Year;
        }

        result.Item = episodeResult;
        result.HasMetadata = true;

        return result;
    }

    /// <inheritdoc />
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
    {
        // Episode search is not supported, return empty
        return Task.FromResult<IEnumerable<RemoteSearchResult>>(Array.Empty<RemoteSearchResult>());
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return new HttpClient().GetAsync(new Uri(url), cancellationToken);
    }
}

/// <summary>
/// Season metadata provider for Kinopoisk.
/// </summary>
public class KinopoiskSeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>
{
    private readonly KinopoiskApiClient _apiClient;
    private readonly ILogger<KinopoiskSeasonProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskSeasonProvider"/> class.
    /// </summary>
    public KinopoiskSeasonProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<KinopoiskSeasonProvider> logger,
        ILogger<KinopoiskApiClient> apiLogger)
    {
        _apiClient = new KinopoiskApiClient(httpClientFactory, cache, apiLogger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => Plugin.PluginName;

    /// <inheritdoc />
    public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Season>();

        if (!info.SeriesProviderIds.TryGetValue(Plugin.ProviderId, out var seriesIdString) || 
            !int.TryParse(seriesIdString, out var seriesId))
        {
            return result;
        }

        var seasonNumber = info.IndexNumber;
        if (!seasonNumber.HasValue)
        {
            return result;
        }

        var seasons = await _apiClient.GetSeasonsAsync(seriesId, cancellationToken).ConfigureAwait(false);
        if (seasons?.Items == null)
        {
            return result;
        }

        var season = seasons.Items.FirstOrDefault(s => s.Number == seasonNumber.Value);
        if (season == null)
        {
            return result;
        }

        var seasonResult = new Season
        {
            Name = $"Сезон {season.Number}",
            IndexNumber = season.Number
        };

        // Try to get premiere date from first episode
        var firstEpisode = season.Episodes?.OrderBy(e => e.EpisodeNumber).FirstOrDefault();
        if (firstEpisode != null && !string.IsNullOrEmpty(firstEpisode.ReleaseDate) && 
            DateTime.TryParse(firstEpisode.ReleaseDate, out var premiereDate))
        {
            seasonResult.PremiereDate = premiereDate;
            seasonResult.ProductionYear = premiereDate.Year;
        }

        result.Item = seasonResult;
        result.HasMetadata = true;

        return result;
    }

    /// <inheritdoc />
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<RemoteSearchResult>>(Array.Empty<RemoteSearchResult>());
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return new HttpClient().GetAsync(new Uri(url), cancellationToken);
    }
}
