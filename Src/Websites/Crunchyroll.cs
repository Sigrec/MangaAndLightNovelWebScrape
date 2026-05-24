using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class Crunchyroll : IWebsite
{
    private readonly ILogger _logger;

    public Crunchyroll(ILogger<Crunchyroll>? logger = null)
    {
        _logger = logger ?? NullLogger<Crunchyroll>.Instance;
    }

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='tab-content']//div[@class='pdp-link']/a");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//div[@class='tab-content']//span[@class='sales']/span");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='tab-content']//div[@class='product-tile js-product-tile']//div[@class='image-container']//div[@class='product-sashes']");

    /// <inheritdoc />
    public const string TITLE = "Crunchyroll";

    /// <inheritdoc />
    public const string BASE_URL = "https://store.crunchyroll.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    [GeneratedRegex(@"Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@",|\(.*?\)| Manga| Graphic Novel|:|(?<=(?:Vol|Box Set)\s+\d{1,3}(?:\.\d)?\s+).*|Hardcover", RegexOptions.IgnoreCase)] private static partial Regex ParseAndCleanTitleRegex();
    [GeneratedRegex(@",| Manga| Graphic Novel|:|(?:Vol|Box Set)\s+\d{1,3}(\.\d)?[^\d]+.*|Hardcover", RegexOptions.IgnoreCase)] private static partial Regex BundleParseRegex();
    [GeneratedRegex(@"(?:\d-in-\d|Omnibus) Edition", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"\((\d{1,3}-\d{1,3})\) Bundle", RegexOptions.IgnoreCase)] private static partial Regex BundleVolRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunHtmlScrapeAsync(
            this, Website.Crunchyroll, bookTitle, bookType, masterDataList, masterLinkList, errors, curRegion, cancellationToken);

    private string GenerateWebsiteUrl(BookType bookType, string bookTitle, bool retry = false)
    {
        // https://store.crunchyroll.com/search?q=naruto&prefn1=subcategory&prefv1=Light%20Novels
        // https://store.crunchyroll.com/collections/jujutsu-kaisen/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles
        // https://store.crunchyroll.com/collections/one-piece/?cgid=one-piece&prefn1=category&prefv1=Manga%20%26%20Books&sz=200
        // https://store.crunchyroll.com/collections/one-piece/?cgid=one-piece&prefn1=category&prefv1=Manga%20%26%20Books&start=100&sz=100
        // https://store.crunchyroll.com/search?q=Akane-Banashi&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga&sz={maxTotalProducts}
        
        bookTitle = InternalHelpers.FilterBookTitle(bookTitle);
        string url = bookType == BookType.Manga
            ? (retry
                ? $"{BASE_URL}/collections/{bookTitle}/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz={int.MaxValue}"
                : $"{BASE_URL}/search?q={bookTitle}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz={int.MaxValue}")
            : (retry
                ? $"{BASE_URL}/collections/{bookTitle}/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Novels&sz={int.MaxValue}" :
                $"{BASE_URL}/search?q={bookTitle}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Novels&sz={int.MaxValue}");

        _logger.UrlGenerated(url);
        return url;
    }

    private static string ParseAndCleanTitle(string entryTitle, string baseTitleText, string bookTitle, BookType bookType)
    {
        StringBuilder curTitle;

        // Check if we need to replace "Omnibus" or "Bundle"
        if (OmnibusRegex().IsMatch(entryTitle))
        {
            curTitle = new StringBuilder(OmnibusRegex().Replace(entryTitle, "Omnibus"));
        }
        else if (!bookTitle.Contains("Bundle") && entryTitle.Contains("Bundle"))
        {
            curTitle = new StringBuilder(BundleVolRegex().Replace(entryTitle, "Bundle Vol $1"));
        }
        else
        {
            curTitle = new StringBuilder(entryTitle);
        }

        // Perform specific changes for Manga books
        if (bookType == BookType.Manga)
        {
            if (entryTitle.Contains("Deluxe Edition"))
            {
                curTitle.Replace("Omnibus ", string.Empty).Replace("Deluxe Edition", "Deluxe");
            }

            if (entryTitle.Contains("with Playing Cards", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(" with Playing Cards", string.Empty);
                int index = MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index;
                if (index > 0)
                {
                    curTitle.Insert(index, "Special Edition Vol ");
                }
            }

            if (!entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set"))
            {
                Match volMatch = MasterScrape.FindVolNumRegex().Match(entryTitle);
                if (volMatch.Success)
                {
                    curTitle.Insert(volMatch.Index, "Vol ");
                }
            }

            // Single snapshot read, two Contains checks — old code materialized curTitle twice.
            if (bookTitle.Equals("attack on titan", StringComparison.OrdinalIgnoreCase)
                && baseTitleText.Contains("(Hardcover)"))
            {
                string snapshot = curTitle.ToString();
                if (!snapshot.Contains("In Color") && !snapshot.Contains("Color Edition"))
                {
                    curTitle.Append(" In Color");
                }
            }
        }
        else if (bookType == BookType.LightNovel && !entryTitle.Contains("Novel"))
        {
            // One IndexOf instead of Contains + IndexOf — same scan, sentinel value for the
            // "not found" branch.
            int volIndex = entryTitle.IndexOf("Vol");
            if (volIndex >= 0)
            {
                curTitle.Insert(volIndex, "Novel ");
            }
            else
            {
                curTitle.Append(" Novel");
            }
        }

        // Remove unwanted characters from the title
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '-', "Bundle");
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

        // Final cleanup and return
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            HtmlWeb html = HtmlFactory.CreateWeb();

            bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            _logger.BookTitleRemovalCheck(bookTitleRemovalCheck);

            string url = GenerateWebsiteUrl(bookType, bookTitle);
            links.Add(url);

            HtmlDocument doc = await html.LoadFromWebAsync(url);
            doc.ConfigurePerf();

            HtmlNodeCollection? titleData = doc.DocumentNode.SelectNodes(TitleXPath);
            HtmlNodeCollection? priceData = doc.DocumentNode.SelectNodes(PriceXPath);
            HtmlNodeCollection? stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);

            // The /search?q= URL sometimes returns no results for a series that DOES have its
            // own /collections/ landing page (slug-based). Crunchyroll signals "no results" by
            // returning null for ALL three XPaths. A partial null is normal — e.g. the
            // product-sashes badge XPath only matches items with a sale/OOS sash, so a
            // listing where every product is plain in-stock has null stockStatusData. AND
            // here (not OR) so we only retry when there's genuinely nothing.
            if (titleData == null && priceData == null && stockStatusData == null)
            {
                _logger.TryingSecondLink();
                links.Clear();

                url = GenerateWebsiteUrl(bookType, bookTitle, true);
                links.Add(url);
                doc = await html.LoadFromWebAsync(url);
                doc.ConfigurePerf();
                titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
            }

            // titleData is the canonical "has products?" signal — every product carries a title.
            // priceData and stockStatusData can be partially null (or fully null in the case of
            // an in-stock listing where no item has a sale badge). Guard accesses against those
            // below rather than bailing here.
            if (titleData is null)
            {
                _logger.NoResultsAfterRetry(bookTitle, bookType);
                return (data, links);
            }

            // priceData and stockStatusData may be shorter than titleData (or null). The loop
            // bounds against titleData and guards the others per-entry.
            int entryCount = titleData.Count;
            for (int x = 0; x < entryCount; x++)
            {
                string entryTitle = WebUtility.HtmlDecode(titleData[x].InnerText.Trim());
                // First check: does the book title contain the entry title?
                if (!InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle))
                {
                    _logger.EntryRemovedDebug(1, entryTitle);
                    continue;
                }

                // Second check: Is the entry title removed based on the regex or the removal flag?
                if (InternalHelpers.ShouldRemoveEntry(entryTitle) && !bookTitleRemovalCheck)
                {
                    _logger.EntryRemovedDebug(2, entryTitle);
                    continue;
                }

                bool shouldRemoveEntry = false;
                if (bookType == BookType.Manga)
                {
                    shouldRemoveEntry =
                        (!bookTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)) ||
                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, ["of Gluttony", "Darkness Ink"]) ||
                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto") ||
                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Pirate Recipes");
                }
                else if (bookType == BookType.LightNovel)
                {
                    shouldRemoveEntry = InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented");
                }

                if (!shouldRemoveEntry)
                {
                    entryTitle = FixVolumeRegex().Replace(entryTitle, "Vol");

                    // Cache the Bundle check — used to pick the parsing regex AND to decide
                    // post-processing inside ParseAndCleanTitle.
                    bool isBundle = entryTitle.Contains("Bundle");
                    entryTitle = isBundle
                        ? BundleParseRegex().Replace(entryTitle, string.Empty)
                        : ParseAndCleanTitleRegex().Replace(entryTitle, string.Empty);

                    string cleanedTitle = ParseAndCleanTitle(entryTitle, titleData[x].InnerText, bookTitle, bookType);

                    // stockStatusData is null when no item on the page has a sale/OOS sash —
                    // i.e. everything is plain in-stock. Treat missing sash as in-stock.
                    string stockStatusText = stockStatusData is not null && x < stockStatusData.Count
                        ? stockStatusData[x].SelectSingleNode("./div/span")?.InnerText.Trim() ?? string.Empty
                        : string.Empty;
                    StockStatus stockStatus = stockStatusText switch
                    {
                        "SOLD-OUT" => StockStatus.OOS,
                        "PRE-ORDER" => StockStatus.PO,
                        "Back Order" => StockStatus.BO,
                        "COMING-SOON" => StockStatus.CS,
                        _ => StockStatus.IS,
                    };

                    // priceData *should* align 1:1 with titleData. Guard against partial
                    // misalignment rather than crash; missing-price entries get $ERROR which
                    // the dedup/sort downstream will surface clearly.
                    string priceContent = priceData is not null && x < priceData.Count
                        ? priceData[x].GetAttributeValue("content", "ERROR")
                        : "ERROR";

                    data.Add(new EntryModel(cleanedTitle, $"${priceContent}", stockStatus, TITLE));
                }
                else
                {
                    _logger.EntryRemovedDebug(3, entryTitle);
                }
            }
        }
        finally
        {
            data.TrimExcess();
            links.TrimExcess();
            data.SortByVolume();
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        }
        
        return (data, links);
    }
}