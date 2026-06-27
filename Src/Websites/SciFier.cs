using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;
// Note: Microsoft.Playwright is needed for the IPage? parameter on IWebsite.GetData
// — SciFier itself is HtmlWeb-only and never instantiates Playwright.

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class SciFier : IWebsite
{
    private readonly ILogger _logger;

    public SciFier(ILogger<SciFier>? logger = null)
    {
        _logger = logger ?? NullLogger<SciFier>.Instance;
    }

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//ul[@class='productGrid']//h3[@class='card-title']/a");
    private static readonly XPathExpression _priceXPath = XPathExpression.Compile("//div[@class='card-body']//span[contains(@class, 'price price--withTax price--main')]");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//a[@aria-label='Next']");
    private static readonly XPathExpression _summaryXPath = XPathExpression.Compile("//div[@class='card-text card-text--summary']");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//div[@class='card-buttons']");
    private static readonly XPathExpression _entryDescXPath = XPathExpression.Compile("(//div[@class='productView-description-tabContent is-open'])[1]");

    [GeneratedRegex(@"(?<=Vol\s+(?:\d{1,3}|\d{1,3}\.\d{1}))[^\d.].+|(?<=Box Set \d{1,3}).*|\(Manga\)|The Manga|Manga", RegexOptions.IgnoreCase)] private static partial Regex TitleFixRegex();
    [GeneratedRegex(@"\s{1}[a-zA-Z]+\s{1}[a-zA-Z]+\s{1}\d{13}|,|\s+by\s+.*$")] private static partial Regex RemoveAuthorAndIdRegex();
    [GeneratedRegex(@"\s{1}[a-zA-Z]+\s{1}\d{13}|,|\s+by\s+.*$")] private static partial Regex RemoveAuthorAndIdSingleRegex();
    [GeneratedRegex(@"(?:Vol|Box Set) \d{1,3}\s{1}([a-zA-Z]+)\s{1}\d{13}", RegexOptions.IgnoreCase)] private static partial Regex GetAuthorAndIdRegex();
    [GeneratedRegex(@"\s{1}([a-zA-Z]+)\s{1}\d{13}", RegexOptions.IgnoreCase)] private static partial Regex GetAuthorAndIdNoVolRegex();
    [GeneratedRegex(@"\d{1,3}(?!\d*th)")]private static partial Regex GetVolNumRegex();
    [GeneratedRegex(@"\((?:\d{1}-in-\d{1}|Omnibus) Edition\)|:[\w\s]+\d{1,3}-\d{1,3}-\d{1,3}|Omnibus (\d{1,3})")]private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"[$£€]\d{1,3}\.\d{1,2} - ([$£€]\d{1,3}\.\d{1,2})")] private static partial Regex PriceRangeRegex();
    [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"V\d{1,3}")] private static partial Regex TitleRemoveRegex();
    
    /// <inheritdoc />
    public const string TITLE = "SciFier";

    /// <inheritdoc />
    public const string BASE_URL = "https://scifier.com";

    /// <inheritdoc />
    public const Region REGION = Region.America | Region.Europe | Region.Britain | Region.Canada | Region.Australia;

    private static readonly FrozenDictionary<Region, ushort> CURRENCY_DICTIONARY = new Dictionary<Region, ushort>
    {
        {Region.Britain, 1},
        {Region.America, 2},
        {Region.Australia, 3},
        {Region.Europe, 5},
        {Region.Canada, 6}
    }.ToFrozenDictionary();

    private static readonly FrozenSet<string> _checkDescStrings = ["Vol", "Box Set"];

    // Manga entries that don't carry any of these keywords get pruned unless they're priced
    // above $50 (rare special editions / artbooks). Hoisted from a per-call array literal so
    // we don't allocate a fresh string[] for every entry in the RemoveAll predicate.
    private static readonly FrozenSet<string> _mangaKeepStrings =
        FrozenSet.ToFrozenSet(new[] { "Vol", "Box Set", "Color", "Comic", "Anniversary" }, StringComparer.Ordinal);

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunHtmlScrapeAsync(
            this, Website.SciFier, bookTitle, bookType, masterDataList, masterLinkList, errors, curRegion, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    // Has issues where the search is not very strict unforunate
    internal string GenerateWebsiteUrl(string bookTitle, BookType bookType, Region curRegion, bool letterIsFrontHalf)
    {
        // https://scifier.com/search.php?setCurrencyId=4&section=product&search_query_adv=jujutsu+kaisen&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2060&section=product

        // https://scifier.com/search.php?setCurrencyId=6&section=product&search_query_adv=classroom+of+the+elite&searchsubs=ON&brand=&price_from=&price_to=&category=2060&limit=100&sort=alphaasc&mode=6

        // https://scifier.com/search.php?search_query_adv=overlord+novel&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2175&section=product&sort=alphadesc&limit=100&mode=6\
        
        string url;
        if (bookType == BookType.Manga)
        {
            url = $"{BASE_URL}/search.php?setCurrencyId={CURRENCY_DICTIONARY[curRegion]}&section=product&search_query_adv={bookTitle.Replace(' ', '+')}&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2060&section=product&limit=100&sort=alpha{(letterIsFrontHalf ? "asc" : "desc")}&mode=6";
        }
        else
        {
            url = $"{BASE_URL}/search.php?setCurrencyId={CURRENCY_DICTIONARY[curRegion]}&search_query_adv={bookTitle.Replace(' ', '+')}+light+novels&searchsubs=ON&brand=&price_from=&price_to=&section=product";
        }

        _logger.UrlGenerated(url);
        return url;
    }

    /// <summary>
    /// Cheap-filter check, lifted out of the main loop body so the pre-pass and the main loop
    /// stay in sync. Touch one, touch both — same conditions, same outcome.
    /// </summary>
    private static bool PassesBroadFilter(
        string entryTitle,
        string bookTitle,
        string normalizedBookTitle,
        bool bookTitleRemovalCheck,
        string summaryInnerText,
        BookType bookType)
    {
        return (!InternalHelpers.ShouldRemoveEntry(entryTitle) || bookTitleRemovalCheck)
            && InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle)
            && !entryTitle.Contains("USED COPY", StringComparison.OrdinalIgnoreCase)
            && !TitleRemoveRegex().IsMatch(entryTitle)
            && (
                (
                    bookType == BookType.Manga
                    && !entryTitle.Contains("Novel)", StringComparison.OrdinalIgnoreCase)
                    && (!summaryInnerText.Contains("novel", StringComparison.OrdinalIgnoreCase) || entryTitle.Contains("(Manga)") || entryTitle.Contains("Box Set"))
                    && !(
                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Funny Sports")
                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, ["Overlord Kugane Maruyama 9781975374785", "Unimplemented"])
                    )
                )
                ||
                (
                    bookType == BookType.LightNovel
                    && !entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase)
                )
            );
    }

    private static string CleanAndParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        Match omniCheck = OmnibusRegex().Match(entryTitle);
        if (omniCheck.Success)
        {
            if (!string.IsNullOrWhiteSpace(omniCheck.Groups[1].Value))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus Vol $1");
            }
            else
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, " Omnibus");
            }
        }
        if (bookType == BookType.Manga && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Vol"))
        {
            entryTitle = entryTitle.Insert(GetVolNumRegex().Match(entryTitle).Index, "Vol ");
        }

        StringBuilder curTitle = new(TitleFixRegex().Replace(entryTitle, string.Empty));
        string volNum = TitleFixRegex().Match(entryTitle).Groups[1].Value;

        // Cache StringBuilder snapshots so each Contains/IndexOf query doesn't allocate a fresh
        // string. We invalidate the cache (snapshot = null) after any mutation that could change
        // the answer.
        string? snapshot = curTitle.ToString();
        if (!string.IsNullOrWhiteSpace(volNum) && !snapshot.Contains("Vol"))
        {
            curTitle.AppendFormat(" Vol {0}", volNum);
            snapshot = null;
        }
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, '-', ' ');
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Complete ", string.Empty);
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Color Edition", "In Color");
        snapshot = null; // four mutators above may all have modified it

        if (entryTitle.Contains("Special Edition"))
        {
            snapshot ??= curTitle.ToString();
            curTitle.Insert(MasterScrape.FindVolNumRegex().Match(snapshot).Index - 4, "Special Edition ");
            snapshot = null;
        }
        if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
        {
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Naruto Next Generations", string.Empty);
            snapshot = null;
        }
        if (entryTitle.StartsWith("Vol "))
        {
            curTitle.Remove(0, 4);
            snapshot = null;
        }

        snapshot ??= curTitle.ToString();
        if (bookTitle.Equals("Bleach", StringComparison.OrdinalIgnoreCase)
            && !snapshot.Contains("Vol")
            && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace("Bleach", "Bleach Vol 40");
            snapshot = null;
        }
        if (bookType == BookType.LightNovel)
        {
            curTitle.Replace("(Light Novel)", "Novel");
            snapshot = null;
        }

        snapshot ??= curTitle.ToString();
        if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase)
            && !snapshot.Contains("Omnibus")
            && !snapshot.Contains("Stray Stories")
            && !snapshot.Contains("Stray God"))
        {
            int index = snapshot.IndexOf("Vol");
            if (index != -1)
            {
                curTitle.Insert(index, "Stray God ");
                snapshot = null;
            }
        }

        if (bookType == BookType.LightNovel)
        {
            snapshot ??= curTitle.ToString();
            if (!snapshot.Contains("Novel"))
            {
                int index = snapshot.IndexOf("Vol");
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

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            HtmlWeb html = HtmlFactory.CreateWeb();

            // SciFier returns results in alphabetical order. To get the relevant pages first
            // (the ones starting with the search letter), we request ascending sort for titles
            // beginning a-m (front half of the alphabet) and descending for n-z. The bit-trick
            // `c & 0b11111` maps ASCII A-Z and a-z both to 1-26, so position <= 13 ⇒ a-m.
            bool letterIsFrontHalf = char.IsDigit(bookTitle[0]) || (bookTitle[0] & 0b11111) <= 13;
            string url = GenerateWebsiteUrl(bookTitle, bookType, curRegion, letterIsFrontHalf);
            links.Add(url);

            // Walk pagination, gathering listing docs. The actual parse + box-set-desc fetch
            // pipeline lives in ParsePages so fixture-based tests can drive the same code path.
            List<HtmlDocument> listingPages = [];
            HtmlDocument doc = await html.LoadFromWebAsync(url).ConfigureAwait(false);
            doc.ConfigurePerf();
            listingPages.Add(doc);

            while (true)
            {
                if (ShouldStopPagination(doc, bookTitle, letterIsFrontHalf, out bool stop))
                {
                    if (stop) break;
                    // skippingPage path — fall through to grab the next page without parsing this one
                }

                HtmlNode? pageCheck = doc.DocumentNode.SelectSingleNode(_pageCheckXPath);
                if (pageCheck is null) break;

                url = $"https://scifier.com{WebUtility.HtmlDecode(pageCheck.GetAttributeValue("href", "Url Error"))}";
                doc = await html.LoadFromWebAsync(url).ConfigureAwait(false);
                doc.ConfigurePerf();
                links.Add(url);
                _logger.NextPageUrl(url);
                listingPages.Add(doc);
            }

            // Real-network desc resolver — fetches each detail-page URL on demand. The test
            // path swaps this for a dictionary-backed resolver that returns pre-loaded fixtures.
            data = await ParsePages(
                listingPages,
                bookTitle,
                bookType,
                async href =>
                {
                    HtmlDocument descDoc = await html.LoadFromWebAsync(href).ConfigureAwait(false);
                    return descDoc;
                }).ConfigureAwait(false);
        }
        finally
        {
            links.TrimExcess();
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        }

        return (data, links);
    }

    /// <summary>
    /// Returns <c>true</c> when pagination is over for the current alphabetical letter run.
    /// <paramref name="stop"/> is <c>true</c> for the "skip past relevant letters" exit;
    /// <c>false</c> means "this page is irrelevant but the next might still match". Exposed
    /// as <c>internal</c> so fixture-based tests can validate page selection independently.
    /// </summary>
    internal bool ShouldStopPagination(HtmlDocument doc, string bookTitle, bool letterIsFrontHalf, out bool stop)
    {
        stop = false;
        HtmlNodeCollection? titleData = doc.DocumentNode.SelectNodes(_titleXPath);
        if (titleData is null || titleData.Count == 0) return true;

        char firstEntryFirstChar = char.ToLowerInvariant(titleData[0].InnerText.TrimStart()[0]);
        char lastEntryFirstChar = char.ToLowerInvariant(titleData[^1].InnerText.TrimStart()[0]);
        char bookTitleFirstChar = char.ToLowerInvariant(bookTitle.TrimStart()[0]);

        if ((letterIsFrontHalf && firstEntryFirstChar > bookTitleFirstChar)
            || (!letterIsFrontHalf && firstEntryFirstChar < bookTitleFirstChar))
        {
            _logger.EndingScrapeEarly(lastEntryFirstChar, letterIsFrontHalf ? '>' : '<', firstEntryFirstChar);
            stop = true;
            return true;
        }

        if ((letterIsFrontHalf && firstEntryFirstChar < bookTitleFirstChar && lastEntryFirstChar < bookTitleFirstChar)
            || (!letterIsFrontHalf && firstEntryFirstChar > bookTitleFirstChar && lastEntryFirstChar > bookTitleFirstChar))
        {
            char cmp = letterIsFrontHalf ? '<' : '>';
            _logger.SkippingPage(firstEntryFirstChar, cmp, bookTitleFirstChar, lastEntryFirstChar, cmp, bookTitleFirstChar);
            // Irrelevant page — caller should fetch next without parsing this one.
            return true;
        }

        return false;
    }

    /// <summary>
    /// Runs the per-page parse + per-entry filter + box-set-desc check on pre-loaded listing
    /// docs. The <paramref name="resolveDescDoc"/> delegate fetches a detail page by href —
    /// the live path calls <c>HtmlWeb</c>; tests pass a dictionary-backed resolver so the
    /// run is fully offline.
    /// </summary>
    internal async Task<List<EntryModel>> ParsePages(
        IReadOnlyList<HtmlDocument> listingPages,
        string bookTitle,
        BookType bookType,
        Func<string, Task<HtmlDocument>> resolveDescDoc)
    {
        List<EntryModel> data = [];
        bool IsSingleName = true;
        bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);

        foreach (HtmlDocument doc in listingPages)
        {
            HtmlNodeCollection? titleData = doc.DocumentNode.SelectNodes(_titleXPath);
            if (titleData is null) continue;
            HtmlNodeCollection? priceData = doc.DocumentNode.SelectNodes(_priceXPath);
            HtmlNodeCollection? summaryData = doc.DocumentNode.SelectNodes(_summaryXPath);
            HtmlNodeCollection? stockStatusData = doc.DocumentNode.SelectNodes(_stockStatusXPath);
            if (priceData is null || summaryData is null || stockStatusData is null) continue;

            int count = titleData.Count;

            // Cache the decoded + Vol-normalized entry title per index. Used by both the
            // LightNovel pre-pass and the main processing loop, so compute once.
            string[] entryTitles = new string[count];
            for (int x = 0; x < count; x++)
            {
                entryTitles[x] = WebUtility.HtmlDecode(FixVolumeRegex().Replace(titleData[x].InnerText.Trim(), "Vol"));
            }

            // Light-novel desc-page fetches are the slowest thing on this scrape. Pre-pass
            // identifies which entries need a fetch, then Task.WhenAll runs them in parallel
            // through the supplied resolver. Main loop reads the cached result.
            HtmlDocument?[] lnDescCache = new HtmlDocument?[count];
            if (bookType == BookType.LightNovel)
            {
                List<int> needsLnDesc = [];
                for (int x = 0; x < count; x++)
                {
                    if (!entryTitles[x].Contains("novel", StringComparison.OrdinalIgnoreCase)
                        && PassesBroadFilter(entryTitles[x], bookTitle, normalizedBookTitle, BookTitleRemovalCheck, summaryData[x].InnerText, bookType))
                    {
                        needsLnDesc.Add(x);
                    }
                }

                if (needsLnDesc.Count > 0)
                {
                    Task<HtmlDocument>[] fetches = new Task<HtmlDocument>[needsLnDesc.Count];
                    for (int i = 0; i < needsLnDesc.Count; i++)
                    {
                        string href = titleData[needsLnDesc[i]].GetAttributeValue<string>("href", "ERROR");
                        fetches[i] = resolveDescDoc(href);
                    }
                    HtmlDocument[] docs = await Task.WhenAll(fetches).ConfigureAwait(false);
                    for (int i = 0; i < needsLnDesc.Count; i++)
                    {
                        lnDescCache[needsLnDesc[i]] = docs[i];
                    }
                }
            }

            int foundCheck = 0;
            for (int x = 0; x < count; x++)
            {
                string entryTitle = entryTitles[x];
                if (!PassesBroadFilter(entryTitle, bookTitle, normalizedBookTitle, BookTitleRemovalCheck, summaryData[x].InnerText, bookType))
                {
                    _logger.EntryRemovedDebug(1, entryTitle);
                    continue;
                }

                if (bookType == BookType.LightNovel && !entryTitle.Contains("novel", StringComparison.OrdinalIgnoreCase))
                {
                    HtmlNode? descNode = lnDescCache[x]?.DocumentNode.SelectSingleNode(_entryDescXPath);
                    if (descNode is null
                        || !descNode.InnerText.ContainsAny(["novel series", "series of prose novels"]))
                    {
                        _logger.EntryRemoved(3, entryTitle);
                        continue;
                    }
                }

                foundCheck++;
                if (foundCheck == 1)
                {
                    if (entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase))
                    {
                        IsSingleName = !string.IsNullOrWhiteSpace(GetAuthorAndIdRegex().Match(entryTitle).Groups[1].Value);
                    }
                    else
                    {
                        string author = GetAuthorAndIdNoVolRegex().Match(entryTitle).Groups[1].Value;
                        IsSingleName = !string.IsNullOrWhiteSpace(author) && author.Count(char.IsUpper) == 2;
                    }
                }

                entryTitle = IsSingleName
                    ? RemoveAuthorAndIdSingleRegex().Replace(entryTitle, string.Empty)
                    : RemoveAuthorAndIdRegex().Replace(entryTitle, string.Empty);

                if (InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle)
                    || InternalHelpers.Similar(bookTitle, entryTitle, Math.Min(bookTitle.Length, entryTitle.Length) / 6) != -1)
                {
                    string price = priceData[x].InnerText.Trim();
                    string priceCheck = PriceRangeRegex().Match(price).Groups[1].Value;
                    string stockStatus = stockStatusData[x].InnerText.Trim();

                    entryTitle = CleanAndParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType);

                    if (!entryTitle.ContainsAny(_checkDescStrings))
                    {
                        _logger.CheckingDescription(entryTitle);
                        string href = titleData[x].GetAttributeValue<string>("href", "ERROR");
                        HtmlDocument descDoc = await resolveDescDoc(href).ConfigureAwait(false);
                        HtmlNode descNode = descDoc.DocumentNode.SelectSingleNode(_entryDescXPath);
                        if (descNode is not null && descNode.InnerText.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
                        {
                            entryTitle += " Box Set";
                        }
                    }

                    data.Add(new EntryModel(
                        entryTitle,
                        string.IsNullOrWhiteSpace(priceCheck) ? price : priceCheck,
                        stockStatus switch
                        {
                            string status when status.Contains("Pre-Order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                            string status when status.Contains("Add to Cart", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                            _ => StockStatus.OOS,
                        },
                        TITLE));
                }
                else
                {
                    _logger.EntryRemovedDebug(2, entryTitle);
                }
            }
        }

        data.TrimExcess();
        data.SortByVolume();

        // For Manga scrapes, prune entries that don't look like real volumes — unless the
        // result set contained no "Vol" markers at all (one-shot / artbook series, where
        // every entry is the parent product).
        if (bookType == BookType.Manga)
        {
            bool hasAnyVol = false;
            foreach (EntryModel e in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(data))
            {
                if (e.Entry.Contains("Vol")) { hasAnyVol = true; break; }
            }
            if (hasAnyVol)
            {
                data.RemoveAll(entry => !entry.Entry.ContainsAny(_mangaKeepStrings) && entry.ParsePrice() <= 50);
            }
        }

        return data;
    }

    /// <summary>
    /// Helper for fixture-based tests: returns a desc-resolver closure that looks each href
    /// up in <paramref name="hrefToDoc"/>. Throws <see cref="KeyNotFoundException"/> if the
    /// parse path requests a fixture that wasn't saved — that's a regenerate gap and should
    /// fail loudly rather than silently mis-render.
    /// </summary>
    internal static Func<string, Task<HtmlDocument>> CreateOfflineDescResolver(IReadOnlyDictionary<string, HtmlDocument> hrefToDoc)
        => href => hrefToDoc.TryGetValue(href, out HtmlDocument? doc)
            ? Task.FromResult(doc)
            : throw new KeyNotFoundException(
                $"Desc fixture missing for href '{href}'. Re-run the Regenerate task to capture it.");
}