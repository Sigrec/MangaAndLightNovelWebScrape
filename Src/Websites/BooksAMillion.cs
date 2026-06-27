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

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    internal static string GenerateWebsiteUrl(string bookTitle, bool boxSetCheck, BookType bookType, int pageNum)
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

        HtmlWeb descHtml = HtmlFactory.CreateWeb();
        descHtml.PreRequest += req =>
        {
            req.UserAgent = _userAgent;
            return true;
        };

        // Walk the Playwright-driven listing pagination (regular pass, then box-set pass
        // if validated), capturing each settled DOM into an HtmlDocument. The captured
        // docs preserve the boxSetCheck flag and pageNum in their order — pass 1 first,
        // box-set pass second. ParsePages re-implements the same iteration order and
        // resolves desc pages via the supplied delegate.
        List<HtmlDocument> listingPages = [];
        List<bool> boxSetFlags = [];

        bool boxSetCheck = false, boxsetValidation = false;
        int pageNum = 1;
        string curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, pageNum);
        _logger.InitialUrl(curUrl);
        links.Add(curUrl);

        await page!.GotoAsync(curUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);

        IReadOnlyList<IElementHandle> popupContainer = await page.QuerySelectorAllAsync(".ltkpopup-container").ConfigureAwait(false);
        if (popupContainer.Count > 0)
        {
            await page.ClickAsync(".ltkpopup-close").ConfigureAwait(false);
        }

        while (true)
        {
            await page.WaitForSelectorAsync(".search-item-title").ConfigureAwait(false);
            HtmlDocument doc = HtmlFactory.CreateDocument();
            doc.LoadHtml(await page.ContentAsync().ConfigureAwait(false));
            listingPages.Add(doc);
            boxSetFlags.Add(boxSetCheck);

            // Probe just enough to make pagination/pivot decisions — full parsing happens
            // in ParsePages over the captured docs.
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();
            List<string> titlesProbe = [];
            {
                XPathNodeIterator iter = nav.Select(_titleXPath);
                while (iter.MoveNext())
                {
                    if (iter.Current is null) continue;
                    titlesProbe.Add(iter.Current.Value);
                }
            }
            if (titlesProbe.Count == 0)
            {
                _logger.HelmCollectionEmpty();
                break;
            }
            XPathNavigator? pageCheck = nav.SelectSingleNode(_pageCheckXPath);

            // Detect whether this pass turned up any qualifying box-set match — drives
            // the pivot to the box-set pass after we exhaust the regular pages.
            if (!boxsetValidation)
            {
                foreach (string raw in titlesProbe)
                {
                    string t = WebUtility.HtmlDecode(raw.Trim());
                    if (t.Contains(bookTitle, StringComparison.OrdinalIgnoreCase)
                        && t.ContainsAny(_boxSetIncludeVals)
                        && (bookType == BookType.Manga
                            || (!t.ContainsAny(["Manga", "Volumes", "Vol"])
                                && !MangaRemovalRegex().IsMatch(t))))
                    {
                        boxsetValidation = true;
                        break;
                    }
                }
            }

            if (pageCheck != null)
            {
                curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, ++pageNum);
                links.Add(curUrl);
                _logger.NextPage(curUrl);
                await page.GotoAsync(curUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
            }
            else if (boxsetValidation && !boxSetCheck)
            {
                boxSetCheck = true;
                pageNum = 1;
                curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, pageNum);
                links.Add(curUrl);
                _logger.BoxSetUrl(curUrl);
                await page.GotoAsync(curUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
            }
            else
            {
                break;
            }
        }

        data = await ParsePages(
            listingPages,
            boxSetFlags,
            bookTitle,
            bookType,
            isMember,
            async href =>
            {
                string fullUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? href
                    : href.StartsWith('/') ? $"{BASE_URL}{href}" : $"{BASE_URL}/{href}";
                return await descHtml.LoadFromWebAsync(fullUrl).ConfigureAwait(false);
            }).ConfigureAwait(false);

        links.TrimExcess();
        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        return (data, links);
    }

    /// <summary>
    /// Parses pre-loaded BooksAMillion listing docs into <see cref="EntryModel"/>s. The
    /// <paramref name="boxSetFlags"/> list pairs each doc with whether it was captured
    /// during the regular pass (false) or the box-set pass (true) — affects the per-entry
    /// eligibility check. <paramref name="resolveDescDoc"/> turns a product href into the
    /// detail-page HTML — live runs use <see cref="HtmlWeb"/>; tests use an offline lookup.
    /// </summary>
    internal async Task<List<EntryModel>> ParsePages(
        IReadOnlyList<HtmlDocument> listingPages,
        IReadOnlyList<bool> boxSetFlags,
        string bookTitle,
        BookType bookType,
        bool isMember,
        Func<string, Task<HtmlDocument>> resolveDescDoc)
    {
        List<EntryModel> data = [];
        bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);
        bool bookTitleHasDigit = HasAnyDigit(bookTitle);

        // pageNum is reconstructed per-pass: starts at 1 each time boxSet flag flips.
        int pageNumWithinPass = 0;
        bool? prevBoxSet = null;

        for (int docIdx = 0; docIdx < listingPages.Count; docIdx++)
        {
            HtmlDocument doc = listingPages[docIdx];
            bool boxSetCheck = boxSetFlags[docIdx];
            if (prevBoxSet != boxSetCheck)
            {
                pageNumWithinPass = 1;
                prevBoxSet = boxSetCheck;
            }
            else
            {
                pageNumWithinPass++;
            }
            int pageNum = pageNumWithinPass;

            XPathNavigator nav = doc.DocumentNode.CreateNavigator();

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
            if (entryCount == 0) continue;

            List<string> bookQualities = CollectValues(nav, _bookQualityXPath, entryCount);
            List<string> prices = CollectValues(nav, _pricexPath, entryCount);
            List<string> stocks = CollectValues(nav, _stockStatusXPath, entryCount);

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
                        fetches[i] = resolveDescDoc(titleHrefs[needsDesc[i]]!);
                    }
                    HtmlDocument[] docs = await Task.WhenAll(fetches).ConfigureAwait(false);
                    for (int i = 0; i < needsDesc.Count; i++)
                    {
                        descCache[needsDesc[i]] = docs[i];
                    }
                }
            }

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

            for (int i = 0; i < entryCount; i++)
            {
                string entryTitle = titles[i];
                bool entryHasMangaMarker = hasMangaMarker[i];
                bool entryHasBoxSet = hasBoxSetMarker[i];
                bool entryMatchesMangaRemoval = matchesMangaRemoval[i];

                // Box-set "validation" rows are skipped by the parser; their job is purely
                // signal to pivot to the box-set listing pass (already done in GetData).
                if (entryTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase)
                    && entryHasBoxSet
                    && !boxSetCheck
                    && (bookType == BookType.Manga ||
                        (bookType == BookType.LightNovel &&
                         !entryTitle.ContainsAny(["Manga", "Volumes", "Vol"]) &&
                         !entryMatchesMangaRemoval)))
                {
                    continue;
                }

                if (InternalHelpers.ShouldRemoveEntry(entryTitle) && !bookTitleRemovalCheck)
                {
                    _logger.EntryRemovedSimpleDebug(entryTitle);
                    continue;
                }

                string bookQuality = bookQualities[i];

                if (
                    InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle) &&
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