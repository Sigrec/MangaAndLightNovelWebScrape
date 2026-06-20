using System.Collections.Frozen;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class MerryManga : IWebsite
{
    private readonly ILogger _logger;

    public MerryManga(ILogger<MerryManga>? logger = null)
    {
        _logger = logger ?? NullLogger<MerryManga>.Instance;
    }

    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|Omnibus( \d{1,2})(?:, |\s{1})Vol \d{1,3}-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex FixOmnibusRegex();
    [GeneratedRegex(@"(?<=Box Set \d{1}).*", RegexOptions.IgnoreCase)] private static partial Regex FixBoxSetRegex();
    [GeneratedRegex(@" \(.*\)|,")] private static partial Regex FixTitleRegex();
    [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

    /// <inheritdoc />
    public const string TITLE = "MerryManga";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.merrymanga.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    private static readonly FrozenSet<string> _stockClasses = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "instock",
        "outofstock",
        "onbackorder",
        "preorder",
        "available_at_warehouse"
    );

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunPlaywrightScrapeAsync(
            this, Website.MerryManga, bookTitle, bookType, masterDataList, masterLinkList, errors, browser!, curRegion, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    // https://www.merrymanga.com/?s=Naruto&post_type=product&_categories=box-sets
    // https://www.merrymanga.com/?s=jujutsu+kaisen&post_type=product&orderby=release_date&_categories=manga
    internal string GenerateWebsiteUrl(string bookTitle, BookType bookType, bool hasBoxSet)
    {
        string url;
        if (hasBoxSet && bookType != BookType.LightNovel)
        {
            url = $"{BASE_URL}/?s={bookTitle.Replace(" ", "+")}&post_type=product&orderby=release_date&_categories=box-sets";
        }
        else
        {
            url = $"{BASE_URL}/?s={bookTitle.Replace(" ", "+")}&post_type=product&orderby=release_date&_categories={(bookType == BookType.Manga ? "manga" : "light-novels")}";
        }
        _logger.UrlGenerated(url);
        return url;
    }

    private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        string s = entryTitle;

        if (FixOmnibusRegex().IsMatch(s))
        {
            s = FixOmnibusRegex().Replace(s, "Omnibus$1");
            if (!s.Contains("Vol", StringComparison.Ordinal))
            {
                int pos = s.IndexOf("Omnibus", StringComparison.Ordinal) + "Omnibus".Length;
                s = s.Insert(pos, " Vol");
            }
        }
        else if (FixBoxSetRegex().IsMatch(s))
        {
            s = FixBoxSetRegex().Replace(s, string.Empty);
        }

        s = FixTitleRegex().Replace(s, string.Empty);

        StringBuilder sb = new(s);

        if (bookType == BookType.LightNovel && !s.Contains("Novel", StringComparison.Ordinal))
        {
            int idx = s.IndexOf("Vol", StringComparison.Ordinal);
            sb.Insert(idx >= 0 ? idx : sb.Length, " Novel ");
        }
        else if (bookType == BookType.Manga && 
                bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
        {
            sb.Replace("Naruto Next Generations", string.Empty);
        }

        InternalHelpers.RemoveCharacterFromTitle(ref sb, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref sb, bookTitle, "–", " ");

        string collapsed = MasterScrape
            .MultipleWhiteSpaceRegex()
            .Replace(sb.ToString().Trim(), " ");

        return collapsed;
    }

    private void ExtractProductData(
        HtmlDocument doc,
        out List<string> titles,
        out List<string> prices,
        out List<StockStatus> statuses)
    {
        titles = [];
        prices = [];
        statuses = [];

        foreach (HtmlNode node in doc.DocumentNode.Descendants())
        {
            string nodeName = node.Name;

            // 1) TITLE: <h2 class="woocommerce-loop-product__title">
            if (nodeName == "h2")
            {
                // //h2[@class='woocommerce-loop-product__title']
                string classAttr = node.GetAttributeValue("class", string.Empty);
                if (classAttr.Equals("woocommerce-loop-product__title", StringComparison.OrdinalIgnoreCase))
                {
                    string text = node.InnerText.Trim();
                    titles.Add(text);
                    _logger.ProductTitleSeen(text);
                }
            }

            // 2) PRICE: match either
            //    a) <span class="price"> → <ins> → <span class="woocommerce-Price-amount amount"> → <bdi>
            // or b) <span class="price"> → <span class="woocommerce-Price-amount amount"> → <bdi>
            if (nodeName == "bdi")
            {
                HtmlNode parentNode = node.ParentNode;
                if (parentNode is not null
                    && parentNode.Name == "span"
                    && parentNode.GetAttributeValue("class", string.Empty)
                        .Contains("woocommerce-Price-amount amount", StringComparison.OrdinalIgnoreCase))
                {
                    HtmlNode grandParent = parentNode.ParentNode;
                    if (grandParent is not null)
                    {
                        // case (a): under <ins>
                        if (grandParent.Name == "ins")
                        {
                            HtmlNode greatGrand = grandParent.ParentNode;
                            if (greatGrand is not null
                                && greatGrand.Name == "span"
                                && greatGrand.GetAttributeValue("class", string.Empty)
                                    .Equals("price", StringComparison.OrdinalIgnoreCase))
                            {
                                prices.Add(node.InnerText.Trim());
                            }
                        }
                        // case (b): directly under <span class="price">
                        else if (grandParent.Name == "span"
                            && grandParent.GetAttributeValue("class", string.Empty)
                                .Equals("price", StringComparison.OrdinalIgnoreCase))
                        {
                            prices.Add(node.InnerText.Trim());
                        }
                    }
                }
                continue;
            }

            // 3) STOCK: //li[contains(@class, 'instock')] | //li[contains(@class, 'outofstock')] | ...
            if (nodeName == "li")
            {
                string classAttr = node.GetAttributeValue("class", string.Empty);
                // Span-based split: no string[] allocation per node. For pages with hundreds of
                // <li> elements this is the difference between hundreds of short-lived heap
                // arrays per scrape and zero.
                foreach (Range partRange in classAttr.AsSpan().Split(' '))
                {
                    ReadOnlySpan<char> part = classAttr.AsSpan(partRange);
                    if (part.IsEmpty) continue;

                    // _stockClasses lookup needs a string key today. The Length-prefilter below
                    // short-circuits the lookup for the vast majority of class tokens (which are
                    // arbitrary lengths like "product_cat-manga").
                    if (part.Length < 6 || part.Length > 23) continue;

                    string partStr = part.ToString();
                    if (_stockClasses.Contains(partStr))
                    {
                        statuses.Add(partStr switch
                        {
                            "instock" or "available_at_warehouse" => StockStatus.IS,
                            "outofstock" => StockStatus.OOS,
                            "preorder" => StockStatus.PO,
                            "onbackorder" => StockStatus.BO,
                            _ => StockStatus.NA,
                        });
                        break;
                    }
                }
            }
        }
    }

    private async Task CheckAndProceedIfRated18Async(IPage page)
    {
        ILocator heading = page.Locator("h2.popup_heading");

        // Wait a short moment to see if it appears (optional)
        if (await heading.CountAsync() > 0)
        {
            string text = (await heading.First.InnerTextAsync()).Trim();

            if (text.Equals("This product is rated 18+", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator("button.btn_submit#submit").ClickAsync();
                _logger.ProceededFromAgePopup();
            }
        }
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        string bookTitleLower = bookTitle.ToLower();
        bool hasBoxSet = true;
        List<HtmlDocument> listingPages = [];

    Restart:
        string url = GenerateWebsiteUrl(bookTitleLower, bookType, hasBoxSet);
        links.Add(url);

        await page!.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForSelectorAsync("div.container.main-content");

        HtmlDocument doc = HtmlFactory.CreateDocument();
        doc.LoadHtml(await page.ContentAsync());

        if (hasBoxSet && doc.Text.Contains("No products were found matching your selection."))
        {
            _logger.NoBoxSetEntries();
            hasBoxSet = false;
            links.Clear();
            url = GenerateWebsiteUrl(bookTitleLower, bookType, hasBoxSet);
            links.Add(url);

            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForSelectorAsync("div.container.main-content");
        }

        await CheckAndProceedIfRated18Async(page);
        await ExhaustLoadMoreAsync(page);

        doc = HtmlFactory.CreateDocument();
        doc.LoadHtml(await page.ContentAsync());
        listingPages.Add(doc);

        if (hasBoxSet)
        {
            hasBoxSet = false;
            goto Restart;
        }

        data = ParsePages(listingPages, bookTitle, bookType);
        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        return (data, links);
    }

    /// <summary>
    /// Clicks the "Load More" facet button until it disappears or the "no products" banner
    /// shows. Extracted so live runs and the fixture-regenerate task share the exhaust logic.
    /// </summary>
    private async Task ExhaustLoadMoreAsync(IPage page)
    {
        ILocator visibleBtn = page.Locator("button.facetwp-load-more:not(.facetwp-hidden)");
        if (await visibleBtn.CountAsync() == 0) return;

        ILocator hiddenBtn = page.Locator("button.facetwp-load-more.facetwp-hidden");
        ILocator pager = page.Locator("div.facetwp-facet.facetwp-facet-load_more.facetwp-type-pager");
        ILocator noProducts = page.Locator("div.woocommerce-info");

        while (true)
        {
            if (await noProducts.CountAsync() > 0)
            {
                string text = (await noProducts.First.InnerTextAsync()).Trim();
                if (text.Equals("No products were found matching your selection.", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            _logger.MerryMangaLoadingMoreEntries();
            if (await visibleBtn.CountAsync() == 0) break;

            await visibleBtn.First.ClickAsync();
            await pager.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = 5000
            });

            Task tHidden = hiddenBtn.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 4000 });
            Task tGone = visibleBtn.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 4000 });
            await Task.WhenAny(tHidden, tGone);
            await page.WaitForTimeoutAsync(50);

            if (await hiddenBtn.CountAsync() > 0 || await visibleBtn.CountAsync() == 0)
            {
                break;
            }
        }

        _logger.FinishedLoadingMoreEntries();
    }

    /// <summary>
    /// Parses pre-loaded MerryManga listing docs into <see cref="EntryModel"/>s. Fixture-based
    /// tests load the saved HTML and call this directly — no Playwright required.
    /// </summary>
    internal List<EntryModel> ParsePages(IReadOnlyList<HtmlDocument> listingPages, string bookTitle, BookType bookType)
    {
        List<EntryModel> data = [];
        bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);

        foreach (HtmlDocument doc in listingPages)
        {
            ExtractProductData(doc, out List<string> titleData, out List<string> priceData, out List<StockStatus> stockStatusData);

            for (int x = 0; x < titleData.Count; x++)
            {
                string entryTitle = titleData[x];
                if (
                    InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle)
                    && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                    && !(
                            (
                                bookType == BookType.Manga
                                && (
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                    )
                            )
                        ||
                            (
                                bookType == BookType.LightNovel
                                && (
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented")
                                    )
                            )
                        )
                    )
                {
                    string price = x < priceData.Count ? priceData[x] : string.Empty;
                    StockStatus status = x < stockStatusData.Count ? stockStatusData[x] : StockStatus.NA;
                    data.Add(new EntryModel(
                        ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol").Trim(), bookTitle, bookType),
                        price,
                        status,
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
}