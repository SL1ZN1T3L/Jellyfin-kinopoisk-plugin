using System.Globalization;
using System.Net.Http;
using Jellyfin.Plugin.Kinopoisk.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Person metadata provider for Kinopoisk.
/// </summary>
public class KinopoiskPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
{
    private readonly KinopoiskApiClient _apiClient;
    private readonly ILogger<KinopoiskPersonProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskPersonProvider"/> class.
    /// </summary>
    public KinopoiskPersonProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<KinopoiskPersonProvider> logger,
        ILogger<KinopoiskApiClient> apiLogger)
    {
        _apiClient = new KinopoiskApiClient(httpClientFactory, cache, apiLogger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => Plugin.PluginName;

    /// <inheritdoc />
    public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Person>();

        if (!info.ProviderIds.TryGetValue(Plugin.ProviderId, out var idString) || !int.TryParse(idString, out var personId))
        {
            _logger.LogDebug("No Kinopoisk ID found for person {Name}", info.Name);
            return result;
        }

        var person = await _apiClient.GetPersonAsync(personId, cancellationToken).ConfigureAwait(false);
        if (person == null)
        {
            _logger.LogDebug("Person not found for Kinopoisk ID {Id}", personId);
            return result;
        }

        var preferRussian = Plugin.Instance?.Configuration.PreferRussianMetadata ?? true;

        var personResult = new Person
        {
            Name = person.GetName(preferRussian) ?? info.Name,
            ProductionLocations = !string.IsNullOrEmpty(person.Birthplace) 
                ? new[] { person.Birthplace } 
                : Array.Empty<string>(),
            Overview = GetPersonOverview(person)
        };

        // Parse birthday
        if (!string.IsNullOrEmpty(person.Birthday) && DateTime.TryParse(person.Birthday, out var birthday))
        {
            personResult.PremiereDate = birthday;
        }

        // Parse death date
        if (!string.IsNullOrEmpty(person.Death) && DateTime.TryParse(person.Death, out var death))
        {
            personResult.EndDate = death;
        }

        personResult.ProviderIds[Plugin.ProviderId] = personId.ToString(CultureInfo.InvariantCulture);

        result.Item = personResult;
        result.HasMetadata = true;

        return result;
    }

    /// <inheritdoc />
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        // Kinopoisk API doesn't have person search, so we return empty results
        // Persons are typically found via film staff
        return Task.FromResult<IEnumerable<RemoteSearchResult>>(Array.Empty<RemoteSearchResult>());
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return new HttpClient().GetAsync(new Uri(url), cancellationToken);
    }

    private static string? GetPersonOverview(Api.Models.KinopoiskPerson person)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(person.Profession))
        {
            parts.Add(person.Profession);
        }

        if (person.Growth.HasValue && person.Growth > 0)
        {
            parts.Add($"Рост: {person.Growth} см");
        }

        if (!string.IsNullOrEmpty(person.Birthplace))
        {
            parts.Add($"Место рождения: {person.Birthplace}");
        }

        if (person.Facts?.Count > 0)
        {
            parts.Add(string.Empty);
            parts.Add("Факты:");
            parts.AddRange(person.Facts.Take(5).Select(f => $"• {f}"));
        }

        return parts.Count > 0 ? string.Join("\n", parts) : null;
    }
}
