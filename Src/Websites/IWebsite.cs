using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

/// <summary>
/// Contract for a per-site scraper. Implementations are constructed once via
/// <c>InternalHelpers.CreateScraper</c> and dispatched in parallel from <c>ScheduleScrapes</c>.
/// </summary>
/// <remarks>
/// Each implementation also exposes <c>public const string TITLE</c>, <c>BASE_URL</c>, and
/// <c>public const Region REGION</c>. These aren't part of the interface — call sites that
/// need them reference the concrete type directly (e.g. <c>AmazonUSA.TITLE</c>) or look them
/// up via <see cref="Helpers.WebsiteTitleMap"/> / <see cref="Helpers.WebsitesByRegion"/>.
/// </remarks>
public interface IWebsite
{
    /// <summary>
    /// Builds and runs a scrape for the given title/book-type, appending results to the master
    /// collections. Should be invoked through <c>Task.WhenAll</c> alongside other sites.
    /// Per-site failures are caught by the shared helper and recorded in
    /// <paramref name="errors"/>; one site failing never aborts the rest of the scrape.
    /// </summary>
    Task CreateTask(
        string bookTitle,
        BookType bookType,
        ConcurrentBag<List<EntryModel>> masterDataList,
        ConcurrentDictionary<Website, string> masterLinkList,
        ConcurrentDictionary<Website, Exception> errors,
        IBrowser? browser,
        Region curRegion,
        Membership memberships = Membership.None,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the actual scrape work and returns the parsed entries plus the page URLs visited.
    /// </summary>
    Task<(List<EntryModel> Data, List<string> Links)> GetData(
        string bookTitle,
        BookType bookType,
        IPage? page = null,
        bool isMember = false,
        Region curRegion = Region.America,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reachability probe — returns <c>true</c> when the site's host resolves and the
    /// server answers with any non-5xx status. DNS failures, connection refusals,
    /// timeouts, and 5xx responses all return <c>false</c>. CDN challenges (Cloudflare
    /// interstitials) and auth gates (401/403) count as "up": the server is alive,
    /// even if the listing endpoint requires further work to reach.
    /// </summary>
    /// <remarks>
    /// Useful as a pre-flight before <see cref="CreateTask"/> when you want to skip
    /// dead sites instead of waiting for the scrape's own timeout. Each implementation
    /// delegates to <c>MangaAndLightNovelWebScrape.Services.SiteHealth.IsReachableAsync</c>
    /// with its own <c>BASE_URL</c>.
    /// </remarks>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
