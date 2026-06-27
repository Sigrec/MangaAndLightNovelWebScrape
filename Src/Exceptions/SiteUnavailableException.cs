namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Recorded in <see cref="MasterScrape.Errors"/> when
/// <see cref="MasterScrape.SkipUnavailableSites"/> is enabled and the pre-flight
/// reachability probe for the site returned <c>false</c>.
/// <para>
/// Distinct from <see cref="SiteScrapeException"/>: the scrape never ran for this
/// site — it was skipped before fan-out because the host didn't respond.
/// </para>
/// </summary>
public sealed class SiteUnavailableException : ScrapeException
{
    /// <summary>The website that was skipped.</summary>
    public Website Site { get; }

    public SiteUnavailableException(Website site)
        : base($"{site} was skipped — pre-flight reachability check failed.")
    {
        Site = site;
    }
}
