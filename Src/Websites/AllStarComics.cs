using System.Buffers;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

/// <summary>
/// All Star Comics Melbourne — Australian comic / manga retailer running on Shopify.
/// Same DOM shape as MangaMart (title + inline price + add-to-cart button on the listing
/// card) but pure HTML — no JS rendering required. Prices already include AUD on the
/// search page, so no per-product detail fetch is needed.
/// </summary>
public sealed partial class AllStarComics : IWebsite
{
    private readonly ILogger _logger;

    public AllStarComics(ILogger<AllStarComics>? logger = null)
    {
        _logger = logger ?? NullLogger<AllStarComics>.Instance;
    }

    /// <inheritdoc />
    public const string TITLE = "All Star Comics";
    /// <inheritdoc />
    public const string BASE_URL = "https://allstarcomics.com.au";
    /// <inheritdoc />
    public const Region REGION = Region.Australia;

    private static readonly XPathExpression _productCardXPath = XPathExpression.Compile("//div[contains(@class,'product-item') and contains(@class,'product-item--vertical')]");
    private static readonly XPathExpression _titleRelXPath = XPathExpression.Compile(".//a[contains(@class,'product-item__title')]");
    private static readonly XPathExpression _priceRelXPath = XPathExpression.Compile(".//span[contains(@class,'price')]");
    private static readonly XPathExpression _addToCartBtnRelXPath = XPathExpression.Compile(".//button[contains(@class,'product-item__action-button')]");
    private static readonly XPathExpression _nextPageXPath = XPathExpression.Compile("//a[contains(@class,'pagination__next')]");

    // Vectorized multi-substring scan replaces 6 sequential case-insensitive `Contains`
    // checks per card. One pass per entry instead of six.
    private static readonly SearchValues<string> _mangaMarkerSearch = SearchValues.Create(
        ["GN VOL", "TP VOL", "HC VOL", "BOX SET", "OMNIBUS", "3-IN-1"],
        StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"GN\s+VOL|\bV(?=\d)|Volume|Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"\s*\(C:\s*[\d\-]+\)|,|\s+HC\b|\s+TP\b", RegexOptions.IgnoreCase)] private static partial Regex CleanTitleRegex();
    [GeneratedRegex(@"\d{1,3}-IN-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    // Diamond catalog zero-pads single-digit volume numbers ("Vol 01", "Box Set 02").
    // Match the marker + space + one-or-more leading zeros + the surviving digit; rewrite
    // to "<marker> <digit>" so single-digit volumes line up with the rest of the library's
    // canonical "Vol N" format (also dedups correctly against other sites' "Vol 1" etc).
    [GeneratedRegex(@"\b(Vol|Box Set|Omnibus)\s+0+(\d)\b", RegexOptions.IgnoreCase)] private static partial Regex StripLeadingZeroRegex();
    // Strip everything after the volume number — Diamond catalog appends edition markers
    // like " New Ptg", " (Mr)", " (Mature)" that vary between reprints of the same volume.
    // Without this, "Vol 8 New Ptg (Mr)" and "Vol 8 (Mr)" survive as separate rows and the
    // cross-site dedup can't collapse them against other retailers' clean "Vol 8".
    //
    // Capture-group form (not lookbehind) — earlier lookbehind variant matched non-greedily
    // and chopped the trailing digit of multi-digit numbers ("Vol 10" → "Vol 1"). Here
    // `\d+(?:\.\d+)?` greedily consumes the full vol number (including decimal vols like
    // "Vol 1.5"), then `[^\d.]` requires a real boundary before stripping the rest.
    [GeneratedRegex(@"((?:Vol|Box Set|Omnibus)\s+\d+(?:\.\d+)?)[^\d.].*", RegexOptions.IgnoreCase)] private static partial Regex StripAfterVolNumberRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunHtmlScrapeAsync(
            this, Website.AllStarComics, bookTitle, bookType, masterDataList, masterLinkList, errors, curRegion, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    internal string GenerateWebsiteUrl(string bookTitle, int curPage)
    {
        // https://allstarcomics.com.au/search?q=jujutsu+kaisen&type=product&page=2
        string url = $"{BASE_URL}/search?q={bookTitle.Replace(' ', '+')}&type=product&page={curPage}";
        _logger.PageUrlGenerated(curPage, url);
        return url;
    }

    /// <summary>
    /// Normalises the abbreviated Diamond-distributor catalog format into the canonical
    /// <c>"Title Vol N"</c> form the rest of the library uses. Examples:
    ///   "JUJUTSU KAISEN GN VOL 22 (C: 0-1-2)" → "Jujutsu Kaisen Vol 22"
    ///   "NARUTO 3-IN-1 EDITION TP VOL 02"     → "Naruto Omnibus 02"
    /// </summary>
    private static string CleanAndParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        // Title-case the SHOUTING entry text so it merges cleanly with other sites'
        // mixed-case output during cross-site dedup.
        string s = ToTitleCase(entryTitle);

        // Diamond box-set / omnibus shorthand: "3-IN-1 EDITION" → "Omnibus". `Replace` is a
        // no-op when the pattern doesn't match, so the explicit IsMatch guard was redundant.
        // The string.Replace for " Edition" is cheap on non-matching titles too.
        s = OmnibusRegex().Replace(s, "Omnibus");
        s = s.Replace(" Edition", string.Empty, StringComparison.OrdinalIgnoreCase);

        s = FixVolumeRegex().Replace(s, "Vol");
        s = CleanTitleRegex().Replace(s, string.Empty);
        s = StripAfterVolNumberRegex().Replace(s, "$1");
        s = StripLeadingZeroRegex().Replace(s, "$1 $2");

        StringBuilder curTitle = new(s);
        if (bookType == BookType.LightNovel && !s.Contains("Novel", StringComparison.OrdinalIgnoreCase))
        {
            int volIdx = curTitle.IndexOfOrdinal("Vol");
            if (volIdx >= 0) curTitle.Insert(volIdx, "Novel ");
            else curTitle.Append(" Novel");
        }
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    /// <summary>
    /// Lightweight title-case for SHOUTY listing text. Doesn't try to be perfect — leaves
    /// punctuation untouched and treats every space-delimited token independently.
    /// Writes directly into the string buffer via <see cref="string.Create"/> — no
    /// StringBuilder, no intermediate allocations.
    /// </summary>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return string.Create(input.Length, input, static (dst, src) =>
        {
            bool startOfWord = true;
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                if (char.IsWhiteSpace(c) || c == '-' || c == '(')
                {
                    dst[i] = c;
                    startOfWord = true;
                }
                else if (startOfWord)
                {
                    dst[i] = char.ToUpperInvariant(c);
                    startOfWord = false;
                }
                else
                {
                    dst[i] = char.ToLowerInvariant(c);
                }
            }
        });
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        // All Star Comics stocks Diamond-distributed manga / comics — they don't carry
        // prose light novels. Skip silently for multi-site LightNovel scrapes.
        if (bookType == BookType.LightNovel)
        {
            _logger.BookTypeNotSupported(TITLE, bookType);
            return (data, links);
        }

        HtmlWeb web = HtmlFactory.CreateWeb();
        List<HtmlDocument> listingPages = [];

        const int MaxPages = 30;
        int curPage = 1;
        while (curPage <= MaxPages)
        {
            string url = GenerateWebsiteUrl(bookTitle, curPage);
            links.Add(url);

            HtmlDocument doc = await web.LoadFromWebAsync(url).ConfigureAwait(false);
            doc.ConfigurePerf();
            listingPages.Add(doc);

            HtmlNodeCollection? cards = doc.DocumentNode.SelectNodes(_productCardXPath);
            if (cards is null || cards.Count == 0) break;
            // Pagination "next" anchor absent → we've consumed every page.
            if (doc.DocumentNode.SelectSingleNode(_nextPageXPath) is null) break;
            curPage++;
        }

        data = ParsePages(listingPages, bookTitle, bookType);
        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        return (data, links);
    }

    /// <summary>
    /// Parses pre-loaded All Star Comics listing docs into <see cref="EntryModel"/>s.
    /// Listing cards already carry price + add-to-cart button → no detail fetch.
    /// </summary>
    internal List<EntryModel> ParsePages(IReadOnlyList<HtmlDocument> listingPages, string bookTitle, BookType bookType)
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

        foreach (HtmlDocument doc in listingPages)
        {
            HtmlNodeCollection? cards = doc.DocumentNode.SelectNodes(_productCardXPath);
            if (cards is null) continue;

            foreach (HtmlNode card in cards)
            {
                HtmlNode? titleNode = card.SelectSingleNode(_titleRelXPath);
                if (titleNode is null) continue;

                string entryTitle = WebUtility.HtmlDecode(titleNode.InnerText.Trim());
                if (string.IsNullOrEmpty(entryTitle)) continue;

                // Catalog-level filtering. The SHOUTING titles use "GN VOL" / "TP VOL"
                // markers — anything without one is usually a non-comic item (figure,
                // accessory, supply) that the search dragged in by description. Novel
                // entries that sneak in (occasionally the LN/manga companion releases)
                // get dropped by the explicit "NOVEL" exclusion. SearchValues replaces
                // six sequential ignore-case Contains calls with one vectorized scan.
                ReadOnlySpan<char> entrySpan = entryTitle.AsSpan();
                bool looksLikeManga = entrySpan.IndexOfAny(_mangaMarkerSearch) >= 0;
                bool looksLikeNovel = entrySpan.Contains("NOVEL", StringComparison.OrdinalIgnoreCase);

                if (!looksLikeManga || looksLikeNovel)
                {
                    _logger.EntryRemovedDebug(1, entryTitle);
                    continue;
                }

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

                HtmlNode? priceNode = card.SelectSingleNode(_priceRelXPath);
                string price = priceNode?.InnerText.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(price))
                {
                    _logger.EntryRemovedDebug(2, entryTitle);
                    continue;
                }

                // Stock comes off the action button's class list — "Sold out" is on a
                // disabled button; in-stock items get the primary-action button.
                HtmlNode? actionBtn = card.SelectSingleNode(_addToCartBtnRelXPath);
                string btnClass = actionBtn?.GetAttributeValue<string>("class", string.Empty) ?? string.Empty;
                StockStatus stock = btnClass.Contains("button--disabled", StringComparison.Ordinal)
                    ? StockStatus.OOS
                    : actionBtn?.InnerText.Contains("Pre", StringComparison.OrdinalIgnoreCase) == true
                        ? StockStatus.PO
                        : StockStatus.IS;

                data.Add(new EntryModel(
                    CleanAndParseTitle(entryTitle, bookTitle, bookType),
                    price,
                    stock,
                    TITLE));
            }
        }

        data.SortByVolume();
        data.RemoveDuplicates(_logger);
        return data;
    }
}
