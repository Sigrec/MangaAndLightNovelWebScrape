namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Wraps a failure that originated inside a single site's scrape. Surfaces via
/// <see cref="MasterScrape.Errors"/> after <see cref="MasterScrape.InitializeScrapeAsync"/>
/// completes — per-site failures do not abort the whole scrape, they're recorded for the
/// consumer to inspect.
/// </summary>
public sealed class SiteScrapeException : ScrapeException
{
    /// <summary>The website that failed.</summary>
    public Website Site { get; }

    public SiteScrapeException(Website site, Exception innerException)
        : base($"Scrape failed for {site}: {innerException.Message}", innerException)
    {
        Site = site;
    }
}
