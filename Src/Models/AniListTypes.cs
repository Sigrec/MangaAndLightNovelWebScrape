using System.Text.Json.Serialization;

namespace MangaAndLightNovelWebScrape.Models;

/// <summary>
/// GraphQL request body sent to AniList (<c>https://graphql.anilist.co</c>) by
/// <see cref="TranslateAPI"/>. Source-generated JSON serialization via
/// <see cref="AniListJsonContext"/> so no reflection is needed at runtime.
/// </summary>
internal sealed class AniListRequest
{
    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;

    [JsonPropertyName("variables")]
    public AniListVariables Variables { get; init; } = new();
}

/// <summary>
/// Variables block for the AniList <c>Media(search, format)</c> query. Property names are
/// lowercase to match the GraphQL operation's variable names.
/// </summary>
internal sealed class AniListVariables
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; init; } = string.Empty;
}

/// <summary>
/// Top-level GraphQL response envelope. The <c>errors</c> array is intentionally not
/// modeled — the caller catches deserialization / null-access failures and logs them
/// via <see cref="ILogger"/>.
/// </summary>
internal sealed class AniListResponse
{
    [JsonPropertyName("data")]
    public AniListData? Data { get; init; }
}

internal sealed class AniListData
{
    // AniList capitalizes the operation name in the response — the JSON property is
    // literally "Media" (capital M).
    [JsonPropertyName("Media")]
    public AniListMedia? Media { get; init; }
}

internal sealed class AniListMedia
{
    [JsonPropertyName("title")]
    public AniListTitle? Title { get; init; }
}

internal sealed class AniListTitle
{
    [JsonPropertyName("english")]
    public string? English { get; init; }

    [JsonPropertyName("romaji")]
    public string? Romaji { get; init; }

    [JsonPropertyName("native")]
    public string? Native { get; init; }
}
