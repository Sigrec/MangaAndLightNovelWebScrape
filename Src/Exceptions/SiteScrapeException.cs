namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Wraps a failure that originated inside a single site's scrape. Surfaces via
/// <see cref="MasterScrape.Errors"/> after <see cref="MasterScrape.InitializeScrapeAsync"/>
/// completes — per-site failures do not abort the whole scrape, they're recorded for the
/// consumer to inspect.
/// </summary>
public sealed class SiteScrapeException : ScrapeException
{
    /// <summary>The website whose scrape threw.</summary>
    public Website Site { get; }

    /// <summary>
    /// Builds a wrapper for a single-site failure. The <paramref name="innerException"/>
    /// is whatever the site's <c>GetData</c> threw — typically a Playwright timeout, an
    /// HTTP error, or a parsing exception.
    /// </summary>
    public SiteScrapeException(Website site, Exception innerException)
        : base($"Scrape failed for {site}: {innerException.Message}", innerException)
    {
        Site = site;
    }
}
