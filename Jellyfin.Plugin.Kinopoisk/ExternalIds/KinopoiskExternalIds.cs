using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.ExternalIds;

/// <summary>
/// External ID for Kinopoisk movies.
/// </summary>
public class KinopoiskMovieExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => Plugin.PluginName;

    /// <inheritdoc />
    public string Key => Plugin.ProviderId;

    /// <inheritdoc />
    public ExternalIdMediaType Type => ExternalIdMediaType.Movie;

    /// <inheritdoc />
    public string UrlFormatString => "https://www.kinopoisk.ru/film/{0}/";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Movie;
}

/// <summary>
/// External ID for Kinopoisk series.
/// </summary>
public class KinopoiskSeriesExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => Plugin.PluginName;

    /// <inheritdoc />
    public string Key => Plugin.ProviderId;

    /// <inheritdoc />
    public ExternalIdMediaType Type => ExternalIdMediaType.Series;

    /// <inheritdoc />
    public string UrlFormatString => "https://www.kinopoisk.ru/series/{0}/";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Series;
}

/// <summary>
/// External ID for Kinopoisk persons.
/// </summary>
public class KinopoiskPersonExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => Plugin.PluginName;

    /// <inheritdoc />
    public string Key => Plugin.ProviderId;

    /// <inheritdoc />
    public ExternalIdMediaType Type => ExternalIdMediaType.Person;

    /// <inheritdoc />
    public string UrlFormatString => "https://www.kinopoisk.ru/name/{0}/";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Person;
}
