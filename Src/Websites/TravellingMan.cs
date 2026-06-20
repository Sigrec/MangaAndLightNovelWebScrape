using System.Collections.Frozen;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class TravellingMan : IWebsite
{
    private readonly ILogger _logger;

    public TravellingMan(ILogger<TravellingMan>? logger = null)
    {
        _logger = logger ?? NullLogger<TravellingMan>.Instance;
    }

    /// <inheritdoc />
    public const string TITLE = "TravellingMan";
    /// <inheritdoc />
    public const string BASE_URL = "https://travellingman.com";
    /// <inheritdoc />
    public const Region REGION = Region.Britain;

    // FrozenSet not List — the old `static readonly List<string>` was being MUTATED per
    // scrape (lines that removed items matching the book title or "novel" for LN). Static
    // mutable state means concurrent scrapes interleaved removals and after the first run
    // some items were gone for the rest of the process lifetime. Per-scrape effective set
    // is built locally inside GetData.
    private static readonly FrozenSet<string> _descRemovalDefaults =
        FrozenSet.ToFrozenSet(["novel", "figure", "sculpture", "collection of", "figurine", "statue", "miniature", "Figuarts"], StringComparer.OrdinalIgnoreCase);

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/div/div[2]/div/span");
    private static readonly XPathExpression _priceXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/div/div[3]/dl/div[2]/dd[2]/span[1]");
    private static readonly XPathExpression _entryLinkXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/a");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//ul[@class='list--inline pagination']/li[3]/a");
    private static readonly XPathExpression _descXPath = XPathExpression.Compile("//div[@class='product-single__description rte'] | //div[@class='product-single__description rte']//p");

    [GeneratedRegex(@"Volume|Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@",| The Manga| Manga|\(.*?\)", RegexOptions.IgnoreCase)] private static partial Regex CleanAndParseTitleRegex();
    [GeneratedRegex(@"\d{1,3}-in-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"(?<=Box Set \d{1,3})[^\d{1,3}.]+.*|(?:Box Set) Vol")] private static partial Regex BoxSetRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunHtmlScrapeAsync(
            this, Website.TravellingMan, bookTitle, bookType, masterDataList, masterLinkList, errors, curRegion, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    internal string GenerateWebsiteUrl(string bookTitle, BookType bookType, int curPage)
    {
        // https://travellingman.com/search?page=2&q=naruto
        string url = $"{BASE_URL}/search?page={curPage}&q={bookTitle.Replace(' ', '+')}";
        _logger.PageUrlGenerated(curPage, url);
        return url;
    }

    /// <summary>
    /// Cheap sanity check matching what <see cref="EntryModel.ParsePrice"/> needs: a
    /// leading or trailing currency symbol with digits inbetween. Lets us skip merchandise
    /// entries (no price element) before they reach the dedup/sort pass.
    /// </summary>
    private static bool LooksLikePrice(string price)
    {
        if (price.Length < 2) return false;
        // Either first char is a digit (e.g. "1099¥") or first char is a currency symbol
        // followed by at least one digit.
        if (char.IsDigit(price[0]))
        {
            return char.IsDigit(price[^1]) || !char.IsLetterOrDigit(price[^1]);
        }
        return price.Length >= 2 && char.IsDigit(price[1]);
    }

    private static string CleanAndParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (OmnibusRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
        }
        else
        {
            if (BoxSetRegex().IsMatch(entryTitle))
            {
                entryTitle = BoxSetRegex().Replace(entryTitle, "Box Set");

                // Trim trailing "... Box Set" if present (span-based)
                ReadOnlySpan<char> es = entryTitle.AsSpan();
                ReadOnlySpan<char> boxSet = "Box Set".AsSpan();
                if (es.EndsWith(boxSet, StringComparison.Ordinal))
                {
                    int last = es.LastIndexOf(boxSet, StringComparison.Ordinal);
                    if (last >= 0) entryTitle = entryTitle.Substring(0, last);
                }
            }
        }

        // Build mutable title once after regex cleanup
        StringBuilder curTitle = new(CleanAndParseTitleRegex().Replace(entryTitle, string.Empty));
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

        // Precompute span views for cheap checks
        ReadOnlySpan<char> entrySpan = entryTitle.AsSpan();
        ReadOnlySpan<char> bookSpan = bookTitle.AsSpan();

        // Remove " Hardcover"/" HC" only when present in entry but not in book (case-sensitive, matches original)
        if (entrySpan.Contains("Hardcover".AsSpan(), StringComparison.Ordinal) &&
            !bookSpan.Contains("Hardcover".AsSpan(), StringComparison.Ordinal))
        {
            curTitle.Replace(" Hardcover", string.Empty);
        }

        if (entrySpan.Contains("HC".AsSpan(), StringComparison.Ordinal) &&
            !bookSpan.Contains("HC".AsSpan(), StringComparison.Ordinal))
        {
            curTitle.Replace(" HC", string.Empty);
        }

        // AoT box-set numerals (keep original case behavior: Contains is CI, Replace is exact token)
        if (entrySpan.Contains("Box Set".AsSpan(), StringComparison.Ordinal) &&
            bookTitle.Equals("attack on titan", StringComparison.OrdinalIgnoreCase))
        {
            if (entrySpan.Contains("One".AsSpan(), StringComparison.OrdinalIgnoreCase))  curTitle.Replace("One", "1");
            else if (entrySpan.Contains("Two".AsSpan(), StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Two", "2");
            else if (entrySpan.Contains("Three".AsSpan(), StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Three", "3");
        }

        if (bookType == BookType.LightNovel)
        {
            curTitle.Replace("Light Novel", string.Empty);

            // Insert "Novel " before "Vol" if present, else append " Novel" (avoid ToString allocation)
            int volIdx = curTitle.IndexOfOrdinal("Vol");
            if (volIdx != -1) curTitle.Insert(volIdx, "Novel ");
            else curTitle.Insert(curTitle.Length, " Novel");
        }
        else if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace("Naruto Next Generations", string.Empty);
        }

        string result = curTitle.ToString();

        // Keep your custom ContainsAny extension untouched
        if (!result.ContainsAny(["Box Set", "Anniversary"]))
        {
            result = MasterScrape.FinalCleanRegex().Replace(result, string.Empty);
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(result, " ").Trim();
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        HtmlWeb web = HtmlFactory.CreateWeb();

        // Walk pagination, gathering listing docs. The parse + per-entry desc resolution
        // lives in ParsePages so fixture-based tests can drive the same code path.
        List<HtmlDocument> listingPages = [];
        int nextPage = 1;
        while (true)
        {
            string url = GenerateWebsiteUrl(bookTitle, bookType, nextPage);
            links.Add(url);

            HtmlDocument doc = await web.LoadFromWebAsync(url);
            doc.ConfigurePerf();
            listingPages.Add(doc);

            HtmlNodeCollection? titleData = doc.DocumentNode.SelectNodes(_titleXPath);
            HtmlNodeCollection? priceData = doc.DocumentNode.SelectNodes(_priceXPath);
            HtmlNode? pageCheck = doc.DocumentNode.SelectSingleNode(_pageCheckXPath);
            if (titleData is null || priceData is null) break;
            if (!(priceData.Count == titleData.Count && pageCheck is not null)) break;
            nextPage++;
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
                HtmlDocument descDoc = await web.LoadFromWebAsync(fullUrl);
                return descDoc;
            });

        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        return (data, links);
    }

    /// <summary>
    /// Runs the per-page parse + per-entry filter + desc-keyword check on pre-loaded
    /// listing docs. The <paramref name="resolveDescDoc"/> delegate fetches a product
    /// detail page from a (possibly relative) href — live path uses <see cref="HtmlWeb"/>;
    /// tests pass an offline dictionary lookup so the run is fully reproducible.
    /// </summary>
    internal async Task<List<EntryModel>> ParsePages(
        IReadOnlyList<HtmlDocument> listingPages,
        string bookTitle,
        BookType bookType,
        Func<string, Task<HtmlDocument>> resolveDescDoc)
    {
        List<EntryModel> data = [];

        bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
        string normalizedBookTitle = InternalHelpers.NormalizeForTitleMatch(bookTitle);
        bool bookTitleContainsNovel = bookTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase);
        bool bookTitleContainsManga = bookTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase);

        // Per-scrape effective desc-removal set. Old code mutated a static List on every
        // scrape (corrupting it across concurrent runs); we now build a fresh local set
        // from the immutable default and drop terms the user's bookTitle already implies.
        HashSet<string> descRemoval = new(_descRemovalDefaults, StringComparer.OrdinalIgnoreCase);
        foreach (string term in _descRemovalDefaults)
        {
            if (bookTitle.Contains(term, StringComparison.OrdinalIgnoreCase)) descRemoval.Remove(term);
        }
        if (bookType == BookType.LightNovel) descRemoval.Remove("novel");

        foreach (HtmlDocument doc in listingPages)
        {
            HtmlNodeCollection? titleData = doc.DocumentNode.SelectNodes(_titleXPath);
            HtmlNodeCollection? priceData = doc.DocumentNode.SelectNodes(_priceXPath);
            HtmlNodeCollection? linkData = doc.DocumentNode.SelectNodes(_entryLinkXPath);
            if (titleData is null || priceData is null) continue;

            int entryCount = titleData.Count;
            string[] entryTitles = new string[entryCount];
            string[] prices = new string[entryCount];
            string?[] hrefs = new string?[entryCount];

            for (int i = 0; i < entryCount; i++)
            {
                entryTitles[i] = titleData[i].InnerText.Trim();
                prices[i] = i < priceData.Count ? priceData[i].InnerText.Trim() : string.Empty;
                hrefs[i] = linkData is not null && i < linkData.Count
                    ? linkData[i].GetAttributeValue<string?>("href", null)
                    : null;
            }

            // Eligibility pre-pass. Each eligible Manga entry without a Vol/Box-Set marker
            // AND every LightNovel entry needs the product detail page checked. Old code
            // awaited the fetch sequentially inside the entry loop — collect indices,
            // batch through the resolver.
            bool[] eligible = new bool[entryCount];
            List<int> needsDesc = [];

            for (int i = 0; i < entryCount; i++)
            {
                string entryTitle = entryTitles[i];

                if (entryTitle.ContainsAny(["Banpresto", "Nendoroid"])
                    || !InternalHelpers.EntryTitleContainsNormalizedBookTitle(normalizedBookTitle, entryTitle)
                    || (InternalHelpers.ShouldRemoveEntry(entryTitle) && !BookTitleRemovalCheck))
                {
                    _logger.EntryRemoved(1, entryTitle);
                    continue;
                }

                bool mangaPath = bookType == BookType.Manga
                    && !bookTitleContainsNovel
                    && !entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                    && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto");

                bool novelPath = bookType == BookType.LightNovel
                    && !entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase)
                    && !bookTitleContainsManga
                    && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Kharadron");

                if (!(mangaPath || novelPath)
                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "Unimplemented"))
                {
                    _logger.EntryRemoved(1, entryTitle);
                    continue;
                }

                eligible[i] = true;

                if (bookType == BookType.LightNovel
                    || !entryTitle.ContainsAny(["Vol.", "Volume", "Box Set", "Comic"]))
                {
                    if (!string.IsNullOrWhiteSpace(hrefs[i])) needsDesc.Add(i);
                }
            }

            HtmlDocument?[] descDocs = new HtmlDocument?[entryCount];
            if (needsDesc.Count > 0)
            {
                Task<HtmlDocument>[] fetches = new Task<HtmlDocument>[needsDesc.Count];
                for (int j = 0; j < needsDesc.Count; j++)
                {
                    fetches[j] = resolveDescDoc(hrefs[needsDesc[j]]!);
                }
                HtmlDocument[] docs = await Task.WhenAll(fetches);
                for (int j = 0; j < needsDesc.Count; j++)
                {
                    descDocs[needsDesc[j]] = docs[j];
                }
            }

            for (int i = 0; i < entryCount; i++)
            {
                if (!eligible[i]) continue;
                string entryTitle = entryTitles[i];

                bool descIsValid = true;
                if (descDocs[i] is HtmlDocument descDoc)
                {
                    HtmlNodeCollection? descNodes = descDoc.DocumentNode.SelectNodes(_descXPath.Expression);
                    StringBuilder descBuilder = new();
                    if (descNodes is not null)
                    {
                        foreach (HtmlNode node in descNodes) descBuilder.AppendLine(node.InnerText);
                    }
                    string descText = descBuilder.ToString();
                    descIsValid = bookType == BookType.LightNovel
                        ? entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) || descText.Contains("novel", StringComparison.OrdinalIgnoreCase)
                        : !descText.ContainsAny(descRemoval);
                }

                if (!descIsValid)
                {
                    _logger.EntryRemoved(2, entryTitle);
                    continue;
                }

                // Skip entries with no parseable price (merch, gift cards, etc).
                string price = prices[i];
                if (string.IsNullOrWhiteSpace(price) || !LooksLikePrice(price))
                {
                    _logger.EntryRemoved(2, entryTitle);
                    continue;
                }

                data.Add(new EntryModel(
                    CleanAndParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                    price,
                    StockStatus.IS,
                    TITLE));
            }
        }

        data.SortByVolume();
        data.RemoveDuplicates(_logger);
        return data;
    }

    /// <summary>
    /// Test helper: returns a resolver that looks each href up in <paramref name="hrefToDoc"/>.
    /// Throws when a fixture is missing — that's a regenerate gap, not a silent miscompare.
    /// </summary>
    internal static Func<string, Task<HtmlDocument>> CreateOfflineDescResolver(IReadOnlyDictionary<string, HtmlDocument> hrefToDoc)
        => href => hrefToDoc.TryGetValue(href, out HtmlDocument? doc)
            ? Task.FromResult(doc)
            : throw new KeyNotFoundException(
                $"Desc fixture missing for href '{href}'. Re-run the Regenerate task to capture it.");
}