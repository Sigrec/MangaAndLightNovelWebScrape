using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class MangaMate : IWebsite
{
    private readonly ILogger _logger;

    public MangaMate(ILogger<MangaMate>? logger = null)
    {
        _logger = logger ?? NullLogger<MangaMate>.Instance;
    }

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//div[@class='grid-product__title grid-product__title--body']");
    private static readonly XPathExpression _priceXPath = XPathExpression.Compile("//div[@class='grid-product__price']/text()[3]");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//div[@class='grid-product__content']/div[1]");
    private static readonly XPathExpression _stockStatusXPath2 = XPathExpression.Compile("//div[@class='grid-product__image-mask']/div[1]");
    private static readonly XPathExpression _entryLinkXPath = XPathExpression.Compile("//div[@class='grid__item-image-wrapper']/a");
    private static readonly XPathExpression _entryTypeXPath = XPathExpression.Compile("//div[@class='product-block'][4]/div/span/table//tr[4]/td[2]");

    [GeneratedRegex(@"The Manga|\(.*\)|Manga", RegexOptions.IgnoreCase)] private static partial Regex TitleParseRegex();
    [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

    /// <inheritdoc />
    public const string TITLE = "MangaMate";

    /// <inheritdoc />
    public const string BASE_URL = "https://mangamate.shop";

    /// <inheritdoc />
    public const Region REGION = Region.Australia;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunPlaywrightScrapeAsync(
            this, Website.MangaMate, bookTitle, bookType, masterDataList, masterLinkList, errors, browser!, curRegion, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum)
    {
        // https://mangamate.shop/search?q=akane%20banashi&options%5Bprefix%5D=last
        string url = $"{BASE_URL}/search?options%5Bprefix%5D=last&page={pageNum}&q={InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"))}+{(bookType == BookType.Manga ? "manga" : "novel")}";
        _logger.PageUrlGenerated(pageNum, url);
        return url;
    }

    private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (OmnibusRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
        }
        else
        {
            entryTitle = TitleParseRegex().Replace(entryTitle, string.Empty);
        }
        StringBuilder curTitle = new StringBuilder(entryTitle).Replace(",", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, ":", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Color Edition", "In Color");
        if (bookTitle.Equals("boruto", StringComparison.OrdinalIgnoreCase)) { curTitle.Replace(" Naruto Next Generations", string.Empty); }

        Match findVolNumMatch = MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim());
        if (bookType == BookType.Manga && !entryTitle.Contains("Box Set") && !entryTitle.Contains("Vol") && !string.IsNullOrWhiteSpace(findVolNumMatch.Groups[0].Value))
        {
            curTitle.Insert(findVolNumMatch.Index, "Vol ").TrimEnd();
        }
        else if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Stray Stories") && string.IsNullOrWhiteSpace(findVolNumMatch.Groups[0].Value))
        {
            curTitle.Insert(curTitle.Length, " Vol 1");
        }

        string volNum = findVolNumMatch.Groups[0].Value;
        if (volNum.Length > 1 && volNum.StartsWith('0'))
        {
            curTitle.Replace(volNum, volNum.TrimStart('0'));
        }
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    private static async Task WaitForProductPageLoad(IPage page)
    {
        // Wait on the title element directly — that's the thing we parse, and waiting on
        // it sidesteps brittle container selectors that get renamed across redesigns.
        // The old `(//div[@class='grid grid--uniform'])[2]` selector started timing out
        // after MangaMate rebuilt their grid wrapper.
        try
        {
            await page.WaitForSelectorAsync(
                "//div[@class='grid-product__title grid-product__title--body']",
                new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 15000
                });
        }
        catch (TimeoutException)
        {
            // No products on the page — let the caller's parse return 0 and end the loop.
        }
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<(string Html, uint MaxPageNum)> GetInitialData(IPage page, string url)
    {
        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForProductPageLoad(page);

        // Snapshot the page early so debug dumps survive any subsequent timeout
        // (e.g. a missing currency picker after a site redesign).
        string earlyHtml = await page.ContentAsync();
        DumpDebugHtml(earlyHtml, "after_load");

        // Get the max page number (//span[@class='page'][last()])
        uint maxPageNum = 1;
        ILocator pages = page.Locator("//span[@class='page']").Last;
        int count = await pages.CountAsync();
        if (count > 0)
        {
            string? lastText = await pages.Nth(count - 1).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(lastText) && uint.TryParse(lastText.Trim(), out uint parsed))
            {
                maxPageNum = parsed;
            }
        }
        _logger.MaxPageNum(maxPageNum);

        // Currency picker is a nice-to-have — if the button is gone the scrape should
        // still proceed in the site's default currency, not fail outright with a 30s timeout.
        try
        {
            ILocator currencyBtn = page.Locator("//button[@aria-controls='CurrencyList-toolbar']");
            if (await currencyBtn.CountAsync() == 0)
            {
                _logger.ClickedAudCurrency();
                return (earlyHtml, maxPageNum);
            }

            await currencyBtn.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
            await page.Locator("button[aria-controls='CurrencyList-toolbar'][aria-expanded='true']")
                .WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

            string? menuId = await currencyBtn.GetAttributeAsync("aria-controls");
            ILocator menu = page.Locator($"#{menuId}");
            await menu.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });
            await menu.Locator("a.disclosure-list__option[data-value='AU']").First
                .ClickAsync(new LocatorClickOptions { Timeout = 5000 });

            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            _logger.ClickedAudCurrency();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        catch (TimeoutException)
        {
            // Currency picker UI changed or is missing — proceed with default currency.
        }
        catch (PlaywrightException)
        {
            // Same: any Playwright-side issue selecting the currency shouldn't crater the scrape.
        }

        return (await page.ContentAsync(), maxPageNum);
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        HtmlWeb html = HtmlFactory.CreateWeb();
        HtmlDocument doc = HtmlFactory.CreateDocument();

        ushort curPageNum = 1;
        bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);
        string url = GenerateWebsiteUrl(bookTitle, bookType, curPageNum);
        links.Add(url);

        (string firstHtml, uint maxPageNum) = await GetInitialData(page!, url);
        doc.LoadHtml(firstHtml);

        while (true)
        {
            // Re-create the navigator per page — HtmlAgilityPack's LoadHtml replaces
            // the subtree under DocumentNode but the old navigator's iterators may
            // already be bound to stale nodes.
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();

            // Snapshot the parallel iterators into Lists. The old lockstep MoveNext()
            // pattern desynced when any of the 5 iterators yielded fewer matches than
            // titleData; materializing once lets the pre-pass below index in O(1).
            List<string> titles = CollectValues(nav, _titleXPath);
            int entryCount = titles.Count;
            if (entryCount == 0)
            {
                if (curPageNum < maxPageNum) goto NextPage;
                break;
            }

            List<string> prices = CollectValues(nav, _priceXPath, entryCount);
            List<string> stocks = CollectValues(nav, _stockStatusXPath, entryCount);
            List<string> stockClasses = CollectAttributes(nav, _stockStatusXPath2, "class", entryCount);
            List<string> hrefs = CollectAttributes(nav, _entryLinkXPath, "href", entryCount);

            // Eligibility pre-pass: skip ineligible entries entirely — no point spending
            // an HTTP detail-page fetch on titles we'll drop anyway. Survivors go into
            // `keep`, then we batch-fetch all detail pages via Task.WhenAll. Old code
            // did one sequential `await html.LoadFromWebAsync(...)` per entry inside the
            // loop — N round-trips per page.
            bool[] eligible = new bool[entryCount];
            List<int> needsType = [];
            for (int i = 0; i < entryCount; i++)
            {
                string t = titles[i];
                if (!InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, t))
                {
                    _logger.EntryRemoved(1, t);
                    continue;
                }
                if (InternalHelpers.ShouldRemoveEntry(t) && !BookTitleRemovalCheck)
                {
                    _logger.EntryRemoved(1, t);
                    continue;
                }
                if (bookType == BookType.Manga &&
                    (t.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                     || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", t, "Boruto")
                     || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", t, "Story")
                     || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", t, "Can't Fear")))
                {
                    _logger.EntryRemoved(1, t);
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(hrefs[i]))
                {
                    eligible[i] = true;
                    needsType.Add(i);
                }
            }

            HtmlDocument?[] typeDocs = new HtmlDocument?[entryCount];
            if (needsType.Count > 0)
            {
                Task<HtmlDocument>[] fetches = new Task<HtmlDocument>[needsType.Count];
                for (int j = 0; j < needsType.Count; j++)
                {
                    string href = hrefs[needsType[j]];
                    string fullUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? href
                        : href.StartsWith('/') ? $"{BASE_URL}{href}" : $"{BASE_URL}/{href}";
                    fetches[j] = html.LoadFromWebAsync(fullUrl);
                }
                HtmlDocument[] docs = await Task.WhenAll(fetches);
                for (int j = 0; j < needsType.Count; j++)
                {
                    typeDocs[needsType[j]] = docs[j];
                }
            }

            for (int i = 0; i < entryCount; i++)
            {
                if (!eligible[i]) continue;

                string entryTitle = titles[i];
                HtmlDocument? detail = typeDocs[i];
                string? type = detail?.DocumentNode.CreateNavigator().SelectSingleNode(_entryTypeXPath)?.Value?.Trim();
                if (string.IsNullOrEmpty(type))
                {
                    continue;
                }

                bool typeMatchesManga = bookType == BookType.Manga
                    && (type.Equals("Manga", StringComparison.OrdinalIgnoreCase)
                        || type.Equals("Box Set", StringComparison.OrdinalIgnoreCase));
                bool typeMatchesNovel = bookType == BookType.LightNovel
                    && (type.Equals("Novel", StringComparison.OrdinalIgnoreCase)
                        || type.Equals("Box Set", StringComparison.OrdinalIgnoreCase));
                if (!(typeMatchesManga || typeMatchesNovel))
                {
                    _logger.EntryRemovedSimple(entryTitle);
                    continue;
                }

                StockStatus stockStatus = stockClasses[i].Contains("preorder", StringComparison.Ordinal)
                    ? StockStatus.PO
                    : stocks[i].Trim() switch
                    {
                        "Sold Out" => StockStatus.OOS,
                        _ => StockStatus.IS,
                    };

                data.Add(new EntryModel(
                    ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                    prices[i].Trim(),
                    stockStatus,
                    TITLE));
            }

        NextPage:
            if (curPageNum < maxPageNum)
            {
                url = GenerateWebsiteUrl(bookTitle, bookType, ++curPageNum);
                links.Add(url);
                await page!.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await WaitForProductPageLoad(page);
                doc.LoadHtml(await page.ContentAsync());
            }
            else
            {
                break;
            }
        }

        data.TrimExcess();
        links.TrimExcess();
        data.RemoveDuplicates(_logger);
        data.SortByVolume();
        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);

        return (data, links);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private static void DumpDebugHtml(string html, string label)
    {
        try
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"MangaMate_{label}.html"), html ?? string.Empty);
        }
        catch { /* diagnostic only */ }
    }

    private static List<string> CollectValues(XPathNavigator nav, XPathExpression xpath)
    {
        List<string> result = [];
        XPathNodeIterator iter = nav.Select(xpath);
        while (iter.MoveNext())
        {
            result.Add(iter.Current?.Value ?? string.Empty);
        }
        return result;
    }

    private static List<string> CollectValues(XPathNavigator nav, XPathExpression xpath, int expectedCount)
    {
        List<string> result = CollectValues(nav, xpath);
        // Pad to align with the title list so per-index access in the main loop is safe.
        while (result.Count < expectedCount) result.Add(string.Empty);
        return result;
    }

    private static List<string> CollectAttributes(XPathNavigator nav, XPathExpression xpath, string attribute, int expectedCount)
    {
        List<string> result = [];
        XPathNodeIterator iter = nav.Select(xpath);
        while (iter.MoveNext())
        {
            result.Add(iter.Current?.GetAttribute(attribute, string.Empty) ?? string.Empty);
        }
        while (result.Count < expectedCount) result.Add(string.Empty);
        return result;
    }
}