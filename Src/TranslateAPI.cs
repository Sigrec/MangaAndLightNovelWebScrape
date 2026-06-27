using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MangaAndLightNovelWebScrape.Models;

namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Title translation against the AniList GraphQL endpoint. Three static methods convert a
/// manga or light-novel title between English, Romaji, and Japanese.
/// <para>
/// The <see cref="IDisposable"/> surface is preserved for backward compatibility but is a
/// no-op — every method is static and the underlying <see cref="HttpClient"/> is process-lifetime.
/// Disposing an instance does not affect any in-flight or future calls.
/// </para>
/// </summary>
public partial class TranslateAPI : IDisposable
{
    /// <summary>
    /// Optional logger used by <see cref="TranslateAPI"/> static methods.
    /// Defaults to <see cref="NullLogger.Instance"/>; callers can assign their own.
    /// </summary>
    public static ILogger _logger { get; set; } = NullLogger.Instance;

    private const string ANILIST_ENDPOINT = "https://graphql.anilist.co";
    private const string USER_AGENT =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    // Process-lifetime HttpClient — AniList is one endpoint and queries are infrequent, so a
    // single shared client avoids socket churn and lets the connection pool warm.
    private static readonly HttpClient _client = CreateClient();

    private static HttpClient CreateClient()
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);
        return client;
    }

    // Queries match the AniList schema — `relations { edges, nodes }` is left in the request
    // body (server doesn't care about extra fields in the response, and removing it would be
    // a behavior change in this version).
    private const string QUERY_ENGLISH = @"query ($title: String, $format: MediaFormat) {
                                Media(search: $title, format: $format) {
                                  title {
                                    english
                                  }
                                  relations {
                                    edges {
                                      relationType(version: 2)
                                    }
                                    nodes {
                                        title {
                                          native
                                        }
                                      }
                                    }
                                  }
                                }";

    private const string QUERY_ROMAJI = @"query ($title: String, $format: MediaFormat) {
                            Media(search: $title, format: $format) {
                                title {
                                romaji
                                }
                                relations {
                                edges {
                                    relationType(version: 2)
                                }
                                nodes {
                                    title {
                                        native
                                    }
                                    }
                                }
                                }
                            }";

    private const string QUERY_JAPANESE = @"query ($title: String, $format: MediaFormat) {
                            Media(search: $title, format: $format) {
                                title {
                                native
                                }
                                relations {
                                edges {
                                    relationType(version: 2)
                                }
                                nodes {
                                    title {
                                        native
                                    }
                                    }
                                }
                                }
                            }";

    /// <summary>
    /// Translates a given manga or light novel title from Japanese or Romaji to English.
    /// </summary>
    /// <param name="title">The title of the series. Any synonym AniList recognizes is valid.</param>
    /// <param name="format">The media format — typically <c>MANGA</c> or <c>NOVEL</c>.</param>
    /// <returns>The English title if AniList knows one; <c>null</c> on miss or failure.</returns>
    public static Task<string?> ToEnglish(string title, string format)
        => QueryTitleAsync(QUERY_ENGLISH, title, format, static t => t.English);

    /// <summary>
    /// Translates a given manga or light novel title from Japanese or English to Romaji.
    /// </summary>
    public static Task<string?> ToRomaji(string title, string format)
        => QueryTitleAsync(QUERY_ROMAJI, title, format, static t => t.Romaji);

    /// <summary>
    /// Translates a given manga or light novel title from Romaji or English to Japanese.
    /// </summary>
    public static Task<string?> ToJapanese(string title, string format)
        => QueryTitleAsync(QUERY_JAPANESE, title, format, static t => t.Native);

    /// <summary>
    /// Shared implementation: build a GraphQL request, POST it, observe rate-limit headers,
    /// retry once if throttled, then pluck a language-specific field from the response.
    /// </summary>
    private static async Task<string?> QueryTitleAsync(
        string query,
        string title,
        string format,
        Func<AniListTitle, string?> selector)
    {
        try
        {
            AniListRequest request = new()
            {
                Query = query,
                Variables = new AniListVariables { Title = title, Format = format }
            };

            HttpResponseMessage response = await SendQueryAsync(request).ConfigureAwait(false);

            short rateCheck = RateLimitCheck(response.Headers);
            if (rateCheck != -1)
            {
                _logger.WaitingForRateLimit(rateCheck);
                await Task.Delay(TimeSpan.FromSeconds(rateCheck)).ConfigureAwait(false);
                response.Dispose();
                response = await SendQueryAsync(request).ConfigureAwait(false);
            }

            using (response)
            {
                AniListResponse? envelope = await response.Content
                    .ReadFromJsonAsync(AniListJsonContext.Default.AniListResponse)
                    .ConfigureAwait(false);

                AniListTitle? titleObj = envelope?.Data?.Media?.Title;
                return titleObj is null ? null : selector(titleObj);
            }
        }
        catch (Exception e)
        {
            _logger.AniListRequestFailed(title, e.Message);
            return null;
        }
    }

    /// <summary>
    /// Serializes <paramref name="request"/> via the source-generated context and POSTs it
    /// to the AniList endpoint. Callers own the returned response and must dispose it.
    /// </summary>
    private static async Task<HttpResponseMessage> SendQueryAsync(AniListRequest request)
    {
        string json = JsonSerializer.Serialize(request, AniListJsonContext.Default.AniListRequest);
        using StringContent content = new(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(ANILIST_ENDPOINT, content).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads AniList's rate-limit headers and returns the number of seconds to back off when
    /// the remaining-request count has reached zero. Returns <c>-1</c> when the request can
    /// proceed immediately.
    /// </summary>
    private static short RateLimitCheck(HttpResponseHeaders responseHeaders)
    {
        responseHeaders.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string>? rateRemainingValues);
        _ = short.TryParse(rateRemainingValues?.FirstOrDefault(), out short rateRemaining);
        _logger.RateRemaining(rateRemaining);
        if (rateRemaining > 0)
        {
            return -1;
        }

        responseHeaders.TryGetValues("Retry-After", out IEnumerable<string>? retryAfter);
        _ = short.TryParse(retryAfter?.FirstOrDefault(), out short retryAfterInSeconds);
        return retryAfterInSeconds;
    }

    // ─── IDisposable surface (no-op; preserved for backward compatibility) ───────────
    //
    // The old code disposed a static GraphQLHttpClient from an instance's Dispose, which
    // would have destroyed the shared client for every other caller in the same process —
    // a bug that's now moot because the HttpClient is process-lifetime and not held by
    // any instance.

    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;
        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
