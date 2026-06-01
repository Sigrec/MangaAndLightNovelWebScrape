namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Thrown by <see cref="MasterScrape.InitializeScrapeAsync"/> when the underlying Playwright
/// browser fails to launch — typically because the requested browser channel (msedge/chrome)
/// isn't installed on the host, or the Playwright Node driver couldn't start.
/// <para>
/// Catch this distinctly from <see cref="SiteScrapeException"/>: a launch failure is
/// catastrophic (no site can be scraped) and is usually a host-setup issue, while a site
/// failure is a per-site condition like a rate limit or schema change.
/// </para>
/// </summary>
public sealed class ScrapeBrowserLaunchException : ScrapeException
{
    /// <summary>
    /// Wraps the underlying Playwright launch failure — typically a missing browser
    /// channel install, a missing Node driver, or a sandbox/permissions issue on the host.
    /// </summary>
    public ScrapeBrowserLaunchException(Exception innerException)
        : base($"Failed to launch the Playwright browser: {innerException.Message}", innerException)
    {
    }
}
