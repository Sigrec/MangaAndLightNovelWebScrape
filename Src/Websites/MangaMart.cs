using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class MangaMart : IWebsite
{
    private readonly ILogger _logger;

    public MangaMart(ILogger<MangaMart>? logger = null)
    {
        _logger = logger ?? NullLogger<MangaMart>.Instance;
    }

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='product-item__title text--strong link']");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price' or @class='price price--highlight']/text()[2]");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[contains(@class, 'product-item product-item--vertical')]");
    private static readonly XPathExpression DeepStockStatusXPath = XPathExpression.Compile(".//span[@class='bss_pl_text_hover_text bss_pl_text_hover_link_disable']/div/strong");
    private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//a[@class='pagination__nav-item link'])[last()]");
    private static readonly XPathExpression EntryTitleDesc = XPathExpression.Compile("//div[@class='rte text--pull']");

    [GeneratedRegex(@"\b(?:Vols?|Volume)\b\.?", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"(?>,|\([^)]*\)|\s*Includes.*|(?<=Omnibus,\s*Vol\s*\d{1,3}\.).*|(?<=Vol\s*\d{1,3}(?!\d)).*|-The Manga|The Manga|Manga)", RegexOptions.IgnoreCase)] private static partial Regex ParseAndCleanTitleRegex();
    [GeneratedRegex(@"(?<=Box\s*Set\s*\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex BoxSetTitleCleanRegex();
    [GeneratedRegex(@"\((?:Omnibus|\d{1,3}-in-\d{1,3}) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusTitleCleanRegex();

    /// <inheritdoc />
    public const string TITLE = "MangaMart";

    /// <inheritdoc />
    public const string BASE_URL = "https://mangamart.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunPlaywrightScrapeAsync(
            this, Website.MangaMart, bookTitle, bookType, masterDataList, masterLinkList, errors, browser!, curRegion, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    /// <summary>
    /// URL builder for a given page. The <paramref name="encodedBookTitle"/> is pre-escaped by
    /// the caller — escaping happens once per scrape rather than per page.
    /// </summary>
    internal string GenerateWebsiteUrl(BookType bookType, string encodedBookTitle, uint curPageNum)
    {
        // https://mangamart.com/search?type=product&q=jujutsu+kaisen&page=2
        // https://mangamart.com/search?type=product&q=overlord+novel
        string url = $"{BASE_URL}/search?type=product&q={encodedBookTitle}{(bookType == BookType.Manga ? string.Empty : "+novel")}&page={curPageNum}";
        _logger.PageUrl(curPageNum, url);
        return url;
    }

    private static string ParseAndCleanTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            entryTitle = BoxSetTitleCleanRegex().Replace(entryTitle, string.Empty);
        }
        else if (OmnibusTitleCleanRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusTitleCleanRegex().Replace(entryTitle, "Omnibus");
        }

        StringBuilder curTitle = new(ParseAndCleanTitleRegex().Replace(entryTitle, string.Empty));

        if (bookType == BookType.LightNovel)
        {
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "(Light Novel)", string.Empty);
        }
        else
        {
            if (bookTitle.Contains("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(" Naruto Next Generations", string.Empty);
            }
        }

        if (bookTitle.Contains(':'))
        {
            InternalHelpers.RemoveAfterLastIfMultiple(ref curTitle, ':');
        }
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        
        curTitle.TrimEnd().AddVolToString();
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    private static async Task WaitForStablePageLoadAsync(IPage page)
    {
        try
        {
            await page.WaitForSelectorAsync(
                "span.bss_pl_text_hover_text div strong",
                new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 5_000
                }
            );

            await page.WaitForSelectorAsync(
                "span.price.price--highlight",
                new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 5_000
                }
            );
        }
        catch (TimeoutException) { }
        finally
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.ScrollToBottomUntilStableAsync(
                "span.price.price--highlight",
                maxScrolls: 60,
                stabilityMs: 900,
                stepPx: 1400
            );
        }
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        HtmlWeb html = HtmlFactory.CreateWeb();
        string encodedBookTitle = Uri.EscapeDataString(bookTitle);

        // Walk paginated Playwright navigation, capturing each settled DOM into an
        // HtmlDocument. Parsing happens in ParsePages so fixture-based tests can replay.
        List<HtmlDocument> listingPages = [];
        uint curPageNum = 1;
        string url = GenerateWebsiteUrl(bookType, encodedBookTitle, curPageNum);
        links.Add(url);

        await page!.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForStablePageLoadAsync(page);
        HtmlDocument doc = HtmlFactory.CreateDocument();
        doc.LoadHtml(await page.ContentAsync());
        listingPages.Add(doc);

        int maxPageNum;
        {
            XPathNavigator initialNav = doc.DocumentNode.CreateNavigator();
            XPathNavigator? pageNode = initialNav.SelectSingleNode(PageCheckXPath);
            maxPageNum = pageNode is not null ? pageNode.ValueAsInt : 1;
        }
        _logger.MaxPages(maxPageNum);

        while (curPageNum < maxPageNum)
        {
            curPageNum++;
            url = GenerateWebsiteUrl(bookType, encodedBookTitle, curPageNum);
            links.Add(url);
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForStablePageLoadAsync(page);
            HtmlDocument next = HtmlFactory.CreateDocument();
            next.LoadHtml(await page.ContentAsync());
            listingPages.Add(next);
        }

        data = await ParsePages(
            listingPages,
            bookTitle,
            bookType,
            async href =>
            {
                string fullUrl = href.StartsWith('/') ? $"{BASE_URL}{href}" : $"{BASE_URL}/{href}";
                return await html.LoadFromWebAsync(fullUrl);
            });

        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        return (data, links);
    }

    /// <summary>
    /// Parses pre-loaded MangaMart listing docs into <see cref="EntryModel"/>s. The
    /// <paramref name="resolveDescDoc"/> delegate turns a product href into the detail-page
    /// HTML — live path uses <see cref="HtmlWeb"/>; tests pass an offline dictionary lookup.
    /// </summary>
    internal async Task<List<EntryModel>> ParsePages(
        IReadOnlyList<HtmlDocument> listingPages,
        string bookTitle,
        BookType bookType,
        Func<string, Task<HtmlDocument>> resolveDescDoc)
    {
        List<EntryModel> data = [];
        bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);

        foreach (HtmlDocument doc in listingPages)
        {
            // Re-create the navigator per page — old code reused one bound to a stale subtree.
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();

            List<string> entryTitles = [];
            List<string?> hrefs = [];
            {
                XPathNodeIterator titleData = nav.Select(TitleXPath);
                while (titleData.MoveNext())
                {
                    XPathNavigator? cur = titleData.Current;
                    if (cur is null) continue;
                    string? entryTitle = WebUtility.HtmlDecode(cur.Value)?.Trim();
                    if (entryTitle is null) continue;
                    entryTitles.Add(entryTitle);
                    hrefs.Add(cur.GetAttribute("href", string.Empty));
                }
            }

            List<string> prices = [];
            {
                XPathNodeIterator priceData = nav.Select(PriceXPath);
                while (priceData.MoveNext())
                {
                    prices.Add(priceData.Current?.Value ?? string.Empty);
                }
            }

            List<XPathNavigator> stockStatusNodes = [];
            {
                XPathNodeIterator stockStatusData = nav.Select(StockStatusXPath);
                while (stockStatusData.MoveNext())
                {
                    if (stockStatusData.Current is not null) stockStatusNodes.Add(stockStatusData.Current.Clone());
                }
            }

            int entryCount = entryTitles.Count;

            // Desc-fetch pre-pass: Manga entries without "Vol" need a detail check to filter
            // out light novels. Collect indices, batch through the resolver.
            HtmlDocument?[] descCache = new HtmlDocument?[entryCount];
            if (bookType == BookType.Manga)
            {
                List<int> needsDesc = [];
                for (int i = 0; i < entryCount; i++)
                {
                    if (entryTitles[i].Contains("Vol", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitles[i])
                        || (!bookTitleRemovalCheck && InternalHelpers.ShouldRemoveEntry(entryTitles[i])))
                    {
                        continue;
                    }
                    string? href = hrefs[i];
                    if (!string.IsNullOrWhiteSpace(href)) needsDesc.Add(i);
                }

                if (needsDesc.Count > 0)
                {
                    Task<HtmlDocument>[] fetches = new Task<HtmlDocument>[needsDesc.Count];
                    for (int i = 0; i < needsDesc.Count; i++)
                    {
                        fetches[i] = resolveDescDoc(hrefs[needsDesc[i]]!);
                    }
                    HtmlDocument[] docs = await Task.WhenAll(fetches);
                    for (int i = 0; i < needsDesc.Count; i++)
                    {
                        descCache[needsDesc[i]] = docs[i];
                    }
                }
            }

            for (int i = 0; i < entryCount; i++)
            {
                string entryTitle = entryTitles[i];

                bool shouldRemoveEntry = !InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle)
                    || (!bookTitleRemovalCheck && InternalHelpers.ShouldRemoveEntry(entryTitle));

                if (bookType == BookType.Manga)
                {
                    shouldRemoveEntry = shouldRemoveEntry
                        || (!bookTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase))
                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, ["Pirate Recipes"])
                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, ["of Gluttony"])
                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, ["Boruto"])
                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, ["Unimplemented"]);

                    if (!shouldRemoveEntry && !entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.CheckingForNovel(entryTitle);
                        HtmlDocument? descDoc = descCache[i];
                        HtmlNode? descNode = descDoc?.DocumentNode.SelectSingleNode(EntryTitleDesc);
                        string? innerText = descNode?.InnerText;
                        if (innerText is not null
                            && (innerText.Contains("Light Novel", StringComparison.OrdinalIgnoreCase)
                                || innerText.Contains("novels", StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.FoundNovelInMangaScrape();
                            shouldRemoveEntry = true;
                        }
                    }
                }
                else if (bookType == BookType.LightNovel)
                {
                    shouldRemoveEntry = shouldRemoveEntry
                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented");
                }

                if (!shouldRemoveEntry)
                {
                    string stockStatusNode = i < stockStatusNodes.Count
                        ? stockStatusNodes[i].SelectSingleNode(DeepStockStatusXPath)?.Value.Trim() ?? string.Empty
                        : string.Empty;
                    StockStatus stockStatus = stockStatusNode switch
                    {
                        "PRE-ORDER" => StockStatus.PO,
                        "BACK-ORDER" => StockStatus.BO,
                        _ => StockStatus.IS,
                    };
                    _logger.StockStatusDebug(entryTitle, string.IsNullOrWhiteSpace(stockStatusNode), stockStatusNode);

                    string price = i < prices.Count ? prices[i] : string.Empty;
                    data.Add(new EntryModel(
                        ParseAndCleanTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                        price,
                        stockStatus,
                        TITLE));
                }
                else
                {
                    _logger.EntryRemovedSimpleDebug(entryTitle);
                }
            }
        }

        data.TrimExcess();
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