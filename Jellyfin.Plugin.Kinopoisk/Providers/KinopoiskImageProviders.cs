using System.Globalization;
using System.Net.Http;
using Jellyfin.Plugin.Kinopoisk.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Image provider for movies.
/// </summary>
public class KinopoiskMovieImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly KinopoiskApiClient _apiClient;
    private readonly ILogger<KinopoiskMovieImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskMovieImageProvider"/> class.
    /// </summary>
    public KinopoiskMovieImageProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<KinopoiskMovieImageProvider> logger,
        ILogger<KinopoiskApiClient> apiLogger)
    {
        _apiClient = new KinopoiskApiClient(httpClientFactory, cache, apiLogger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => Plugin.PluginName;

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is Movie;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new[]
        {
            ImageType.Primary,
            ImageType.Backdrop,
            ImageType.Logo
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var images = new List<RemoteImageInfo>();

        if (!item.ProviderIds.TryGetValue(Plugin.ProviderId, out var idString) || !int.TryParse(idString, out var kinopoiskId))
        {
            return images;
        }

        var film = await _apiClient.GetFilmAsync(kinopoiskId, cancellationToken).ConfigureAwait(false);
        if (film == null)
        {
            return images;
        }

        // Add poster
        if (!string.IsNullOrEmpty(film.PosterUrl))
        {
            images.Add(new RemoteImageInfo
            {
                Url = film.PosterUrl,
                ThumbnailUrl = film.PosterUrlPreview,
                Type = ImageType.Primary,
                ProviderName = Name,
                Language = "ru"
            });
        }

        // Add cover/backdrop
        if (!string.IsNullOrEmpty(film.CoverUrl))
        {
            images.Add(new RemoteImageInfo
            {
                Url = film.CoverUrl,
                Type = ImageType.Backdrop,
                ProviderName = Name,
                Language = "ru"
            });
        }

        // Add logo
        if (!string.IsNullOrEmpty(film.LogoUrl))
        {
            images.Add(new RemoteImageInfo
            {
                Url = film.LogoUrl,
                Type = ImageType.Logo,
                ProviderName = Name,
                Language = "ru"
            });
        }

        // Get additional backdrops from images API
        var backdrops = await _apiClient.GetImagesAsync(kinopoiskId, "STILL", cancellationToken).ConfigureAwait(false);
        if (backdrops?.Items != null)
        {
            foreach (var backdrop in backdrops.Items.Take(5))
            {
                if (!string.IsNullOrEmpty(backdrop.ImageUrl))
                {
                    images.Add(new RemoteImageInfo
                    {
                        Url = backdrop.ImageUrl,
                        ThumbnailUrl = backdrop.PreviewUrl,
                        Type = ImageType.Backdrop,
                        ProviderName = Name,
                        Language = "ru"
                    });
                }
            }
        }

        return images;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return new HttpClient().GetAsync(new Uri(url), cancellationToken);
    }
}

/// <summary>
/// Image provider for series.
/// </summary>
public class KinopoiskSeriesImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly KinopoiskApiClient _apiClient;
    private readonly ILogger<KinopoiskSeriesImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskSeriesImageProvider"/> class.
    /// </summary>
    public KinopoiskSeriesImageProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<KinopoiskSeriesImageProvider> logger,
        ILogger<KinopoiskApiClient> apiLogger)
    {
        _apiClient = new KinopoiskApiClient(httpClientFactory, cache, apiLogger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => Plugin.PluginName;

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is Series;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new[]
        {
            ImageType.Primary,
            ImageType.Backdrop,
            ImageType.Logo
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var images = new List<RemoteImageInfo>();

        if (!item.ProviderIds.TryGetValue(Plugin.ProviderId, out var idString) || !int.TryParse(idString, out var kinopoiskId))
        {
            return images;
        }

        var film = await _apiClient.GetFilmAsync(kinopoiskId, cancellationToken).ConfigureAwait(false);
        if (film == null)
        {
            return images;
        }

        if (!string.IsNullOrEmpty(film.PosterUrl))
        {
            images.Add(new RemoteImageInfo
            {
                Url = film.PosterUrl,
                ThumbnailUrl = film.PosterUrlPreview,
                Type = ImageType.Primary,
                ProviderName = Name,
                Language = "ru"
            });
        }

        if (!string.IsNullOrEmpty(film.CoverUrl))
        {
            images.Add(new RemoteImageInfo
            {
                Url = film.CoverUrl,
                Type = ImageType.Backdrop,
                ProviderName = Name,
                Language = "ru"
            });
        }

        if (!string.IsNullOrEmpty(film.LogoUrl))
        {
            images.Add(new RemoteImageInfo
            {
                Url = film.LogoUrl,
                Type = ImageType.Logo,
                ProviderName = Name,
                Language = "ru"
            });
        }

        var backdrops = await _apiClient.GetImagesAsync(kinopoiskId, "STILL", cancellationToken).ConfigureAwait(false);
        if (backdrops?.Items != null)
        {
            foreach (var backdrop in backdrops.Items.Take(5))
            {
                if (!string.IsNullOrEmpty(backdrop.ImageUrl))
                {
                    images.Add(new RemoteImageInfo
                    {
                        Url = backdrop.ImageUrl,
                        ThumbnailUrl = backdrop.PreviewUrl,
                        Type = ImageType.Backdrop,
                        ProviderName = Name,
                        Language = "ru"
                    });
                }
            }
        }

        return images;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return new HttpClient().GetAsync(new Uri(url), cancellationToken);
    }
}

/// <summary>
/// Image provider for persons.
/// </summary>
public class KinopoiskPersonImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly KinopoiskApiClient _apiClient;
    private readonly ILogger<KinopoiskPersonImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskPersonImageProvider"/> class.
    /// </summary>
    public KinopoiskPersonImageProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<KinopoiskPersonImageProvider> logger,
        ILogger<KinopoiskApiClient> apiLogger)
    {
        _apiClient = new KinopoiskApiClient(httpClientFactory, cache, apiLogger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => Plugin.PluginName;

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is Person;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new[] { ImageType.Primary };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var images = new List<RemoteImageInfo>();

        if (!item.ProviderIds.TryGetValue(Plugin.ProviderId, out var idString) || !int.TryParse(idString, out var personId))
        {
            return images;
        }

        var person = await _apiClient.GetPersonAsync(personId, cancellationToken).ConfigureAwait(false);
        if (person == null || string.IsNullOrEmpty(person.PosterUrl))
        {
            return images;
        }

        images.Add(new RemoteImageInfo
        {
            Url = person.PosterUrl,
            Type = ImageType.Primary,
            ProviderName = Name,
            Language = "ru"
        });

        return images;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return new HttpClient().GetAsync(new Uri(url), cancellationToken);
    }
}
