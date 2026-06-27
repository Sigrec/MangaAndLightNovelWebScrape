using System.Net.Http;

namespace MangaAndLightNovelWebScrape.Services;

/// <summary>
/// Lightweight reachability probe for site root URLs. Per-site
/// <see cref="MangaAndLightNovelWebScrape.Websites.IWebsite.IsAvailableAsync"/>
/// implementations delegate here so the HEAD+timeout logic stays in one place.
/// </summary>
public static class SiteHealth
{
    // Shared HttpClient — Per-site checks are infrequent and read-only, so a single
    // process-wide instance avoids socket churn. 5 s is generous for HEAD round-trips
    // but short enough that a hung scrape doesn't stall a multi-site availability sweep.
    private static readonly HttpClient _client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
        ConnectTimeout = TimeSpan.FromSeconds(5),
    })
    {
        Timeout = TimeSpan.FromSeconds(5),
        DefaultRequestHeaders =
        {
            // Identify as a modern browser — many CDNs reject HEAD requests from the
            // default .NET UA outright, which would make "is the site up?" return
            // false negatives.
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36" }
        }
    };

    /// <summary>
    /// Returns <c>true</c> when the URL's host resolves AND the server answers with any
    /// non-5xx status. 5xx, DNS failures, connection refusals, and timeouts all return
    /// <c>false</c>. Treats 4xx and CDN challenge responses (Cloudflare interstitials,
    /// rate limits) as "up" — the site is functional, just gated.
    /// </summary>
    public static async Task<bool> IsReachableAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Head, url);
            using HttpResponseMessage response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Some servers reject HEAD with 405 Method Not Allowed. Re-probe with GET
            // (no body read) to confirm the site is actually reachable, not just HEAD-hostile.
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                using HttpRequestMessage getReq = new(HttpMethod.Get, url);
                using HttpResponseMessage getResp = await _client.SendAsync(
                    getReq,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);
                return (int)getResp.StatusCode < 500;
            }

            return (int)response.StatusCode < 500;
        }
        catch (HttpRequestException)
        {
            // DNS resolution failure, connection refused, TLS handshake failure, etc.
            return false;
        }
        catch (TaskCanceledException)
        {
            // Timeout — the per-request timeout fires by canceling the task.
            return false;
        }
    }
}
