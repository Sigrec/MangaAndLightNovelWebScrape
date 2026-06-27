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

    // The Crunchyroll store rebuilt its DOM around emotion-css / chakra-ui classes; the
    // old `.tab-content > .pdp-link > a` shape is gone. Stable anchors now are:
    //   - product-tile container w/ data-pid attribute
    //   - aria-label="View details for ..." on the title <a>
    //   - <b class="current-price" data-price="N.NN"> for the price
    //   - <p class="preandbackOrderAvailability"> shows "Release date : M/D/YYYY" for pre-orders
    //     (empty for in-stock items)
    private static readonly XPathExpression _productTileXPath =
        XPathExpression.Compile("//div[contains(@class,'product-tile') and @data-pid]");
    private const string _titleRel = ".//a[starts-with(@aria-label,'View details for')]//p";
    private const string _priceRel = ".//b[contains(@class,'current-price')]";
    private const string _availabilityRel = ".//p[contains(@class,'preandbackOrderAvailability')]";

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

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    internal string GenerateWebsiteUrl(BookType bookType, string bookTitle, bool retry = false)
    {
        // Manga: filter by category=Manga&Books + subcategory=Specialty Books|Manga|Bundles
        //   https://store.crunchyroll.com/search?q=jujutsu%20kaisen&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz=2147483647
        // LightNovel: filter by subcategory=Novels via the newer `refine=` param shape
        //   https://store.crunchyroll.com/search?q=classroom%20of%20the%20elite&refine=c_subcategory%3DNovels&sort=best-matches
        // Retry path uses /collections/{slug}/ where slug is kebab-case lowercase — NOT
        // a percent-encoded query string. Encoding the path produces a 404-like empty page.

        string encoded = InternalHelpers.FilterBookTitle(bookTitle);
        string slug = bookTitle.Trim().ToLowerInvariant().Replace(' ', '-');

        string url = bookType == BookType.Manga
            ? (retry
                ? $"{BASE_URL}/collections/{slug}/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz={int.MaxValue}"
                : $"{BASE_URL}/search?q={encoded}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz={int.MaxValue}")
            : (retry
                ? $"{BASE_URL}/collections/{slug}/?refine=c_subcategory%3DNovels&sort=best-matches&sz={int.MaxValue}"
                : $"{BASE_URL}/search?q={encoded}&refine=c_subcategory%3DNovels&sort=best-matches&sz={int.MaxValue}");

        _logger.UrlGenerated(url);
        return url;
    }

    private const string _userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    [System.Diagnostics.Conditional("DEBUG")]
    private static void DumpDebugHtml(string html, string label)
    {
        try
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"Crunchyroll_{label}.html"), html ?? string.Empty);
        }
        catch
        {
            // Diagnostic-only.
        }
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
            html.PreRequest += req =>
            {
                // Crunchyroll's CDN returns an empty product shell to default HtmlWeb UA
                // (which advertises as IE6). A modern UA gets the populated HTML.
                req.UserAgent = _userAgent;
                return true;
            };

            _logger.BookTitleRemovalCheck(InternalHelpers.ShouldRemoveEntry(bookTitle));

            string url = GenerateWebsiteUrl(bookType, bookTitle);
            links.Add(url);

            HtmlDocument doc = await html.LoadFromWebAsync(url).ConfigureAwait(false);
            doc.ConfigurePerf();
            DumpDebugHtml(doc.Text, "search");

            // Search URL sometimes returns no results for a series that DOES have a
            // /collections/{slug}/ landing page. Retry once before giving up.
            if (HasProducts(doc) == 0)
            {
                _logger.TryingSecondLink();
                links.Clear();

                url = GenerateWebsiteUrl(bookType, bookTitle, true);
                links.Add(url);
                doc = await html.LoadFromWebAsync(url).ConfigureAwait(false);
                doc.ConfigurePerf();
                DumpDebugHtml(doc.Text, "collections");
            }

            if (HasProducts(doc) == 0)
            {
                _logger.NoResultsAfterRetry(bookTitle, bookType);
                return (data, links);
            }

            data = ParseProducts(doc, bookTitle, bookType);
        }
        finally
        {
            links.TrimExcess();
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        }

        return (data, links);
    }

    /// <summary>
    /// Counts product tiles in a pre-loaded listing document. Exposed as <c>internal</c> so
    /// fixture-based tests and the retry path in <see cref="GetData"/> share a single
    /// definition of "did this URL return anything".
    /// </summary>
    internal static int HasProducts(HtmlDocument doc)
        => doc.DocumentNode.SelectNodes(_productTileXPath.Expression)?.Count ?? 0;

    /// <summary>
    /// Parses one Crunchyroll listing page into <see cref="EntryModel"/>s. Holds the entire
    /// per-tile filter / removal / clean-title pipeline so that fixture-based tests can
    /// drive the same code path <see cref="GetData"/> uses without network I/O.
    /// </summary>
    internal List<EntryModel> ParseProducts(HtmlDocument doc, string bookTitle, BookType bookType)
    {
        List<EntryModel> data = [];

        HtmlNodeCollection? products = doc.DocumentNode.SelectNodes(_productTileXPath.Expression);
        if (products is null || products.Count == 0) return data;

        bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);

        foreach (HtmlNode tile in products)
        {
            HtmlNode? titleNode = tile.SelectSingleNode(_titleRel);
            if (titleNode is null) continue;

            string entryTitle = WebUtility.HtmlDecode(titleNode.InnerText.Trim());

            if (!InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle))
            {
                _logger.EntryRemovedDebug(1, entryTitle);
                continue;
            }

            // The site no longer enforces category/subcategory filters server-side (they're
            // applied client-side via JS, which HtmlWeb can't run). The results page returns
            // figures / t-shirts / desk mats alongside books, so reject anything that doesn't
            // carry a book-like volume marker.
            bool isBookProduct = bookType == BookType.Manga
                ? entryTitle.ContainsAny(["Manga", "Vol ", "Volume ", "Box Set", "Omnibus", "Bundle"])
                : entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase);
            if (!isBookProduct)
            {
                _logger.EntryRemovedDebug(4, entryTitle);
                continue;
            }

            // Crunchyroll sells variant/exclusive editions of mainline manga volumes (e.g.
            // "Jujutsu Kaisen Variant Cover Manga Volume 30 - Crunchyroll Exclusive"). The
            // global removal list contains "Exclusive" — strip the marker before the removal
            // check so the entry survives, then re-append it as a suffix after the title is
            // parsed.
            bool isCrunchyrollExclusive = entryTitle.Contains("Crunchyroll Exclusive", StringComparison.OrdinalIgnoreCase);
            string titleForRemovalCheck = isCrunchyrollExclusive
                ? entryTitle.Replace("Crunchyroll Exclusive", string.Empty, StringComparison.OrdinalIgnoreCase)
                : entryTitle;

            if (InternalHelpers.ShouldRemoveEntry(titleForRemovalCheck) && !bookTitleRemovalCheck)
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

            if (shouldRemoveEntry)
            {
                _logger.EntryRemovedDebug(3, entryTitle);
                continue;
            }

            string baseTitleText = titleNode.InnerText;
            entryTitle = FixVolumeRegex().Replace(entryTitle, "Vol");

            bool isBundle = entryTitle.Contains("Bundle");
            entryTitle = isBundle
                ? BundleParseRegex().Replace(entryTitle, string.Empty)
                : ParseAndCleanTitleRegex().Replace(entryTitle, string.Empty);

            string cleanedTitle = ParseAndCleanTitle(entryTitle, baseTitleText, bookTitle, bookType);

            if (isCrunchyrollExclusive)
            {
                // The strip pipeline drops "Crunchyroll Exclusive" (it lives after the vol
                // number, which the regex truncates). Re-append it as a suffix and drop the
                // redundant "Variant Cover" tag — the Exclusive marker alone is enough to
                // disambiguate this from the mainline edition.
                cleanedTitle = cleanedTitle
                    .Replace("Variant Cover ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .TrimEnd();
                cleanedTitle = $"{cleanedTitle} Crunchyroll Exclusive";
            }

            HtmlNode? priceNode = tile.SelectSingleNode(_priceRel);
            string price = priceNode?.GetAttributeValue("data-price", "ERROR") ?? "ERROR";

            // preandbackOrderAvailability holds "Release date : M/D/YYYY" for pre-orders;
            // empty for in-stock items. No explicit OOS / Back-Order signal in the listing
            // grid — those products carry it on the detail page only.
            HtmlNode? availNode = tile.SelectSingleNode(_availabilityRel);
            bool isPreOrder = availNode is not null && !string.IsNullOrWhiteSpace(availNode.InnerText);
            StockStatus stockStatus = isPreOrder ? StockStatus.PO : StockStatus.IS;

            data.Add(new EntryModel(cleanedTitle, $"${price}", stockStatus, TITLE));
        }

        data.TrimExcess();
        data.SortByVolume();
        return data;
    }
}