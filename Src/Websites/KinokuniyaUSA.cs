using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class KinokuniyaUSA : IWebsite
{
    private readonly ILogger _logger;

    public KinokuniyaUSA(ILogger<KinokuniyaUSA>? logger = null)
    {
        _logger = logger ?? NullLogger<KinokuniyaUSA>.Instance;
    }
    
    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//span[@class='underline']");
    private static readonly XPathExpression _memberPriceXPath = XPathExpression.Compile("//li[@class='price'][2]/span");
    private static readonly XPathExpression _nonMemberPriceXPath = XPathExpression.Compile("//li[@class='price'][1]/span");
    private static readonly XPathExpression _descXPath = XPathExpression.Compile("//p[@class='description']");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//li[@class='status']");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//div[@class='categoryPager']/ul/li[last()]/a");
    
    [GeneratedRegex(@"\((?:Omnibus\s*|\d{1,3}\s*In\s*\d{1,3}\s*)Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"\(Light Novel\)|Light Novel|Novel", RegexOptions.IgnoreCase)] private static partial Regex NovelRegex();
    [GeneratedRegex(@"\((.*?)\)+", RegexOptions.IgnoreCase)] private static partial Regex TitleCaptureRegex();
    [GeneratedRegex(@"^[^\(]+", RegexOptions.IgnoreCase)] private static partial Regex CleanInFrontTitleRegex();
    [GeneratedRegex(@"(?<=\d{1,3})[^\d{1,3}].*", RegexOptions.IgnoreCase)] private static partial Regex CleanBehindTitleRegex();
    [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\({1,}.*?\){1,}|<.*?>|w/DVD|<|>|(?<=\d{1,3})\s+:.*", RegexOptions.IgnoreCase)] private static partial Regex MangaTitleFixRegex();
    [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\({1,}.*?\){1,}|(?<=\d{1,3})\s?:.*|<.*?[^\d+]>|w/DVD|<|>", RegexOptions.IgnoreCase)] private static partial Regex NovelTitleFixRegex();
    [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"\d{1,3}\.\d{1,3}|\d{1,3}")] private static partial Regex FindVolNumRegex();

    /// <inheritdoc />
    public const string TITLE = "Kinokuniya USA";

    /// <inheritdoc />
    public const string BASE_URL = "https://united-states.kinokuniya.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    private static readonly int STATUS_START_INDEX = "Availability Status : ".Length;
    private static readonly FrozenSet<string> _skipBookTitles = ["Attack on Titan"];
    private static readonly FrozenSet<string> _bookTypeKeyWords = ["Vol", "Box Set", "Anniversary"];

    // Search URL (no server-side category filter — Manga/Novel narrowing happens via the
    // on-page "Manga" facet click). Old code passed `taxon=2&x=39&y=11&page=1&per_page=100`
    // which no longer works after Kinokuniya's redesign.
    //   https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=jujutsu+kaisen&taxon=&x=0&y=0
    //   https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=classroom+of+the+elite+novel&taxon=&x=0&y=0

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunPlaywrightScrapeAsync(
            this, Website.KinokuniyaUSA, bookTitle, bookType, masterDataList, masterLinkList, errors, browser!, curRegion, cancellationToken,
            isMember: memberships.HasFlag(Membership.KinokuniyaUSA),
            needsUserAgent: true);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    internal string GenerateWebsiteUrl(string bookTitle, BookType bookType)
    {
        string url = $"{BASE_URL}/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords={bookTitle.Replace(" ", "+")}{(bookType == BookType.LightNovel ? "+novel" : string.Empty)}&taxon=&x=0&y=0";
        _logger.UrlGenerated(url);
        return url;
    }

    private static async Task WaitForPageLoad(IPage page, int timeoutMilliseconds = 30000)
    {
        // The locator for your loading element
        ILocator loadingElement = page.Locator("#loading");

        DateTime startTime = DateTime.Now;
        TimeSpan maxTime = TimeSpan.FromMilliseconds(timeoutMilliseconds);

        // Loop until the element is hidden or the timeout is reached
        while (true)
        {
            // Check if the element is hidden
            bool isHidden = await loadingElement.IsHiddenAsync().ConfigureAwait(false);

            if (isHidden)
            {
                return; // The element is hidden, success!
            }

            // If it's not hidden, check if we've run out of time
            if (DateTime.Now - startTime > maxTime)
            {
                // Throw an exception if the element doesn't disappear in time
                throw new TimeoutException("The loading element did not disappear within the specified timeout.");
            }

            // Wait a short duration before checking again
            await Task.Delay(100).ConfigureAwait(false);
        }
    }

    private static string ParseAndCleanTitle(string entryTitle, BookType bookType, string bookTitle, string entryDesc, bool oneShotCheck)
    {
        // 1) Cheap pre-computations / span-based checks
        bool entryTitleHasDigit = ContainsDigit(entryTitle);
        bool bookTitleHasDigit  = ContainsDigit(bookTitle);

        // Avoid doing Replace if not needed (micro); Replace returns original if not found, but we skip the call entirely
        if (bookTitle.IndexOf('-', StringComparison.Ordinal) < 0 && entryTitle.IndexOf('-', StringComparison.Ordinal) >= 0)
        {
            entryTitle = entryTitle.Replace("-", " ");
        }

        string output;

        // 2) Front cleanup / capture once
        string parseCheckTitle = TitleCaptureRegex().Match(entryTitle).Groups[1].Value;
        string checkBeforeText = CleanInFrontTitleRegex().Match(entryTitle).Value;

        if (!_skipBookTitles.Contains(bookTitle, StringComparer.OrdinalIgnoreCase)
            && parseCheckTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase)
            && !checkBeforeText.Contains(bookTitle, StringComparison.OrdinalIgnoreCase)
            && entryTitleHasDigit
            && !bookTitleHasDigit)
        {
            // Concat avoids building an intermediate then calling Insert on a new instance
            string cleaned = CleanInFrontTitleRegex().Replace(entryTitle, string.Empty);
            entryTitle = string.Concat(parseCheckTitle, " ", cleaned);
        }

        if (!oneShotCheck)
        {
            // 3) Regex transforms in a single pass per regex (avoid nesting; same work, clearer)
            string newEntryTitle = FixVolumeRegex().Replace(entryTitle, "Vol");
            newEntryTitle = OmnibusRegex().Replace(newEntryTitle, "Omnibus");

            if (bookType == BookType.LightNovel)
            {
                newEntryTitle = NovelRegex().Replace(newEntryTitle, "Novel");
                newEntryTitle = NovelTitleFixRegex().Replace(newEntryTitle, "$1");
            }
            else
            {
                newEntryTitle = MangaTitleFixRegex().Replace(newEntryTitle, "$1");
            }

            // 4) Work in StringBuilder, minimize ToString()
            StringBuilder curTitle = new(newEntryTitle.Length + 16);
            curTitle.Append(newEntryTitle);
            curTitle.Replace(",", string.Empty);
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

            // We need a string snapshot a few times; take it once, reuse, then invalidate only when needed
            string curSnapshot = curTitle.ToString().Trim();

            if (bookType == BookType.LightNovel)
            {
                // Insert book title at front if missing
                if (!curSnapshot.Contains(bookTitle, StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Insert(0, $"{char.ToUpperInvariant(bookTitle[0])}{bookTitle.AsSpan(1)} ");
                    curSnapshot = null!; // invalidate snapshot
                }

                string snapshot = curSnapshot ??= curTitle.ToString();
                bool containsNovel = snapshot.Contains("Novel", StringComparison.OrdinalIgnoreCase);
                bool containsVol   = snapshot.Contains("Vol",   StringComparison.OrdinalIgnoreCase);

                if (!containsNovel && !containsVol)
                {
                    // Reset builder with cleaned string to avoid constructing then discarding
                    string cleanedBehind = CleanBehindTitleRegex().Replace(snapshot, string.Empty);
                    curTitle.Clear().Append(cleanedBehind);
                    curSnapshot = null!;
                }
                else if (!containsNovel && containsVol)
                {
                    int volIdx = snapshot.IndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                    if (volIdx >= 0) curTitle.Insert(volIdx, "Novel ");
                    curSnapshot = null!;
                }

                snapshot = (curSnapshot ??= curTitle.ToString());
                if (!snapshot.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                    && !ContainsDigit(snapshot)
                    && !bookTitleHasDigit)
                {
                    curTitle.Append(" Novel");
                    curSnapshot = null!;
                }
            }
            else if (bookType == BookType.Manga && !newEntryTitle.ContainsAny(_bookTypeKeyWords))
            {
                // Only compute the vol match once
                Match volMatchForManga = FindVolNumRegex().Match(newEntryTitle);
                if (volMatchForManga.Success && !bookTitleHasDigit)
                {
                    curTitle.Insert(volMatchForManga.Index, "Vol ");
                    curSnapshot = null!;
                }
                else
                {
                    // avoid calling Contains twice on entryDesc
                    bool hasCollection = entryDesc.Contains("Collection", StringComparison.OrdinalIgnoreCase);
                    bool hasVolumes    = entryDesc.Contains("volumes",    StringComparison.OrdinalIgnoreCase);
                    if (hasCollection && hasVolumes)
                    {
                        curTitle.Append(" Box Set");
                        curSnapshot = null!;
                    }
                }
            }

            // Special series cleanup
            if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace("Naruto Next Generations", string.Empty);
                curSnapshot = null!;
            }

            // If entry title has digits but we don't already have "Vol" in the *current* string,
            // try to move detected vol number to the end as "Vol X"
            if (entryTitleHasDigit)
            {
                string snapshot = curSnapshot ?? (curSnapshot = curTitle.ToString());
                if (!snapshot.Contains("Vol", StringComparison.Ordinal) && !entryTitle.ContainsAny(new[] { "Box Set", "Anniversary" }))
                {
                    Match volNum = FindVolNumRegex().Match(snapshot);
                    if (volNum.Success)
                    {
                        curTitle.Remove(volNum.Index, volNum.Value.Length);
                        bool needsNovelLabel = bookType == BookType.LightNovel
                                            && snapshot.IndexOf("Novel", StringComparison.OrdinalIgnoreCase) < 0;

                        if (needsNovelLabel)
                        {
                            curTitle.Append(" Novel");
                        }

                        curTitle.Append(" Vol ").Append(volNum.Value);
                        curSnapshot = null!;
                    }
                }
            }

            // Noragami series tweak
            {
                string snapshot = curSnapshot ?? (curSnapshot = curTitle.ToString());
                if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase)
                    && snapshot.IndexOf("Omnibus",       StringComparison.OrdinalIgnoreCase) < 0
                    && snapshot.IndexOf("Stray Stories", StringComparison.OrdinalIgnoreCase) < 0
                    && snapshot.IndexOf("Stray God",     StringComparison.OrdinalIgnoreCase) < 0)
                {
                    int volIdx = snapshot.IndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                    if (volIdx >= 0)
                    {
                        curTitle.Insert(volIdx, "Stray God ");
                        curSnapshot = null!;
                    }
                }
            }

            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

            // Final squish + trim
            string squished = MasterScrape.MultipleWhiteSpaceRegex()
                .Replace(curTitle.Replace("Manga", string.Empty).ToString().Trim(), " ");
            output = squished;
        }
        else
        {
            // oneShotCheck path
            string cleaned = entryTitle.Replace("Manga", string.Empty).Replace(",", string.Empty);
            cleaned = FixVolumeRegex().Replace(cleaned, "Vol");

            if (bookType == BookType.Manga)
            {
                output = MasterScrape.MultipleWhiteSpaceRegex()
                    .Replace(MangaTitleFixRegex().Replace(cleaned, "$1").Trim(), " ");
            }
            else
            {
                cleaned = NovelTitleFixRegex().Replace(cleaned, "$1");
                output  = MasterScrape.MultipleWhiteSpaceRegex().Replace(cleaned.Trim(), " ");
            }
        }

        // Keep original semantics: insert "Special Edition " before "Vol" if the *entryTitle* has that phrase
        if (entryTitle.Contains("Special Edition", StringComparison.OrdinalIgnoreCase))
        {
            int volIdx = output.IndexOf("Vol", StringComparison.Ordinal);
            // original code assumes "Vol" exists; do the same (will throw if not found, matching prior behavior)
            output = output.Insert(volIdx, "Special Edition ");
        }

        static bool ContainsDigit(string s)
        {
            ReadOnlySpan<char> span = s.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (char.IsDigit(span[i])) return true;
            }
            return false;
        }

        return MasterScrape.FinalCleanRegex().Replace(output, string.Empty);
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        string url = GenerateWebsiteUrl(bookTitle, bookType);
        links.Add(url);
        await page!.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
        await WaitForPageLoad(page).ConfigureAwait(false);

        DumpDebugHtml(await page.ContentAsync().ConfigureAwait(false), "after_load");

        // Best-effort UI fiddling — expand per-page to 100, switch to list view, filter to Manga.
        await TrySelectPerPageAsync(page, 100).ConfigureAwait(false);
        try
        {
            await page.Locator("li#detail-button a:has-text(\"List\")").ForceClickAsync(timeout: 5000).ConfigureAwait(false);
            await WaitForPageLoad(page).ConfigureAwait(false);
            _logger.ClickedListMode();
        }
        catch (TimeoutException) { }
        catch (PlaywrightException) { }

        if (bookType == BookType.Manga)
        {
            try
            {
                await page.GetByText("Manga", new PageGetByTextOptions { Exact = true })
                    .ForceClickAsync(timeout: 5000).ConfigureAwait(false);
                await WaitForPageLoad(page).ConfigureAwait(false);
                _logger.ClickedManga();
            }
            catch (TimeoutException) { }
            catch (PlaywrightException) { }
        }

        // Walk pagination via Playwright, capturing each settled DOM. Parsing happens in
        // ParsePages so fixture-based tests can replay.
        List<HtmlDocument> listingPages = [];
        int maxPageCount = -1, curPageNum = 1;
        while (true)
        {
            HtmlDocument doc = HtmlFactory.CreateDocument();
            doc.LoadHtml(await page.ContentAsync().ConfigureAwait(false));
            listingPages.Add(doc);

            if (maxPageCount == -1)
            {
                HtmlNode? pageNode = doc.DocumentNode.SelectSingleNode(_pageCheckXPath);
                maxPageCount = pageNode is not null && int.TryParse(pageNode.InnerText, out int parsed) ? parsed : 1;
                _logger.MaxPageCount(maxPageCount);
            }

            if (curPageNum >= maxPageCount) break;
            curPageNum++;
            try
            {
                await page.Locator("p.pagerArrowR").ForceClickAsync().ConfigureAwait(false);
            }
            catch (TimeoutException) { break; }
            catch (PlaywrightException) { break; }
            await WaitForPageLoad(page).ConfigureAwait(false);
            _logger.PageVisited(curPageNum, page.Url);
        }

        data = ParsePages(listingPages, bookTitle, bookType, isMember);
        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        return (data, links);
    }

    /// <summary>
    /// Parses pre-loaded KinokuniyaUSA listing docs into <see cref="EntryModel"/>s.
    /// Fixture-based tests load the saved HTML and call this directly — no Playwright.
    /// </summary>
    internal List<EntryModel> ParsePages(IReadOnlyList<HtmlDocument> listingPages, string bookTitle, BookType bookType, bool isMember)
    {
        List<EntryModel> data = [];
        bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);
        bool bookTitleHasDigit = HasAnyDigit(bookTitle);
        XPathExpression priceXPath = isMember ? _memberPriceXPath : _nonMemberPriceXPath;
        HashSet<string> seenTitles = new(StringComparer.OrdinalIgnoreCase);

        // For the one-shot heuristic we need a global "did the whole search return a single
        // result on a single page" answer — compute up front from the snapshotted docs.
        int totalPages = listingPages.Count;
        bool singlePageResult = totalPages == 1;

        foreach (HtmlDocument doc in listingPages)
        {
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();
            List<string> titles = CollectValues(nav, _titleXPath);
            int entryCount = titles.Count;
            if (entryCount == 0) continue;

            List<string> prices = CollectValues(nav, priceXPath, entryCount);
            List<string> descs = CollectValues(nav, _descXPath, entryCount);
            List<string> stocks = CollectValues(nav, _stockStatusXPath, entryCount);

            bool oneShotCheck = singlePageResult
                && entryCount == 1
                && !titles[0].Contains("Vol", StringComparison.OrdinalIgnoreCase);

            for (int i = 0; i < entryCount; i++)
            {
                string entryTitle = WebUtility.HtmlDecode(titles[i]);
                string entryDesc = descs[i];

                if (!(
                        InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && (
                                (
                                    bookType == BookType.Manga
                                    && (
                                        InternalHelpers.RemoveEntryTitleCheck(bookTitle, entryTitle, "Novel")
                                        || entryTitle.Contains("graphic novel", StringComparison.OrdinalIgnoreCase)
                                        )
                                    && !entryTitle.Contains("Chapter Book", StringComparison.OrdinalIgnoreCase)
                                    && (
                                        oneShotCheck ||
                                        FixVolumeRegex().IsMatch(entryTitle) ||
                                        entryDesc.ContainsAny(["Collection", "volumes", "color edition", "box set"]) ||
                                        (HasAnyDigit(entryTitle) && !bookTitleHasDigit))
                                    && !(
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony") ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear Your Own World") ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, ["Itachi's Story", "Boruto"]) ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Attack on Titan", entryTitle, ["Kuklo", "end of the world"]) ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented")
                                    )
                                )
                                ||
                                (
                                    bookType == BookType.LightNovel
                                    && !entryTitle.Contains("graphic novel", StringComparison.OrdinalIgnoreCase)
                                    && entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                )
                            )
                    ))
                {
                    _logger.EntryRemoved(1, entryTitle);
                    continue;
                }

                _logger.TitleBefore(entryTitle);
                string cleaned = ParseAndCleanTitle(entryTitle, bookType, bookTitle, entryDesc, oneShotCheck);
                _logger.TitleAfter(cleaned);

                if (!seenTitles.Add(cleaned))
                {
                    _logger.EntryRemoved(2, cleaned);
                    continue;
                }

                string rawStock = stocks[i].Trim();
                StockStatus stockStatus = rawStock.Length >= STATUS_START_INDEX
                    ? rawStock.AsSpan(STATUS_START_INDEX) switch
                    {
                        "In stock at the Fulfilment Center." => StockStatus.IS,
                        "Available for Pre Order" => StockStatus.PO,
                        "Out of stock." => StockStatus.OOS,
                        "Available for order from suppliers." => StockStatus.BO,
                        _ => StockStatus.NA,
                    }
                    : StockStatus.NA;

                data.Add(new EntryModel(cleaned, prices[i].Trim(), stockStatus, TITLE));
            }
        }

        data.TrimExcess();
        data.SortByVolume();
        return data;
    }

    /// <summary>
    /// Tight inline replacement for <c>str.Any(char.IsDigit)</c> — avoids the LINQ
    /// enumerator alloc on a per-entry hot path.
    /// </summary>
    private static bool HasAnyDigit(string str)
    {
        foreach (char c in str.AsSpan())
        {
            if (c >= '0' && c <= '9') return true;
        }
        return false;
    }

    /// <summary>
    /// Best-effort attempt to set the listing's per-page count to <paramref name="perPage"/>.
    /// The <c>&lt;select name='per_page'&gt;</c> control is in the DOM but CSS-hidden,
    /// so <see cref="ILocator.SelectOptionAsync"/> blocks indefinitely on the visibility
    /// check. We bypass it by setting <c>value</c> + dispatching a <c>change</c> event in
    /// JS, which is exactly what the page's own click handler does behind the scenes.
    /// </summary>
    private async Task TrySelectPerPageAsync(IPage page, int perPage)
    {
        try
        {
            bool dispatched = await page.EvaluateAsync<bool>(
                @"(value) => {
                    const s = document.querySelector(""select[name='per_page']"");
                    if (!s) return false;
                    if (![...s.options].some(o => o.value == value)) return false;
                    s.value = value;
                    s.dispatchEvent(new Event('change', { bubbles: true }));
                    return true;
                }",
                perPage.ToString()).ConfigureAwait(false);

            if (dispatched)
            {
                await WaitForPageLoad(page).ConfigureAwait(false);
                _logger.SelectedPerPage(perPage);
            }
        }
        catch (PlaywrightException) { /* swallow — per-page sizing is best-effort */ }
        catch (TimeoutException) { }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private static void DumpDebugHtml(string html, string label)
    {
        try
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"KinokuniyaUSA_{label}.html"), html ?? string.Empty);
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
}