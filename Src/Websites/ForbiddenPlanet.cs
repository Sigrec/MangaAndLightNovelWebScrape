using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class ForbiddenPlanet : IWebsite
{
    private readonly ILogger _logger;

    public ForbiddenPlanet(ILogger<ForbiddenPlanet>? logger = null)
    {
        _logger = logger ?? NullLogger<ForbiddenPlanet>.Instance;
    }
    

    [GeneratedRegex(@"The Manga|\(Hardcover\)|:(?:.*):|\(.*\)", RegexOptions.IgnoreCase)] internal static partial Regex CleanAndParseTitleRegex();
    [GeneratedRegex(@"\(Hardcover\)|The Manga", RegexOptions.IgnoreCase)] internal static partial Regex ColorCleanAndParseTitleRegex();
    [GeneratedRegex(@":.*(?:(?:3|2)-In-1|(?:3|2) In 1) Edition\s{0,3}:|:.*(?:(?:3|2)-In-1|(?:3|2) In 1)\s{0,3}:|(?:(?:3|2)-In-1|(?:3|2) In 1)\s{0,3} Edition:|\(Omnibus Edition\)|Omnibus\s{0,}(\d{1,3}|\d{1,3}.\d{1})(?:\s{0,}|:\s{0,})(?:\(.*\)|Vol \d{1,3}-\d{1,3})|:.*Omnibus:\s+(\d{1,3}).*|:.*:\s+Vol\s+(\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
    [GeneratedRegex(@"Box Set:|:\s+Box Set|Box Set (\d{1,3}):|:\s+(\d{1,3}) \(Box Set\)|\(Box Set\)|Box Set Part", RegexOptions.IgnoreCase)] private static partial Regex BoxSetFixRegex();
    [GeneratedRegex(@"(\d{1,3})-\d{1,3}")] private static partial Regex BoxSetVolFindRegex();
    [GeneratedRegex(@"(?<=(?:Vol|Box Set)\s+(?:\d{1,3}|\d{1,3}.\d{1}))[^\d{1,3}.]+.*")] private static partial Regex RemoveAfterVolNumRegex();
    [GeneratedRegex(@"\((.*?Anniversary.*?)\)")] private static partial Regex AnniversaryMatchRegex();
    [GeneratedRegex(@"Volumes|Volume|Vol\.|Volumr", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"(?:\(Deluxe(?::|\s*)|Deluxe\s*Edition:).*", RegexOptions.IgnoreCase)] internal static partial Regex DeluxeEditionRegex();

    /// <inheritdoc />
    public const string TITLE = "Forbidden Planet";

    /// <inheritdoc />
    public const string BASE_URL = "https://forbiddenplanet.com";

    /// <inheritdoc />
    public const Region REGION = Region.Britain;
    
    private static readonly FrozenSet<string> DescRemovalStrings = ["novel", "original stories", "collecting issues"];

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunPlaywrightScrapeAsync(
            this, Website.ForbiddenPlanet, bookTitle, bookType, masterDataList, masterLinkList, errors, browser!, curRegion, cancellationToken,
            needsUserAgent: true);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    private string GenerateWebsiteUrl(BookType bookType, string entryTitle, bool isSecondCategory, int pageNum)
    {
        // https://forbiddenplanet.com/catalog/manga/?q=Naruto&show_out_of_stock=on&sort=release-date-asc&page=1
        string url = $"{BASE_URL}/catalog/{(!isSecondCategory ? "manga" : "comics-and-graphic-novels")}/?q={(bookType == BookType.Manga ? InternalHelpers.FilterBookTitle(entryTitle) : $"{InternalHelpers.FilterBookTitle(entryTitle)}%20light%20novel")}&show_out_of_stock=on&sort=release-date-asc&page={pageNum}";
        _logger.UrlGenerated(url);
        return url;
    }



    private string CleanAndParseTitle(string bookTitle, string entryTitle, BookType bookType)
    {
        entryTitle = FixVolumeRegex().Replace(entryTitle.Trim(), " Vol");
        StringBuilder curTitle;
        if (entryTitle.EndsWith("(Colour Edition Hardcover)"))
        {
            entryTitle = entryTitle.Replace("(Colour Edition Hardcover)", string.Empty).Trim();
            entryTitle = entryTitle.Insert(entryTitle.IndexOf("Vol"), "In Color ");
        }

        if (!entryTitle.Contains("Anniversary") && entryTitle.ContainsAny(["3-In-1", "3 In 1", "Omnibus"]))
        {
            entryTitle = OmnibusFixRegex().Replace(entryTitle, $" Omnibus $1$2$3");
            curTitle = new StringBuilder(entryTitle);

            if (!entryTitle.Contains("Omnibus"))
            {
                curTitle.Insert(curTitle.IndexOfOrdinal("Vol"), "Omnibus ");
            }

            if (!entryTitle.Contains("Vol"))
            {
                Match volMatch = MasterScrape.FindVolNumRegex().Match(entryTitle);
                if (volMatch.Success)
                {
                    curTitle.Insert(volMatch.Index, "Vol ");
                };
            }
            curTitle.TrimEnd();

            if (!char.IsDigit(curTitle[curTitle.Length - 1]))
            {
                curTitle.Replace("Omnibus", string.Empty);
                curTitle.TrimEnd();
                curTitle.Insert(curTitle.IndexOfOrdinal("Vol"), "Omnibus ");
            }
        }
        else if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            if (BoxSetVolFindRegex().IsMatch(entryTitle))
            {
                curTitle = new StringBuilder(BoxSetVolFindRegex().Replace(BoxSetFixRegex().Replace(entryTitle, " Box Set $1"), string.Empty));
                if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || entryTitle.IndexOf("Vol") < entryTitle.IndexOf("Box Set"))
                {
                    curTitle.Append($" {BoxSetVolFindRegex().Match(entryTitle).Groups[1].Value}");
                }
            }
            else
            {
                curTitle = new StringBuilder(BoxSetFixRegex().Replace(entryTitle, " Box Set $2"));
                if (!char.IsDigit(entryTitle[^1]))
                {
                    curTitle.Replace("Box Set", string.Empty);
                    curTitle.TrimEnd();
                    if (!char.IsDigit(curTitle[curTitle.Length - 1])) curTitle.Append(" 1");
                    
                    if (entryTitle.Contains("Part") && !bookTitle.Contains("Part"))
                    {
                        curTitle.Insert(curTitle.Length, " Box Set");
                    }
                    else curTitle.Insert(curTitle.Length - 1, " Box Set ");
                }
            }
            curTitle.Replace("Vol", string.Empty);
            if (!bookTitle.Contains("One", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("One", "1");
            if (!bookTitle.Contains("Two", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Two", "2");
            if (!bookTitle.Contains("Three", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Three", "3");

            if (InternalHelpers.RemoveNonWordsRegex().Replace(bookTitle, string.Empty).Contains("Attackontitan", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("The Final Season") && entryTitle.Contains("Final Season"))
            {   
                curTitle.Insert("Attack On Titan".Length, " The");
            }
        }
        else
        {
            curTitle = new StringBuilder(entryTitle);
        }

        if (entryTitle.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
        {
            entryTitle = DeluxeEditionRegex().Replace(entryTitle, "Deluxe Edition");
            ReadOnlySpan<char> entryTitleSpan = entryTitle.AsSpan();
            if (!entryTitleSpan.Contains("Deluxe Edition Vol", StringComparison.OrdinalIgnoreCase))
            {
                int index = entryTitleSpan.IndexOf("Vol");
                if (index != -1)
                {
                    curTitle.Insert(index, "Deluxe Edition ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Deluxe Edition");
                }
            }
        }

        curTitle.Replace(",", string.Empty);
        if (!bookTitle.Contains(':') && !entryTitle.ContainsAny(["Year", "Oh", "Edition", "Boruto"]))
        {
            entryTitle = MasterScrape.MultipleWhiteSpaceRegex().Replace(CleanAndParseTitleRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
        }
        else
        {
            entryTitle = MasterScrape.MultipleWhiteSpaceRegex().Replace(ColorCleanAndParseTitleRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
        }
        curTitle = new StringBuilder(entryTitle);
        curTitle.Replace(":", string.Empty);

        if (bookType == BookType.LightNovel)
        {
            curTitle.Replace(" (Light Novel)", string.Empty).Replace(" (Light Novel Hardcover)", string.Empty);
            string snapshot = curTitle.ToString();
            _logger.Snapshot(snapshot);
            if (!snapshot.Contains("Novel"))
            {
                int index = snapshot.AsSpan().IndexOf("Vol");
                if (index != -1)
                {
                    curTitle.Insert(index, "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }
        }
        else
        {
            if (bookTitle.Contains("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(":", string.Empty);
                curTitle.Replace("Naruto Next Generations", string.Empty);
            }
        }

        entryTitle = curTitle.ToString();
        if (entryTitle.Contains("Anniversary") && entryTitle.Contains("Vol"))
        {
            curTitle.Insert(entryTitle.AsSpan().IndexOf("Vol"), $"{AnniversaryMatchRegex().Match(entryTitle).Groups[1].Value} ");
        }
        else if (entryTitle.Contains("Special") && entryTitle.Contains("Edition"))
        {
            curTitle.Insert(entryTitle.AsSpan().IndexOf("Vol"), "Special Edition ");
        }

        if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Omnibus") && !entryTitle.Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
        {
            curTitle.Insert(curTitle.ToString().Trim().AsSpan().IndexOf("Vol"), "Stray God ");
        }

        if (!entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            Match volMatch = MasterScrape.FindVolNumRegex().Match(entryTitle);
            if (volMatch.Success)
            {
                curTitle.Insert(volMatch.Index, "Vol ");
            };
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }



    // Per-product container. All sub-selectors run relative to one of these <li> nodes
    // so a single product missing one field (e.g. no stock badge for in-stock items)
    // never desyncs the columns across products. The price <p> aggregates major and
    // minor parts as child spans — grab the whole <p> instead of trying to stitch them
    // back together after the fact.
    private const string _productListXPath = "//div[@class='full']/ul/li";
    private const string _titleRel = "./section/header/div[2]/h3/a";
    private const string _priceRel = "./section/header/div[2]/p";
    private const string _stockStatusRel = "./section/header/div/ul";
    private const string _bookFormatRel = "./section/header/div[1]/p[1]";
    private const string _detailLinkRel = ".//a[@class='block one-whole clearfix dfbx dfbx--fdc link-banner link--black']";

    [GeneratedRegex(@"\s+")] private static partial Regex CollapseWhitespaceRegex();

    // Matches the leading sale price (optional currency symbol + digits + optional decimals).
    // Sale items render as e.g. "5.67RRP£7.99" — the trailing RRP block confuses
    // EntryModel.ParsePrice, so strip it here at capture time.
    [GeneratedRegex(@"^[£$€]?\d+(?:\.\d{1,2})?")] private static partial Regex LeadingPriceRegex();

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        HtmlDocument doc = HtmlFactory.CreateDocument();
        HtmlWeb html = HtmlFactory.CreateWeb();
        bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);

        // Two-category sweep: Manga first, then Comics & Graphic Novels.
        for (int category = 0; category < 2; category++)
        {
            bool isSecondCategory = category == 1;
            if (isSecondCategory) _logger.CheckingComicsCategory();

            string url = GenerateWebsiteUrl(bookType, bookTitle, isSecondCategory, 1);
            links.Add(url);

            await page!.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            try
            {
                await page.WaitForSelectorAsync("div.full", new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 15000
                });
            }
            catch (TimeoutException)
            {
                continue;
            }

            await LoadAllEntries(page);

            doc.LoadHtml(await page.ContentAsync());
            int parsed = await ProcessPageAsync(doc, bookTitle, bookType, BookTitleRemovalCheck, html, data);
            _logger.NodeCounts(parsed, parsed, parsed, parsed, parsed);
        }

        data.TrimExcess();
        data.SortByVolume();
        data.RemoveDuplicates(_logger);
        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);

        return (data, links);
    }

    /// <summary>
    /// Clicks the page's Load More button until it disappears. After each click, waits
    /// for the product <c>&lt;li&gt;</c> count to actually grow — that's the only signal
    /// that matters. Drives off DOM mutation rather than NetworkIdle (which can hang on
    /// trackers) or a fixed delay (which is either slow or flaky).
    /// </summary>
    private async Task LoadAllEntries(IPage page)
    {
        ILocator productItems = page.Locator(_productListXPath);
        ILocator loadMore = page.Locator("button.load-more.button--brand.brad--sm");

        const int MaxClicks = 50;
        for (int i = 0; i < MaxClicks; i++)
        {
            if (!await loadMore.IsVisibleAsync()) break;
            if (!await loadMore.IsEnabledAsync()) break;

            int before = await productItems.CountAsync();
            _logger.ForbiddenPlanetLoadingMoreEntries();
            await loadMore.ClickAsync();

            try
            {
                // Wait for the product list to grow. The XHR + DOM append usually lands
                // within a couple seconds; 15s is a generous ceiling for slow networks.
                await page.WaitForFunctionAsync(
                    $"() => document.querySelectorAll('div.full > ul > li').length > {before}",
                    new PageWaitForFunctionOptions { Timeout = 15000 });
            }
            catch (TimeoutException)
            {
                // Click didn't add anything within the window — assume we're done.
                break;
            }
        }
    }

    /// <summary>
    /// Parses one paginated listing page out of <paramref name="doc"/>, runs the desc-fetch
    /// pre-pass for Manga rows that need it, and appends qualifying rows to
    /// <paramref name="data"/>. Returns the number of product <c>&lt;li&gt;</c> nodes seen
    /// — used by the caller to decide whether to stop paginating.
    /// </summary>
    private async Task<int> ProcessPageAsync(
        HtmlDocument doc,
        string bookTitle,
        BookType bookType,
        bool bookTitleRemovalCheck,
        HtmlWeb html,
        List<EntryModel> data)
    {
        HtmlNodeCollection? products = doc.DocumentNode.SelectNodes(_productListXPath);
        int entryCount = products?.Count ?? 0;
        if (entryCount == 0) return 0;

        // Materialize per-product fields with sane defaults for optional pieces. A product
        // without a title is unscrapeable and gets `hasTitle[i] = false`; the main loop skips it.
        string[] decodedTitles = new string[entryCount];
        string[] priceVals = new string[entryCount];
        string[] bookFormatVals = new string[entryCount];
        string[] stockStatusVals = new string[entryCount];
        string?[] detailHrefs = new string?[entryCount];
        bool[] hasTitle = new bool[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            HtmlNode li = products![i];

            HtmlNode? titleNode = li.SelectSingleNode(_titleRel);
            if (titleNode is null) continue;
            hasTitle[i] = true;
            decodedTitles[i] = WebUtility.HtmlDecode(titleNode.InnerText.Trim());

            // Price <p> contains major and minor as child spans; InnerText concatenates
            // both. Collapse any whitespace HtmlAgilityPack picked up between the spans,
            // then extract only the leading sale price so RRP overlays (e.g. "5.67RRP£7.99")
            // don't trip up EntryModel.ParsePrice downstream.
            HtmlNode? priceNode = li.SelectSingleNode(_priceRel);
            if (priceNode is null)
            {
                priceVals[i] = string.Empty;
            }
            else
            {
                string rawPrice = CollapseWhitespaceRegex().Replace(WebUtility.HtmlDecode(priceNode.InnerText), string.Empty);
                Match m = LeadingPriceRegex().Match(rawPrice);
                priceVals[i] = m.Success ? m.Value : rawPrice;
            }
            bookFormatVals[i] = li.SelectSingleNode(_bookFormatRel)?.InnerText.Trim() ?? string.Empty;
            stockStatusVals[i] = li.SelectSingleNode(_stockStatusRel)?.InnerText.Trim() ?? string.Empty;
            detailHrefs[i] = li.SelectSingleNode(_detailLinkRel)?.GetAttributeValue<string?>("href", null);
        }

        // Desc-fetch pre-pass: any Manga entry that's a Hardcover or doesn't have a
        // recognized volume marker needs a detail-page fetch to decide whether it's a
        // novel-in-disguise. Serial awaits in the entry loop were the old bottleneck;
        // collect indices, batch via Task.WhenAll.
        HtmlDocument?[] descCache = new HtmlDocument?[entryCount];
        if (bookType == BookType.Manga)
        {
            List<int> needsDesc = [];
            for (int i = 0; i < entryCount; i++)
            {
                if (!hasTitle[i]) continue;
                if (string.Equals(stockStatusVals[i], "Currently Unavailable", StringComparison.OrdinalIgnoreCase)) continue;

                string entryTitle = decodedTitles[i];

                if (!PassesBroadFilter(bookTitle, entryTitle, bookFormatVals[i], bookTitleRemovalCheck, bookType))
                {
                    continue;
                }

                if (!(entryTitle.Contains("Hardcover") || !entryTitle.ContainsAny(["Vol", "Box Set", "Comic"])))
                {
                    continue;
                }

                string? href = detailHrefs[i];
                if (!string.IsNullOrWhiteSpace(href)) needsDesc.Add(i);
            }

            if (needsDesc.Count > 0)
            {
                Task<HtmlDocument>[] fetches = new Task<HtmlDocument>[needsDesc.Count];
                for (int i = 0; i < needsDesc.Count; i++)
                {
                    fetches[i] = html.LoadFromWebAsync($"{BASE_URL}{detailHrefs[needsDesc[i]]}");
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
            if (!hasTitle[i]) continue;

            string entryTitle = decodedTitles[i];
            string bookFormat = bookFormatVals[i];

            // ForbiddenPlanet keeps OOP / unavailable volumes on the listing page even with
            // "show out of stock=on" — they're never going to ship, and letting them through
            // would let their (often stale, often £0.00) price knock out the in-stock copy
            // during cross-site dedup. Skip them entirely.
            if (string.Equals(stockStatusVals[i], "Currently Unavailable", StringComparison.OrdinalIgnoreCase))
            {
                _logger.EntryRemoved(1, entryTitle);
                continue;
            }

            if (!PassesBroadFilter(bookTitle, entryTitle, bookFormat, bookTitleRemovalCheck, bookType))
            {
                _logger.EntryRemoved(1, entryTitle);
                continue;
            }

            bool descIsValid = true;
            if (bookType == BookType.Manga && (entryTitle.Contains("Hardcover") || !entryTitle.ContainsAny(["Vol", "Box Set", "Comic"])))
            {
                HtmlDocument? descDoc = descCache[i];
                if (descDoc is null)
                {
                    _logger.UnableToRetrieveUrlPath(i + 1);
                    continue;
                }

                HtmlNodeCollection descData = descDoc.DocumentNode.SelectNodes("//div[@id='product-description']/p");
                if (descData is not null)
                {
                    StringBuilder desc = new();
                    foreach (HtmlNode node in descData) { desc.AppendLine(node.InnerText); }
                    string descText = desc.ToString();
                    _logger.CheckingDesc(entryTitle, descText);
                    descIsValid = !descText.ContainsAny(DescRemovalStrings);
                }
            }

            if (descIsValid)
            {
                string finalTitle = CleanAndParseTitle(bookTitle, entryTitle, bookType);
                _logger.FinalTitle(finalTitle);
                data.Add(
                    new EntryModel(
                        finalTitle,
                        priceVals[i],
                        stockStatusVals[i] switch
                        {
                            "Pre-Order" => StockStatus.PO,
                            _ => StockStatus.IS
                        },
                        TITLE
                    )
                );
            }
            else
            {
                _logger.EntryRemoved(2, entryTitle);
            }
        }

        return entryCount;
    }

    private static bool PassesBroadFilter(string bookTitle, string entryTitle, string bookFormat, bool bookTitleRemovalCheck, BookType bookType)
    {
        if (!InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)) return false;
        if (InternalHelpers.ShouldRemoveEntry(entryTitle) && !bookTitleRemovalCheck) return false;

        if (bookType == BookType.Manga)
        {
            if (entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)) return false;
            if (!(bookFormat.Equals("Manga") || bookFormat.Equals("Graphic Novel"))) return false;
            if (InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony", "Berserker", "Operation")
                || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear")
                || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto", "Itachi", "Family Day", "Naruto: Shikamaru's Story", "Naruto: Kakashi's Story")
                || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "fullmetal alchemist", entryTitle, "Under The Faraway Sky")
                || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "FLY"))
            {
                return false;
            }
        }

        if (InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "Unimplemented")) return false;

        return true;
    }
}