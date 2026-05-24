using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class BooksAMillion : IWebsite
{
    private readonly ILogger _logger;

    public BooksAMillion(ILogger<BooksAMillion>? logger = null)
    {
        _logger = logger ?? NullLogger<BooksAMillion>.Instance;
    }

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//div[@class='search-item-title']/a");
    private static readonly XPathExpression _descXPath = XPathExpression.Compile("//div[@id='pdpOverview']/div/div");
    private static readonly XPathExpression _bookQualityXPath = XPathExpression.Compile("//div[@class='productInfoText']");
    private static readonly XPathExpression _pricexPath = XPathExpression.Compile("//span[@class='our-price']");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//div[@class='availability_search_results']");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//ul[@class='search-page-list']//a[@title='Next']");

    [GeneratedRegex(@"V\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex MangaRemovalRegex();
    [GeneratedRegex(@"(?<=Box Set).*|:|\!|,|Includes.*|--The Manga|The Manga|\d+-\d+|\(Manga\) |(?<=Omnibus\s\d{1,3})[^\d.].*|\d{1,3}\s+\d{1,3}\s+\&\s+(\d{1,3})|\d{1,3},\s+\d{1,3}\s+\&\s+(\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex MangaFilterTitleRegex();
    [GeneratedRegex(@":|\!|,|Includes.*|\d+-\d+|\d+, \d+ \& \d+", RegexOptions.IgnoreCase)] private static partial Regex NovelFilterTitleRegex();
    [GeneratedRegex(@"(?<=Vol\s+\d+)[^\d\.].*|\(.*?\)$|\[.*?\]|Manga ", RegexOptions.IgnoreCase)] private static partial Regex CleanFilterTitleRegex();
    [GeneratedRegex(@"Box Set (\d+)", RegexOptions.IgnoreCase)] private static partial Regex BoxSetNumberRegex();
    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|3-In-1 V\d+|Vols\.|\d{1,3}-In-\d{1,3}|\d{1,3}-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"3-In-1 V(\d+)|\d{1,3}-In-\d{1,3}|(?:\d{1,3}-(\d{1,3}))$|\d{1,3},\s+\d{1,3}\s+\&\s+(\d{1,3})|\d{1,3}\s+\d{1,3}\s+\&\s+(\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex OmnibusMatchRegex();
    [GeneratedRegex(@"Vol\.|Volumes|Volume|Vols\.|Vols", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

    /// <inheritdoc />
    public const string TITLE = "Books-A-Million";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.booksamillion.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    private const decimal MEMBERSHIP_DISCOUNT = 0.1M;

    private static readonly FrozenSet<string> _mangaDescExcludeVals = [ "Novel", ];
    private static readonly FrozenSet<string> _mangaIncludeVals = [ "Vol", "Box Set", "BOXSET", "Comic", "Anniversary" ];
    private static readonly FrozenSet<string> _boxSetIncludeVals = ["Boxset", "Box Set"];
    private static readonly FrozenSet<string> _novelIncludeVals = [ "Light Novel", "Novel", ];
    private static readonly FrozenSet<string> _novelExcludeVals = [ "Manga", "Volumes", "Vol" ];

    private const string _userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunPlaywrightScrapeAsync(
            this, Website.BooksAMillion, bookTitle, bookType, masterDataList, masterLinkList, errors, browser!, curRegion, cancellationToken,
            isMember: memberships.HasFlag(Membership.BooksAMillion),
            needsUserAgent: true);

    private static string GenerateWebsiteUrl(string bookTitle, bool boxSetCheck, BookType bookType, int pageNum)
    {
        // Initialize a StringBuilder
        StringBuilder stringBuilder = new($"{BASE_URL}/search2?");

        bookTitle = InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"));
        if (bookType == BookType.LightNovel)
        {
            // https://www.booksamillion.com/search2?query=classroom+of+the+elite+light+novel&filters%5Bproduct_type%5D=Books
            // https://www.booksamillion.com/search2?query=Overlord+light+novel
            // https://www.booksamillion.com/search2?query=Overlord+light+novel;filters[product_type]=Books&page=1
            stringBuilder.Append($"query={bookTitle}");
            stringBuilder.Append($"{(boxSetCheck ? "+light+novel+box+set" : "+light+novel")};filters[product_type]=Books;page={pageNum}");
        }
        else
        {
            // https://www.booksamillion.com/search2?query=2.5+dimensional+seduction;filters[product_type]=Books&filters[content_lang]=English

            stringBuilder.Append($"query={bookTitle}{(boxSetCheck ? "manga+box+set" : "+manga")};filters[product_type]=Books&filters[content_lang]=English;page={pageNum}");
        }

        // Convert StringBuilder to string
        string url = stringBuilder.ToString();

        return url;
    }

    private string CleanAndParseTitle(string entryTitle, BookType bookType, string bookTitle)
    {
        StringBuilder curTitle;

        if (bookType == BookType.LightNovel)
        {
            entryTitle = CleanFilterTitleRegex().Replace(NovelFilterTitleRegex().Replace(entryTitle, string.Empty), string.Empty);
            curTitle = new StringBuilder(entryTitle.Length)
                .Append(entryTitle)
                .Replace("(Novel)", "Novel")
                .Replace("(Light Novel)", "Novel");

            ReadOnlySpan<char> curSpan = curTitle.ToString();

            if (!curSpan.Contains("Novel", StringComparison.OrdinalIgnoreCase))
            {
                int volIndex = curSpan.IndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                int boxSetIndex = curSpan.IndexOf("Box Set", StringComparison.OrdinalIgnoreCase);

                if (volIndex != -1)
                    curTitle.Insert(volIndex, "Novel ");
                else if (boxSetIndex != -1)
                    curTitle.Insert(boxSetIndex, "Novel ");
                else
                    curTitle.Append(" Novel");
            }
        }
        else
        {
            ReadOnlySpan<char> rawSpan = entryTitle;

            if (rawSpan.Contains("Omnibus", StringComparison.CurrentCultureIgnoreCase) || 
                rawSpan.Contains("3-in-1", StringComparison.CurrentCultureIgnoreCase) || 
                rawSpan.Contains("2-in-1", StringComparison.CurrentCultureIgnoreCase))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
            }

            curTitle = new StringBuilder(CleanFilterTitleRegex().Replace(MangaFilterTitleRegex().Replace(entryTitle, string.Empty), string.Empty));
            string entryTitleCleaned = curTitle.ToString().Trim();
            ReadOnlySpan<char> cleanedSpan = entryTitleCleaned;

            if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace("Naruto Next Generations", string.Empty);
            }

            ReadOnlySpan<char> entrySpan = entryTitle;

            if (entrySpan.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || entrySpan.Contains("BOXSET", StringComparison.OrdinalIgnoreCase))
            {
                if (entryTitleCleaned.Contains("V01-V27", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("NARUTO BOXSET V01-V27", "Naruto Box Set 1");
                }
                else
                {
                    string boxSetNum = BoxSetNumberRegex().Match(entryTitle).Groups[1].Value;
                    if (!bookTitle.ContainsAny(["attack on titan"]))
                    {
                        curTitle.Append(' ');
                        curTitle.Append(!string.IsNullOrWhiteSpace(boxSetNum) ? boxSetNum : "1");
                    }
                }
            }
            else if (OmnibusMatchRegex().Match(entryTitle) is { Success: true } match)
            {
                // Single Match call instead of the old IsMatch+Match pair — Regex.Match
                // already costs the same as IsMatch and gives us Success cheaply.
                GroupCollection groups = match.Groups;
                string firstOmniNum = groups[1].Value.TrimStart('0');
                string secondOmniNum = groups[2].Value;
                string thirdOmniNum = groups[3].Value;

                _logger.OmnibusDebug(entryTitleCleaned, firstOmniNum, secondOmniNum, thirdOmniNum);

                if (!cleanedSpan.Contains(" Omnibus", StringComparison.OrdinalIgnoreCase))
                {
                    int volPos = cleanedSpan.IndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                    if (volPos != -1)
                        curTitle.Insert(volPos, "Omnibus ");
                }

                if (!cleanedSpan.Contains(" Vol", StringComparison.OrdinalIgnoreCase))
                    curTitle.Append("Vol ");

                ReadOnlySpan<char> trimmed = curTitle.ToString().Trim();
                if (!char.IsDigit(trimmed[^1]))
                {
                    if (!string.IsNullOrWhiteSpace(firstOmniNum))
                    {
                        curTitle.Append(firstOmniNum);
                    }
                    else if (!string.IsNullOrWhiteSpace(secondOmniNum))
                    {
                        curTitle.Append(Math.Ceiling(Convert.ToDecimal(secondOmniNum) / 3));
                    }
                    else if (!string.IsNullOrWhiteSpace(thirdOmniNum))
                    {
                        curTitle.Append(Math.Ceiling(Convert.ToDecimal(thirdOmniNum) / 3));
                    }
                }
            }
            else if (!cleanedSpan.Contains("Vol", StringComparison.OrdinalIgnoreCase) &&
                    !cleanedSpan.Contains("Box Set", StringComparison.OrdinalIgnoreCase) &&
                    MasterScrape.FindVolNumRegex().Match(entryTitleCleaned) is { Success: true } volMatch)
            {
                curTitle.Insert(volMatch.Index, "Vol ");
            }

            if (entrySpan.Contains("Stall", StringComparison.OrdinalIgnoreCase) && 
                !bookTitle.AsSpan().Contains("Stall", StringComparison.OrdinalIgnoreCase))
            {
                string temp = curTitle.ToString();
                int volIndex = temp.LastIndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                if (volIndex != -1)
                    curTitle.Remove(volIndex, curTitle.Length - volIndex);
            }
        }

        string final = curTitle.ToString();

        if (final.AsSpan().Contains("vols.", StringComparison.OrdinalIgnoreCase))
        {
            int index = final.IndexOf("vols.", StringComparison.OrdinalIgnoreCase);
            curTitle.Remove(index, curTitle.Length - index);
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString().Trim(), " ");
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        HtmlDocument doc = HtmlFactory.CreateDocument();

        // HtmlWeb for batched per-entry desc fetches. BaM's CDN gates on UA, so set a
        // modern Chrome string via PreRequest. (Playwright drives the listing nav so
        // that Cloudflare's challenge gets cleared automatically.)
        HtmlWeb descHtml = HtmlFactory.CreateWeb();
        descHtml.PreRequest += req =>
        {
            req.UserAgent = _userAgent;
            return true;
        };

        bool boxSetCheck = false, boxsetValidation = false;
        bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        int pageNum = 1;
        string curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, pageNum);
        _logger.InitialUrl(curUrl);
        links.Add(curUrl);

        await page!.GotoAsync(curUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Dismiss the promotion popup if it appears — persisted in localStorage so it
        // shouldn't reappear on subsequent navigations within the same scrape.
        IReadOnlyList<IElementHandle> popupContainer = await page.QuerySelectorAllAsync(".ltkpopup-container");
        if (popupContainer.Count > 0)
        {
            await page.ClickAsync(".ltkpopup-close");
        }

        while (true)
        {
            await page.WaitForSelectorAsync(".search-item-title");
            doc.LoadHtml(await page.ContentAsync());

            // Re-create the navigator per page — HtmlAgilityPack's LoadHtml replaces
            // the subtree under DocumentNode but the old navigator's iterators may
            // already be bound to stale nodes.
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();

            // Snapshot the parallel iterators into Lists. The old MoveNext()-in-lockstep
            // pattern was fragile across navigator state changes and made the desc-fetch
            // pre-pass impossible. Materializing once lets the pre-pass index in O(1).
            List<string> titles = [];
            List<string?> titleHrefs = [];
            {
                XPathNodeIterator iter = nav.Select(_titleXPath);
                while (iter.MoveNext())
                {
                    XPathNavigator? cur = iter.Current;
                    if (cur is null) continue;
                    titles.Add(WebUtility.HtmlDecode(cur.Value.Trim()));
                    titleHrefs.Add(cur.GetAttribute("href", string.Empty));
                }
            }
            int entryCount = titles.Count;

            if (entryCount == 0)
            {
                _logger.HelmCollectionEmpty();
                break;
            }

            List<string> bookQualities = CollectValues(nav, _bookQualityXPath, entryCount);
            List<string> prices = CollectValues(nav, _pricexPath, entryCount);
            List<string> stocks = CollectValues(nav, _stockStatusXPath, entryCount);
            XPathNavigator? pageCheck = nav.SelectSingleNode(_pageCheckXPath);

            // Desc-fetch pre-pass: any Manga entry whose title lacks a Vol/Box-Set/Comic
            // marker needs the product detail page to disambiguate from a novel. Old
            // code awaited each fetch serially via Playwright GotoAsync (+ a back-nav
            // to the listing). Pre-collect indices, batch the fetches via HtmlWeb,
            // cache the resulting docs by index.
            HtmlDocument?[] descCache = new HtmlDocument?[entryCount];
            if (bookType == BookType.Manga)
            {
                List<int> needsDesc = [];
                for (int i = 0; i < entryCount; i++)
                {
                    if (titles[i].ContainsAny(_mangaIncludeVals)) continue;
                    string? href = titleHrefs[i];
                    if (!string.IsNullOrWhiteSpace(href)) needsDesc.Add(i);
                }

                if (needsDesc.Count > 0)
                {
                    Task<HtmlDocument>[] fetches = new Task<HtmlDocument>[needsDesc.Count];
                    for (int i = 0; i < needsDesc.Count; i++)
                    {
                        string href = titleHrefs[needsDesc[i]]!;
                        string fullUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? href
                            : href.StartsWith('/') ? $"{BASE_URL}{href}" : $"{BASE_URL}/{href}";
                        fetches[i] = descHtml.LoadFromWebAsync(fullUrl);
                    }
                    HtmlDocument[] docs = await Task.WhenAll(fetches);
                    for (int i = 0; i < needsDesc.Count; i++)
                    {
                        descCache[needsDesc[i]] = docs[i];
                    }
                }
            }

            // Title-flag tables computed once per entry. The old per-entry block
            // called ContainsAny(_mangaIncludeVals)/ContainsAny(_boxSetIncludeVals) and
            // MangaRemovalRegex().IsMatch up to 3x each — hoist into locals so we pay
            // the cost once per row instead of per check.
            bool[] hasMangaMarker = new bool[entryCount];
            bool[] hasBoxSetMarker = new bool[entryCount];
            bool[] matchesMangaRemoval = new bool[entryCount];
            bool[] entryHasDigit = new bool[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                hasMangaMarker[i] = titles[i].ContainsAny(_mangaIncludeVals);
                hasBoxSetMarker[i] = titles[i].ContainsAny(_boxSetIncludeVals);
                matchesMangaRemoval[i] = MangaRemovalRegex().IsMatch(titles[i]);
                entryHasDigit[i] = HasAnyDigit(titles[i]);
            }
            bool bookTitleHasDigit = HasAnyDigit(bookTitle);

            for (int i = 0; i < entryCount; i++)
            {
                string entryTitle = titles[i];
                bool entryHasMangaMarker = hasMangaMarker[i];
                bool entryHasBoxSet = hasBoxSetMarker[i];
                bool entryMatchesMangaRemoval = matchesMangaRemoval[i];

                if (!boxsetValidation &&
                    entryTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) &&
                    entryHasBoxSet &&
                    (bookType == BookType.Manga ||
                        (
                            bookType == BookType.LightNovel &&
                            !entryTitle.ContainsAny(["Manga", "Volumes", "Vol"]) &&
                            !entryMatchesMangaRemoval
                        )
                    )
                )
                {
                    boxsetValidation = true;
                    continue;
                }

                if (InternalHelpers.ShouldRemoveEntry(entryTitle) && !bookTitleRemovalCheck)
                {
                    _logger.EntryRemovedSimpleDebug(entryTitle);
                    continue;
                }

                string bookQuality = bookQualities[i];

                if (
                    InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle) &&
                    (string.IsNullOrEmpty(bookQuality) || !bookQuality.Contains("Library Binding")) &&
                    (
                        entryCount == 1 && !boxSetCheck ||
                        (
                            bookType == BookType.Manga &&
                            (
                                (entryTitle.Equals(bookTitle, StringComparison.OrdinalIgnoreCase) && pageNum == 1) ||
                                entryHasMangaMarker ||
                                (!entryHasMangaMarker &&
                                (pageNum > 1 || entryHasDigit[i] && !bookTitleHasDigit))
                            )
                            &&
                            (!(
                                entryTitle.Contains("(Light Novel", StringComparison.OrdinalIgnoreCase) ||
                                InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony") ||
                                InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, ["harsh mistress"]) ||
                                InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto", "Story", "Team 7 Character", "Dragon Rider")
                            )
                            ||
                            (
                                InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Wanted")
                            ))
                        )
                        ||
                        (
                            bookType == BookType.LightNovel &&
                            (entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) ||
                            !entryTitle.ContainsAny(_novelExcludeVals) &&
                            !entryMatchesMangaRemoval)
                        )
                    )
                )
                {
                    if (!entryHasBoxSet && entryMatchesMangaRemoval)
                    {
                        _logger.EntryRemovedDebug(2, entryTitle);
                        continue;
                    }

                    string cleaned = CleanAndParseTitle(
                        FixVolumeRegex().Replace(entryTitle, "Vol "),
                        bookType,
                        bookTitle);

                    string price = prices[i].Trim();
                    if (string.IsNullOrEmpty(price))
                    {
                        _logger.EntryRemovedDebug(4, entryTitle);
                        continue;
                    }

                    // Strip a leading '$' if present; otherwise parse the whole value.
                    ReadOnlySpan<char> priceSpan = price.StartsWith('$') ? price.AsSpan(1) : price.AsSpan();
                    if (!decimal.TryParse(priceSpan, out decimal priceVal))
                    {
                        _logger.EntryRemovedDebug(4, entryTitle);
                        continue;
                    }

                    ReadOnlySpan<char> stockText = stocks[i].AsSpan();
                    StockStatus stockStatus =
                        stockText.Contains("In Stock", StringComparison.OrdinalIgnoreCase) ? StockStatus.IS :
                        stockText.Contains("Preorder", StringComparison.OrdinalIgnoreCase) ? StockStatus.PO :
                        stockText.Contains("On Order", StringComparison.OrdinalIgnoreCase) ? StockStatus.BO :
                        StockStatus.OOS;

                    if (bookType == BookType.Manga && !entryHasMangaMarker)
                    {
                        HtmlDocument? descDoc = descCache[i];
                        if (descDoc is null)
                        {
                            _logger.EntryRemovedDebug(3, entryTitle);
                            continue;
                        }
                        HtmlNode? desc = descDoc.DocumentNode.SelectSingleNode(_descXPath);
                        if (desc is null || desc.InnerText.ContainsAny(_mangaDescExcludeVals))
                        {
                            _logger.EntryRemovedDebug(3, entryTitle);
                            continue;
                        }
                    }

                    data.Add(new EntryModel(
                        cleaned,
                        $"${(isMember ? EntryModel.ApplyDiscount(priceVal, MEMBERSHIP_DISCOUNT) : priceVal.ToString())}",
                        stockStatus,
                        TITLE));
                }
                else
                {
                    _logger.EntryRemovedDebug(1, entryTitle);
                }
            }

            if (pageCheck != null)
            {
                curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, ++pageNum);
                links.Add(curUrl);
                _logger.NextPage(curUrl);
                await page.GotoAsync(curUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            }
            else if (boxsetValidation && !boxSetCheck)
            {
                boxSetCheck = true;
                pageNum = 1;
                curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, pageNum);
                links.Add(curUrl);
                _logger.BoxSetUrl(curUrl);
                await page.GotoAsync(curUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            }
            else
            {
                break;
            }
        }

        data.TrimExcess();
        links.TrimExcess();
        data.SortByVolume();
        data.RemoveDuplicates(_logger);
        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);

        return (data, links);
    }

    /// <summary>
    /// Tight inline replacement for <c>str.Any(char.IsDigit)</c> — avoids the
    /// LINQ enumerator alloc + the static <c>char.IsDigit</c> delegate alloc
    /// (callee may JIT specialize them but the Span loop is unambiguously cheap).
    /// </summary>
    private static bool HasAnyDigit(string str)
    {
        foreach (char c in str.AsSpan())
        {
            if (c >= '0' && c <= '9') return true;
        }
        return false;
    }

    private static List<string> CollectValues(XPathNavigator nav, XPathExpression xpath, int expectedCount)
    {
        List<string> result = new(expectedCount);
        XPathNodeIterator iter = nav.Select(xpath);
        while (iter.MoveNext())
        {
            result.Add(iter.Current?.Value ?? string.Empty);
        }
        // Pad to align with the title list — keeps the main loop indexing simple.
        while (result.Count < expectedCount) result.Add(string.Empty);
        return result;
    }
}