using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

/// <summary>
/// OK Comics — Leeds-based UK comic / manga retailer running on Shopify. Search results
/// are paginated but the listing card only carries title and availability — prices live
/// on the product detail page. Same shape as TravellingMan: parse the listing, batch
/// detail-page fetches via Task.WhenAll, stitch prices back per index.
/// </summary>
public sealed partial class OKComics : IWebsite
{
    private readonly ILogger _logger;

    public OKComics(ILogger<OKComics>? logger = null)
    {
        _logger = logger ?? NullLogger<OKComics>.Instance;
    }

    /// <inheritdoc />
    public const string TITLE = "OK Comics";
    /// <inheritdoc />
    public const string BASE_URL = "https://www.okcomics.co.uk";
    /// <inheritdoc />
    public const Region REGION = Region.Britain;

    // Listing: each card carries name + availability. No price here.
    private static readonly XPathExpression _cardXPath = XPathExpression.Compile("//a[@class='product-card']");
    private static readonly XPathExpression _nameRelXPath = XPathExpression.Compile(".//div[@class='product-card__name']");
    private static readonly XPathExpression _availRelXPath = XPathExpression.Compile(".//div[@class='product-card__availability']");

    // Detail page price.
    private static readonly XPathExpression _detailPriceXPath = XPathExpression.Compile("//span[contains(@class,'product-single__price')][@itemprop='price']");

    [GeneratedRegex(@"\s+by\s+.+$", RegexOptions.IgnoreCase)] private static partial Regex StripByAuthorRegex();
    [GeneratedRegex(@"Volume|Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@",|\(.*?\)|\s+Paperback$|\s+Hardcover$", RegexOptions.IgnoreCase)] private static partial Regex CleanTitleRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunHtmlScrapeAsync(
            this, Website.OKComics, bookTitle, bookType, masterDataList, masterLinkList, errors, curRegion, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    internal string GenerateWebsiteUrl(string bookTitle, BookType bookType, int curPage)
    {
        // https://www.okcomics.co.uk/search?q=jujutsu+kaisen&type=product&page=2
        string url = $"{BASE_URL}/search?q={bookTitle.Replace(' ', '+')}&type=product&page={curPage}";
        _logger.PageUrlGenerated(curPage, url);
        return url;
    }

    /// <summary>
    /// Cleans the listing's raw <c>"Title Volume N by Author"</c> shape into the canonical
    /// <c>"Title Vol N"</c> form used elsewhere in the library.
    /// </summary>
    private static string CleanAndParseTitle(string entryTitle, string bookTitle)
    {
        // Drop the trailing " by <author>" suffix — OK Comics appends it to every product.
        string s = StripByAuthorRegex().Replace(entryTitle, string.Empty);
        s = CleanTitleRegex().Replace(s, string.Empty);
        s = FixVolumeRegex().Replace(s, "Vol");

        StringBuilder curTitle = new(s);
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        // OK Comics is a comic / manga shop — they don't stock prose light novels.
        // Skip silently (no entry in MasterScrape.Errors) so multi-site LightNovel
        // scrapes that happen to include OK Comics still work for the other sites.
        if (bookType == BookType.LightNovel)
        {
            _logger.BookTypeNotSupported(TITLE, bookType);
            return (data, links);
        }

        HtmlWeb web = HtmlFactory.CreateWeb();

        // Walk pagination. OK Comics returns a small curated catalog — even popular
        // searches rarely exceed a handful of results — so a hard cap keeps a 404-on-
        // overshoot from spinning forever.
        List<HtmlDocument> listingPages = [];
        const int MaxPages = 20;
        int curPage = 1;
        while (curPage <= MaxPages)
        {
            string url = GenerateWebsiteUrl(bookTitle, bookType, curPage);
            links.Add(url);
            HtmlDocument doc = await web.LoadFromWebAsync(url);
            doc.ConfigurePerf();
            listingPages.Add(doc);

            // Page N+1 returns the same shell with zero cards when results run out.
            HtmlNodeCollection? cards = doc.DocumentNode.SelectNodes(_cardXPath);
            if (cards is null || cards.Count == 0) break;
            curPage++;
        }

        data = await ParsePages(
            listingPages,
            bookTitle,
            bookType,
            async href =>
            {
                string fullUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? href
                    : href.StartsWith('/') ? $"{BASE_URL}{href}" : $"{BASE_URL}/{href}";
                return await web.LoadFromWebAsync(fullUrl);
            });

        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        return (data, links);
    }

    /// <summary>
    /// Parses pre-loaded OK Comics listing docs into <see cref="EntryModel"/>s. The
    /// <paramref name="resolveDescDoc"/> delegate fetches a product detail page from a
    /// href — live runs use <see cref="HtmlWeb"/>; tests pass an offline lookup.
    /// </summary>
    internal async Task<List<EntryModel>> ParsePages(
        IReadOnlyList<HtmlDocument> listingPages,
        string bookTitle,
        BookType bookType,
        Func<string, Task<HtmlDocument>> resolveDescDoc)
    {
        List<EntryModel> data = [];

        // Match the GetData early-return so tests calling ParsePages directly see the
        // same behavior — LightNovel requests return an empty list.
        if (bookType == BookType.LightNovel)
        {
            _logger.BookTypeNotSupported(TITLE, bookType);
            return data;
        }

        bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);

        // Materialize all eligible cards across all pages first, then batch the price
        // fetches via Task.WhenAll. Going through one fetch per entry as we walk would
        // serialize the network round-trips — exactly the pattern the rest of the library
        // had already moved away from.
        List<(string Title, string Availability, string Href)> survivors = [];

        foreach (HtmlDocument doc in listingPages)
        {
            HtmlNodeCollection? cards = doc.DocumentNode.SelectNodes(_cardXPath);
            if (cards is null) continue;

            foreach (HtmlNode card in cards)
            {
                HtmlNode? nameNode = card.SelectSingleNode(_nameRelXPath);
                if (nameNode is null) continue;
                string entryTitle = WebUtility.HtmlDecode(nameNode.InnerText.Trim());
                if (string.IsNullOrEmpty(entryTitle)) continue;

                if (!InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle))
                {
                    _logger.EntryRemovedDebug(1, entryTitle);
                    continue;
                }
                if (InternalHelpers.ShouldRemoveEntry(entryTitle) && !BookTitleRemovalCheck)
                {
                    _logger.EntryRemovedDebug(1, entryTitle);
                    continue;
                }

                // OK Comics is manga-only; drop anything whose title clearly marks it as a
                // novel (occasionally the search drags in adjacent prose imports).
                if (entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.EntryRemovedDebug(1, entryTitle);
                    continue;
                }

                string href = card.GetAttributeValue<string>("href", string.Empty);
                if (string.IsNullOrWhiteSpace(href))
                {
                    _logger.EntryRemovedDebug(1, entryTitle);
                    continue;
                }

                string availability = card.SelectSingleNode(_availRelXPath)?.InnerText.Trim() ?? string.Empty;
                survivors.Add((entryTitle, availability, href));
            }
        }

        if (survivors.Count == 0) return data;

        // Cap concurrent detail fetches so a popular search doesn't slam OK Comics with
        // 80+ simultaneous HTTPS round-trips. Shopify-small shops will rate-limit (or
        // outright block) us if we fan out without a cap.
        const int MaxConcurrentDetailFetches = 8;
        HtmlDocument[] detailDocs = new HtmlDocument[survivors.Count];
        using (SemaphoreSlim gate = new(MaxConcurrentDetailFetches))
        {
            Task[] tasks = new Task[survivors.Count];
            for (int i = 0; i < survivors.Count; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(async () =>
                {
                    await gate.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        detailDocs[idx] = await resolveDescDoc(survivors[idx].Href).ConfigureAwait(false);
                    }
                    finally
                    {
                        gate.Release();
                    }
                });
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        for (int i = 0; i < survivors.Count; i++)
        {
            (string entryTitle, string availability, _) = survivors[i];
            HtmlNode? priceNode = detailDocs[i].DocumentNode.SelectSingleNode(_detailPriceXPath);
            string price = priceNode?.InnerText.Trim() ?? string.Empty;

            // "Sold Out" / "Pre-Order" maps to the standard StockStatus values.
            StockStatus stock = availability.Equals("Sold Out", StringComparison.OrdinalIgnoreCase)
                ? StockStatus.OOS
                : availability.Contains("Pre", StringComparison.OrdinalIgnoreCase)
                    ? StockStatus.PO
                    : StockStatus.IS;

            data.Add(new EntryModel(
                CleanAndParseTitle(entryTitle, bookTitle),
                price,
                stock,
                TITLE));
        }

        data.SortByVolume();
        data.RemoveDuplicates(_logger);
        return data;
    }

    /// <summary>
    /// Test helper: returns a resolver that looks each href up in <paramref name="hrefToDoc"/>.
    /// </summary>
    internal static Func<string, Task<HtmlDocument>> CreateOfflineDescResolver(IReadOnlyDictionary<string, HtmlDocument> hrefToDoc)
        => href => hrefToDoc.TryGetValue(href, out HtmlDocument? doc)
            ? Task.FromResult(doc)
            : throw new KeyNotFoundException(
                $"Desc fixture missing for href '{href}'. Re-run the Regenerate task to capture it.");
}
