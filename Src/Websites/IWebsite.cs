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
    /// </summary>
    Task CreateTask(
        string bookTitle,
        BookType bookType,
        ConcurrentBag<List<EntryModel>> masterDataList,
        ConcurrentDictionary<Website, string> masterLinkList,
        IBrowser? browser,
        Region curRegion,
        Membership memberships = Membership.None);

    /// <summary>
    /// Performs the actual scrape work and returns the parsed entries plus the page URLs visited.
    /// </summary>
    Task<(List<EntryModel> Data, List<string> Links)> GetData(
        string bookTitle,
        BookType bookType,
        IPage? page = null,
        bool isMember = false,
        Region curRegion = Region.America);
}
