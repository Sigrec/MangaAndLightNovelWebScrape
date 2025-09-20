using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

/// <summary>
/// Contract for a website scraper implementation, providing both static compile-time
/// metadata and instance methods for scraping operations.
/// </summary>
public interface IWebsite
{
    /// <summary>
    /// The display title of the website.
    /// Implementations should typically return a compile‑time const string.
    /// </summary>
    static string TITLE { get; }

    /// <summary>
    /// The base URL of the website.
    /// Implementations should typically return a compile‑time const string.
    /// </summary>
    static string BASE_URL { get; }

    /// <summary>
    /// The region this website serves.
    /// Implementations should typically return a compile‑time const Region.
    /// </summary>
    static Region REGION { get; }

    /// <summary>
    /// Asynchronously creates or enqueues a scraping task for the given title and book type.
    /// Must add data to the master data and link collections in this method
    /// </summary>
    Task CreateTask(
        string bookTitle,
        BookType bookType,
        ConcurrentBag<List<EntryModel>> masterDataList,
        ConcurrentDictionary<Website, string> masterLinkList,
        IBrowser? browser,
        Region curRegion,
        (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships = default);

    /// <summary>
    /// Performs a synchronous scrape and returns the scraped entries.
    /// </summary>
    Task<(List<EntryModel> Data, List<string> Links)> GetData(
        string bookTitle,
        BookType bookType,
        IPage? page = null,
        bool isMember = false,
        Region curRegion = Region.America);
}