using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Film data from Kinopoisk API.
/// </summary>
public class KinopoiskFilm
{
    [JsonPropertyName("kinopoiskId")]
    public int? KinopoiskId { get; set; }

    [JsonPropertyName("filmId")]
    public int? FilmId { get; set; }

    [JsonPropertyName("imdbId")]
    public string? ImdbId { get; set; }

    [JsonPropertyName("nameRu")]
    public string? NameRu { get; set; }

    [JsonPropertyName("nameEn")]
    public string? NameEn { get; set; }

    [JsonPropertyName("nameOriginal")]
    public string? NameOriginal { get; set; }

    [JsonPropertyName("posterUrl")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("posterUrlPreview")]
    public string? PosterUrlPreview { get; set; }

    [JsonPropertyName("coverUrl")]
    public string? CoverUrl { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("ratingKinopoisk")]
    public double? RatingKinopoisk { get; set; }

    [JsonPropertyName("ratingImdb")]
    public double? RatingImdb { get; set; }

    [JsonPropertyName("ratingKinopoiskVoteCount")]
    public int? RatingKinopoiskVoteCount { get; set; }

    [JsonPropertyName("ratingImdbVoteCount")]
    public int? RatingImdbVoteCount { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("filmLength")]
    public int? FilmLength { get; set; }

    [JsonPropertyName("slogan")]
    public string? Slogan { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("ratingMpaa")]
    public string? RatingMpaa { get; set; }

    [JsonPropertyName("ratingAgeLimits")]
    public string? RatingAgeLimits { get; set; }

    [JsonPropertyName("startYear")]
    public int? StartYear { get; set; }

    [JsonPropertyName("endYear")]
    public int? EndYear { get; set; }

    [JsonPropertyName("serial")]
    public bool? Serial { get; set; }

    [JsonPropertyName("completed")]
    public bool? Completed { get; set; }

    [JsonPropertyName("countries")]
    public List<KinopoiskCountry>? Countries { get; set; }

    [JsonPropertyName("genres")]
    public List<KinopoiskGenre>? Genres { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    /// <summary>
    /// Gets the effective Kinopoisk ID.
    /// </summary>
    [JsonIgnore]
    public int EffectiveId => KinopoiskId ?? FilmId ?? 0;

    /// <summary>
    /// Gets the best available name.
    /// </summary>
    public string? GetName(bool preferRussian)
    {
        if (preferRussian && !string.IsNullOrEmpty(NameRu))
            return NameRu;
        return NameOriginal ?? NameEn ?? NameRu;
    }
}

/// <summary>
/// Country data.
/// </summary>
public class KinopoiskCountry
{
    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

/// <summary>
/// Genre data.
/// </summary>
public class KinopoiskGenre
{
    [JsonPropertyName("genre")]
    public string? Genre { get; set; }
}

/// <summary>
/// Search result item.
/// </summary>
public class KinopoiskSearchItem
{
    [JsonPropertyName("kinopoiskId")]
    public int? KinopoiskId { get; set; }

    [JsonPropertyName("filmId")]
    public int? FilmId { get; set; }

    [JsonPropertyName("nameRu")]
    public string? NameRu { get; set; }

    [JsonPropertyName("nameEn")]
    public string? NameEn { get; set; }

    [JsonPropertyName("nameOriginal")]
    public string? NameOriginal { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("year")]
    public string? Year { get; set; }

    [JsonPropertyName("posterUrl")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("posterUrlPreview")]
    public string? PosterUrlPreview { get; set; }

    [JsonPropertyName("ratingKinopoisk")]
    public double? RatingKinopoisk { get; set; }

    [JsonIgnore]
    public int EffectiveId => KinopoiskId ?? FilmId ?? 0;

    public string? GetName(bool preferRussian)
    {
        if (preferRussian && !string.IsNullOrEmpty(NameRu))
            return NameRu;
        return NameOriginal ?? NameEn ?? NameRu;
    }
}

/// <summary>
/// Search response.
/// </summary>
public class KinopoiskSearchResponse
{
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    [JsonPropertyName("pagesCount")]
    public int PagesCount { get; set; }

    [JsonPropertyName("searchFilmsCountResult")]
    public int SearchFilmsCountResult { get; set; }

    [JsonPropertyName("films")]
    public List<KinopoiskSearchItem>? Films { get; set; }
}

/// <summary>
/// Staff/Person data.
/// </summary>
public class KinopoiskStaff
{
    [JsonPropertyName("staffId")]
    public int StaffId { get; set; }

    [JsonPropertyName("nameRu")]
    public string? NameRu { get; set; }

    [JsonPropertyName("nameEn")]
    public string? NameEn { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("posterUrl")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("professionText")]
    public string? ProfessionText { get; set; }

    [JsonPropertyName("professionKey")]
    public string? ProfessionKey { get; set; }

    public string? GetName(bool preferRussian)
    {
        if (preferRussian && !string.IsNullOrEmpty(NameRu))
            return NameRu;
        return NameEn ?? NameRu;
    }
}

/// <summary>
/// Person details.
/// </summary>
public class KinopoiskPerson
{
    [JsonPropertyName("personId")]
    public int PersonId { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    [JsonPropertyName("nameRu")]
    public string? NameRu { get; set; }

    [JsonPropertyName("nameEn")]
    public string? NameEn { get; set; }

    [JsonPropertyName("sex")]
    public string? Sex { get; set; }

    [JsonPropertyName("posterUrl")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("growth")]
    public int? Growth { get; set; }

    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }

    [JsonPropertyName("death")]
    public string? Death { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("birthplace")]
    public string? Birthplace { get; set; }

    [JsonPropertyName("deathplace")]
    public string? Deathplace { get; set; }

    [JsonPropertyName("profession")]
    public string? Profession { get; set; }

    [JsonPropertyName("facts")]
    public List<string>? Facts { get; set; }

    [JsonPropertyName("films")]
    public List<KinopoiskPersonFilm>? Films { get; set; }

    public string? GetName(bool preferRussian)
    {
        if (preferRussian && !string.IsNullOrEmpty(NameRu))
            return NameRu;
        return NameEn ?? NameRu;
    }
}

/// <summary>
/// Film reference in person data.
/// </summary>
public class KinopoiskPersonFilm
{
    [JsonPropertyName("filmId")]
    public int FilmId { get; set; }

    [JsonPropertyName("nameRu")]
    public string? NameRu { get; set; }

    [JsonPropertyName("nameEn")]
    public string? NameEn { get; set; }

    [JsonPropertyName("rating")]
    public string? Rating { get; set; }

    [JsonPropertyName("professionKey")]
    public string? ProfessionKey { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Season data for TV series.
/// </summary>
public class KinopoiskSeasonsResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<KinopoiskSeason>? Items { get; set; }
}

/// <summary>
/// Season details.
/// </summary>
public class KinopoiskSeason
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("episodes")]
    public List<KinopoiskEpisode>? Episodes { get; set; }
}

/// <summary>
/// Episode details.
/// </summary>
public class KinopoiskEpisode
{
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("nameRu")]
    public string? NameRu { get; set; }

    [JsonPropertyName("nameEn")]
    public string? NameEn { get; set; }

    [JsonPropertyName("synopsis")]
    public string? Synopsis { get; set; }

    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }

    public string? GetName(bool preferRussian)
    {
        if (preferRussian && !string.IsNullOrEmpty(NameRu))
            return NameRu;
        return NameEn ?? NameRu;
    }
}

/// <summary>
/// Image/frames response.
/// </summary>
public class KinopoiskImagesResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("items")]
    public List<KinopoiskImage>? Items { get; set; }
}

/// <summary>
/// Image data.
/// </summary>
public class KinopoiskImage
{
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("previewUrl")]
    public string? PreviewUrl { get; set; }
}

/// <summary>
/// Video/trailer data.
/// </summary>
public class KinopoiskVideosResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<KinopoiskVideo>? Items { get; set; }
}

/// <summary>
/// Video item.
/// </summary>
public class KinopoiskVideo
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("site")]
    public string? Site { get; set; }
}
