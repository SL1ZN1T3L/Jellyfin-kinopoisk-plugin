using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Kinopoisk.Api;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Movie metadata provider for Kinopoisk.
/// </summary>
public partial class KinopoiskMovieProvider : IRemoteMetadataProvider<Movie, MovieInfo>
{
    private readonly KinopoiskApiClient _apiClient;
    private readonly ILogger<KinopoiskMovieProvider> _logger;

    // Pattern to extract Kinopoisk ID from filename: kp-12345 or kp12345
    [GeneratedRegex(@"kp-?(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex KinopoiskIdPattern();

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskMovieProvider"/> class.
    /// </summary>
    public KinopoiskMovieProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<KinopoiskMovieProvider> logger,
        ILogger<KinopoiskApiClient> apiLogger)
    {
        _apiClient = new KinopoiskApiClient(httpClientFactory, cache, apiLogger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => Plugin.PluginName;

    /// <inheritdoc />
    public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Movie>();

        var kinopoiskId = GetKinopoiskId(info);
        
        if (kinopoiskId == 0)
        {
            // Try to search by name
            var searchResults = await _apiClient.SearchFilmsAsync(info.Name, cancellationToken).ConfigureAwait(false);
            var matchingFilm = searchResults?.Films?
                .Where(f => f.Type == "FILM" || f.Type == "VIDEO")
                .FirstOrDefault(f => 
                    (info.Year == null || f.Year == info.Year.ToString()) &&
                    (MatchesName(f.NameRu, info.Name) || 
                     MatchesName(f.NameEn, info.Name) || 
                     MatchesName(f.NameOriginal, info.Name)));

            if (matchingFilm != null)
            {
                kinopoiskId = matchingFilm.EffectiveId;
            }
        }

        if (kinopoiskId == 0)
        {
            _logger.LogDebug("No Kinopoisk ID found for {Name}", info.Name);
            return result;
        }

        var film = await _apiClient.GetFilmAsync(kinopoiskId, cancellationToken).ConfigureAwait(false);
        if (film == null)
        {
            _logger.LogDebug("Film not found for Kinopoisk ID {Id}", kinopoiskId);
            return result;
        }

        var preferRussian = Plugin.Instance?.Configuration.PreferRussianMetadata ?? true;

        var movie = new Movie
        {
            Name = film.GetName(preferRussian) ?? info.Name,
            OriginalTitle = film.NameOriginal ?? film.NameEn,
            Overview = film.Description ?? film.ShortDescription,
            Tagline = film.Slogan,
            ProductionYear = film.Year,
            CommunityRating = (float?)film.RatingKinopoisk,
            CriticRating = film.RatingImdb.HasValue ? (float?)(film.RatingImdb * 10) : null,
        };

        // Set provider IDs
        movie.ProviderIds[Plugin.ProviderId] = kinopoiskId.ToString(CultureInfo.InvariantCulture);
        
        if (!string.IsNullOrEmpty(film.ImdbId))
        {
            movie.ProviderIds[MetadataProvider.Imdb.ToString()] = film.ImdbId;
        }

        // Set genres
        if (film.Genres?.Count > 0)
        {
            foreach (var genre in film.Genres.Where(g => !string.IsNullOrEmpty(g.Genre)))
            {
                movie.AddGenre(CapitalizeFirstLetter(genre.Genre!));
            }
        }

        // Set production locations
        if (film.Countries?.Count > 0)
        {
            movie.ProductionLocations = film.Countries
                .Where(c => !string.IsNullOrEmpty(c.Country))
                .Select(c => c.Country!)
                .ToArray();
        }

        // Set runtime
        if (film.FilmLength.HasValue && film.FilmLength > 0)
        {
            movie.RunTimeTicks = TimeSpan.FromMinutes(film.FilmLength.Value).Ticks;
        }

        // Set official rating
        if (!string.IsNullOrEmpty(film.RatingAgeLimits))
        {
            var ageMatch = Regex.Match(film.RatingAgeLimits, @"age(\d+)");
            if (ageMatch.Success)
            {
                movie.OfficialRating = $"{ageMatch.Groups[1].Value}+";
            }
        }
        else if (!string.IsNullOrEmpty(film.RatingMpaa))
        {
            movie.OfficialRating = film.RatingMpaa.ToUpperInvariant();
        }

        result.Item = movie;
        result.HasMetadata = true;

        // Get staff (actors, directors)
        await AddPeopleAsync(result, kinopoiskId, preferRussian, cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();

        var kinopoiskId = GetKinopoiskId(searchInfo);
        
        if (kinopoiskId > 0)
        {
            var film = await _apiClient.GetFilmAsync(kinopoiskId, cancellationToken).ConfigureAwait(false);
            if (film != null)
            {
                var preferRussian = Plugin.Instance?.Configuration.PreferRussianMetadata ?? true;
                results.Add(CreateSearchResult(film.EffectiveId, film.GetName(preferRussian), film.Year, film.PosterUrlPreview ?? film.PosterUrl));
            }
        }
        else
        {
            var searchResponse = await _apiClient.SearchFilmsAsync(searchInfo.Name, cancellationToken).ConfigureAwait(false);
            if (searchResponse?.Films != null)
            {
                var preferRussian = Plugin.Instance?.Configuration.PreferRussianMetadata ?? true;
                
                foreach (var film in searchResponse.Films.Where(f => f.Type == "FILM" || f.Type == "VIDEO").Take(10))
                {
                    int.TryParse(film.Year, out var year);
                    results.Add(CreateSearchResult(film.EffectiveId, film.GetName(preferRussian), year > 0 ? year : null, film.PosterUrlPreview ?? film.PosterUrl));
                }
            }
        }

        return results;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return new HttpClient().GetAsync(new Uri(url), cancellationToken);
    }

    private static int GetKinopoiskId(MovieInfo info)
    {
        // Check if ID is set in provider IDs
        if (info.ProviderIds.TryGetValue(Plugin.ProviderId, out var idString) && int.TryParse(idString, out var id))
        {
            return id;
        }

        // Try to extract from filename or path
        var match = KinopoiskIdPattern().Match(info.Path ?? string.Empty);
        if (match.Success && int.TryParse(match.Groups[1].Value, out id))
        {
            return id;
        }

        match = KinopoiskIdPattern().Match(info.Name ?? string.Empty);
        if (match.Success && int.TryParse(match.Groups[1].Value, out id))
        {
            return id;
        }

        return 0;
    }

    private static bool MatchesName(string? filmName, string searchName)
    {
        if (string.IsNullOrEmpty(filmName) || string.IsNullOrEmpty(searchName))
            return false;

        return filmName.Equals(searchName, StringComparison.OrdinalIgnoreCase) ||
               filmName.Contains(searchName, StringComparison.OrdinalIgnoreCase) ||
               searchName.Contains(filmName, StringComparison.OrdinalIgnoreCase);
    }

    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    private static RemoteSearchResult CreateSearchResult(int id, string? name, int? year, string? imageUrl)
    {
        var result = new RemoteSearchResult
        {
            Name = name,
            ProductionYear = year,
            ImageUrl = imageUrl,
            SearchProviderName = Plugin.PluginName
        };
        
        result.ProviderIds[Plugin.ProviderId] = id.ToString(CultureInfo.InvariantCulture);

        return result;
    }

    private async Task AddPeopleAsync(MetadataResult<Movie> result, int kinopoiskId, bool preferRussian, CancellationToken cancellationToken)
    {
        var staff = await _apiClient.GetStaffAsync(kinopoiskId, cancellationToken).ConfigureAwait(false);
        if (staff == null)
            return;

        foreach (var person in staff)
        {
            var personInfo = new MediaBrowser.Controller.Entities.PersonInfo
            {
                Name = person.GetName(preferRussian) ?? "Unknown",
                ImageUrl = person.PosterUrl,
                Role = person.Description
            };

            personInfo.ProviderIds[Plugin.ProviderId] = person.StaffId.ToString(CultureInfo.InvariantCulture);

            switch (person.ProfessionKey?.ToUpperInvariant())
            {
                case "DIRECTOR":
                    personInfo.Type = PersonKind.Director;
                    break;
                case "WRITER":
                case "SCREENWRITER":
                    personInfo.Type = PersonKind.Writer;
                    break;
                case "PRODUCER":
                case "PRODUCER_USSR":
                    personInfo.Type = PersonKind.Producer;
                    break;
                case "COMPOSER":
                    personInfo.Type = PersonKind.Composer;
                    break;
                case "ACTOR":
                case "VOICE_DIRECTOR":
                case "VOICE_MALE":
                case "VOICE_FEMALE":
                    personInfo.Type = PersonKind.Actor;
                    break;
                default:
                    continue; // Skip unknown roles
            }

            result.AddPerson(personInfo);
        }
    }
}
