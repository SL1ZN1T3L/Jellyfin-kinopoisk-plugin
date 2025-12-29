using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Client for Kinopoisk API Unofficial.
/// </summary>
public class KinopoiskApiClient : IDisposable
{
    private const string BaseUrl = "https://kinopoiskapiunofficial.tech/api";
    private const string DefaultApiToken = ""; // Users should provide their own token
    
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KinopoiskApiClient> _logger;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly JsonSerializerOptions _jsonOptions;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskApiClient"/> class.
    /// </summary>
    public KinopoiskApiClient(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<KinopoiskApiClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("KinopoiskApi");
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        
        _cache = cache;
        _logger = logger;
        _rateLimiter = new SemaphoreSlim(1, 1);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
    }

    private string GetApiToken()
    {
        var token = Plugin.Instance?.Configuration.ApiToken;
        return string.IsNullOrEmpty(token) ? DefaultApiToken : token;
    }

    private TimeSpan GetCacheDuration()
    {
        var minutes = Plugin.Instance?.Configuration.CacheDurationMinutes ?? 60;
        return TimeSpan.FromMinutes(minutes);
    }

    private async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
    {
        if (Plugin.Instance?.Configuration.EnableRateLimiting != true)
            return;

        var maxRequests = Plugin.Instance?.Configuration.MaxRequestsPerSecond ?? 5;
        var minInterval = TimeSpan.FromMilliseconds(1000.0 / maxRequests);

        await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < minInterval)
            {
                var delay = minInterval - timeSinceLastRequest;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken) where T : class
    {
        var cacheKey = $"kp_{endpoint}";
        
        if (_cache.TryGetValue(cacheKey, out T? cachedResult))
        {
            _logger.LogDebug("Cache hit for {Endpoint}", endpoint);
            return cachedResult;
        }

        await WaitForRateLimitAsync(cancellationToken).ConfigureAwait(false);

        var token = GetApiToken();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Kinopoisk API token is not configured");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("X-API-KEY", token);

            _logger.LogDebug("Requesting {Endpoint}", endpoint);
            
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit exceeded for Kinopoisk API");
                // Wait and retry once
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                return await GetAsync<T>(endpoint, cancellationToken).ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Resource not found: {Endpoint}", endpoint);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("Kinopoisk API error {StatusCode}: {Content}", response.StatusCode, content);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            
            if (result != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = GetCacheDuration()
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error requesting {Endpoint}", endpoint);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for {Endpoint}", endpoint);
            return null;
        }
    }

    /// <summary>
    /// Gets film details by Kinopoisk ID.
    /// </summary>
    public Task<KinopoiskFilm?> GetFilmAsync(int kinopoiskId, CancellationToken cancellationToken)
    {
        return GetAsync<KinopoiskFilm>($"/v2.2/films/{kinopoiskId}", cancellationToken);
    }

    /// <summary>
    /// Searches for films by keyword.
    /// </summary>
    public Task<KinopoiskSearchResponse?> SearchFilmsAsync(string keyword, CancellationToken cancellationToken)
    {
        var encodedKeyword = Uri.EscapeDataString(keyword);
        return GetAsync<KinopoiskSearchResponse>($"/v2.1/films/search-by-keyword?keyword={encodedKeyword}", cancellationToken);
    }

    /// <summary>
    /// Gets staff (actors, directors) for a film.
    /// </summary>
    public Task<List<KinopoiskStaff>?> GetStaffAsync(int kinopoiskId, CancellationToken cancellationToken)
    {
        return GetAsync<List<KinopoiskStaff>>($"/v1/staff?filmId={kinopoiskId}", cancellationToken);
    }

    /// <summary>
    /// Gets person details by staff ID.
    /// </summary>
    public Task<KinopoiskPerson?> GetPersonAsync(int staffId, CancellationToken cancellationToken)
    {
        return GetAsync<KinopoiskPerson>($"/v1/staff/{staffId}", cancellationToken);
    }

    /// <summary>
    /// Gets seasons for a TV series.
    /// </summary>
    public Task<KinopoiskSeasonsResponse?> GetSeasonsAsync(int kinopoiskId, CancellationToken cancellationToken)
    {
        return GetAsync<KinopoiskSeasonsResponse>($"/v2.2/films/{kinopoiskId}/seasons", cancellationToken);
    }

    /// <summary>
    /// Gets images/frames for a film.
    /// </summary>
    public Task<KinopoiskImagesResponse?> GetImagesAsync(int kinopoiskId, string type = "STILL", CancellationToken cancellationToken = default)
    {
        return GetAsync<KinopoiskImagesResponse>($"/v2.2/films/{kinopoiskId}/images?type={type}", cancellationToken);
    }

    /// <summary>
    /// Gets videos/trailers for a film.
    /// </summary>
    public Task<KinopoiskVideosResponse?> GetVideosAsync(int kinopoiskId, CancellationToken cancellationToken)
    {
        return GetAsync<KinopoiskVideosResponse>($"/v2.2/films/{kinopoiskId}/videos", cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _rateLimiter.Dispose();
        }

        _disposed = true;
    }
}
