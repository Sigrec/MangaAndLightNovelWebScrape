using System.Buffers;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

/// <summary>
/// Kings Comics — Sydney-based Australian comic / manga retailer running on Shopify.
/// Listing cards carry title + price + availability inline, so no per-product detail
/// fetch is needed. Stocks Diamond-distributed manga in the same SHOUTING catalog
/// format used by All Star Comics, so the title-cleanup pipeline is shared in spirit
/// (leading-zero strip, trailing edition-marker strip, ToTitleCase). Manga / comic
/// shop only — light novels are skipped silently.
/// </summary>
public sealed partial class KingsComics : IWebsite
{
    private readonly ILogger _logger;

    public KingsComics(ILogger<KingsComics>? logger = null)
    {
        _logger = logger ?? NullLogger<KingsComics>.Instance;
    }

    /// <inheritdoc />
    public const string TITLE = "Kings Comics";
    /// <inheritdoc />
    public const string BASE_URL = "https://kingscomics.com";
    /// <inheritdoc />
    public const Region REGION = Region.Australia;

    // Listing card structure (captured from kingscomics.com on 2026-06-21):
    //   <div class="product-card">
    //     <a href="/products/..." class="product-card__link">
    //       <p class="product-card__title">NARUTO 3-IN-1 TP VOL 02</p>
    //       <p class="product-card__price">$24.99</p>
    //       <p class="product-card__availability product-card__availability--in">In stock</p>
    //     </a>
    //   </div>
    // Availability modifier classes seen: --in (In stock), --last (Last one — still
    // buyable), --out (Out of stock), --preorder (Pre-Order).
    private static readonly XPathExpression _productCardXPath = XPathExpression.Compile("//div[@class='product-card']");
    private static readonly XPathExpression _titleRelXPath = XPathExpression.Compile(".//p[contains(@class,'product-card__title')]");
    private static readonly XPathExpression _priceRelXPath = XPathExpression.Compile(".//p[contains(@class,'product-card__price')]");
    private static readonly XPathExpression _availabilityRelXPath = XPathExpression.Compile(".//p[contains(@class,'product-card__availability')]");

    // Vectorized multi-substring scan — same shape as AllStarComics. One pass per
    // entry instead of six sequential ignore-case Contains calls.
    private static readonly SearchValues<string> _mangaMarkerSearch = SearchValues.Create(
        ["GN VOL", "TP VOL", "HC VOL", "BOX SET", "OMNIBUS", "3-IN-1"],
        StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"GN\s+VOL|\bV(?=\d)|Volume|Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"\s*\(C:\s*[\d\-]+\)|,|\s+HC\b|\s+TP\b", RegexOptions.IgnoreCase)] private static partial Regex CleanTitleRegex();
    [GeneratedRegex(@"\d{1,3}-IN-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    // Diamond catalog zero-pads single-digit volume numbers ("Vol 01"). Strip the
    // leading zero so cross-site dedup collapses against other retailers' "Vol 1".
    [GeneratedRegex(@"\b(Vol|Box Set|Omnibus)\s+0+(\d)\b", RegexOptions.IgnoreCase)] private static partial Regex StripLeadingZeroRegex();
    // Strip edition markers after the volume number ("New Ptg", "(Mr)", subtitles).
    // Greedy `\d+(?:\.\d+)?` consumes the full vol number; `[^\d.]` forces a real
    // boundary so multi-digit volumes ("Vol 10") survive intact.
    [GeneratedRegex(@"((?:Vol|Box Set|Omnibus)\s+\d+(?:\.\d+)?)[^\d.].*", RegexOptions.IgnoreCase)] private static partial Regex StripAfterVolNumberRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunHtmlScrapeAsync(
            this, Website.KingsComics, bookTitle, bookType, masterDataList, masterLinkList, errors, curRegion, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);

    internal string GenerateWebsiteUrl(string bookTitle, int curPage)
    {
        // https://kingscomics.com/search?q=naruto&type=product&page=2
        string url = $"{BASE_URL}/search?q={bookTitle.Replace(' ', '+')}&type=product&page={curPage}";
        _logger.PageUrlGenerated(curPage, url);
        return url;
    }

    /// <summary>
    /// Normalises the SHOUTING Diamond-distributor catalog format into the canonical
    /// <c>"Title Vol N"</c> form. Mirrors the All Star Comics pipeline because Kings
    /// Comics carries the same catalog feed with the same conventions.
    /// </summary>
    private static string CleanAndParseTitle(string entryTitle, string bookTitle)
    {
        string s = ToTitleCase(entryTitle);

        // Diamond box-set / omnibus shorthand: "3-IN-1 EDITION" → "Omnibus". Replace
        // is a no-op when the pattern doesn't match, so no IsMatch guard needed.
        s = OmnibusRegex().Replace(s, "Omnibus");
        s = s.Replace(" Edition", string.Empty, StringComparison.OrdinalIgnoreCase);

        s = FixVolumeRegex().Replace(s, "Vol");
        s = CleanTitleRegex().Replace(s, string.Empty);
        s = StripAfterVolNumberRegex().Replace(s, "$1");
        s = StripLeadingZeroRegex().Replace(s, "$1 $2");

        StringBuilder curTitle = new(s);
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    /// <summary>
    /// Lightweight title-case for SHOUTY catalog text. Uses <see cref="string.Create"/>
    /// so no StringBuilder allocation per entry.
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

        // Kings Comics is a comic / manga shop — they don't stock prose light novels.
        // Skip silently for multi-site LightNovel scrapes.
        if (bookType == BookType.LightNovel)
        {
            _logger.BookTypeNotSupported(TITLE, bookType);
            return (data, links);
        }

        HtmlWeb web = HtmlFactory.CreateWeb();
        List<HtmlDocument> listingPages = [];

        // Page N+1 returns 200 with zero cards once results run out — that's the stop
        // signal. Hard cap protects against unexpected pagination loops.
        const int MaxPages = 30;
        int curPage = 1;
        while (curPage <= MaxPages)
        {
            string url = GenerateWebsiteUrl(bookTitle, curPage);
            links.Add(url);

            HtmlDocument doc = await web.LoadFromWebAsync(url);
            doc.ConfigurePerf();

            HtmlNodeCollection? cards = doc.DocumentNode.SelectNodes(_productCardXPath);
            if (cards is null || cards.Count == 0) break;

            listingPages.Add(doc);
            curPage++;
        }

        data = ParsePages(listingPages, bookTitle, bookType);
        InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        return (data, links);
    }

    /// <summary>
    /// Parses pre-loaded Kings Comics listing docs into <see cref="EntryModel"/>s.
    /// Listing cards carry everything — no detail fetch.
    /// </summary>
    internal List<EntryModel> ParsePages(IReadOnlyList<HtmlDocument> listingPages, string bookTitle, BookType bookType)
    {
        List<EntryModel> data = [];

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

                // Catalog-level filtering, same shape as AllStarComics. The search
                // pulls in posters, figures, apparel — anything without a manga
                // marker is dropped. Novel entries that sneak in get excluded too.
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

                // Stock comes off the availability paragraph's class list — the
                // theme appends a modifier (--in / --last / --out / --preorder).
                // "Last one" is still buyable, so it maps to IS not OOS.
                HtmlNode? availNode = card.SelectSingleNode(_availabilityRelXPath);
                string availClass = availNode?.GetAttributeValue<string>("class", string.Empty) ?? string.Empty;
                StockStatus stock =
                    availClass.Contains("product-card__availability--out", StringComparison.Ordinal) ? StockStatus.OOS
                    : availClass.Contains("product-card__availability--preorder", StringComparison.Ordinal) ? StockStatus.PO
                    : StockStatus.IS;

                data.Add(new EntryModel(
                    CleanAndParseTitle(entryTitle, bookTitle),
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
