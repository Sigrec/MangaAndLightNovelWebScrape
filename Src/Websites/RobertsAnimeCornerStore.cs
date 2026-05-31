using System.Globalization;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class RobertsAnimeCornerStore : IWebsite
{
    private readonly ILogger _logger;

    public RobertsAnimeCornerStore(ILogger<RobertsAnimeCornerStore>? logger = null)
    {
        _logger = logger ?? NullLogger<RobertsAnimeCornerStore>.Instance;
    }

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//font[@face='dom bold, arial, helvetica']/b");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//form[@method='POST'][contains(text()[2], '$')]//font[@color='#ffcc33'][2]");
    private static readonly XPathExpression SeriesTitleXPath = XPathExpression.Compile("//b//a[1]");

    [GeneratedRegex(@"[#,]| Graphic Novel| :|\(.*?\)|\[Novel\]")] private static partial Regex TitleFilterRegex();
    [GeneratedRegex(@"[#,]| #\d+(?:-\d+)?|Graphic Novel|:.*?Omnibus|\(.*?\)|\[Novel\]")] private static partial Regex OmnibusTitleFilterRegex();
    [GeneratedRegex(@"-(\d+)")] private static partial Regex OmnibusVolNumberRegex();
    [GeneratedRegex(@"\d{1,3}")] private static partial Regex FindVolNumRegex();
    
    /// <inheritdoc />
    public const string TITLE = "RobertsAnimeCornerStore";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.animecornerstore.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, ConcurrentDictionary<Website, Exception> errors, IBrowser? browser, Region curRegion, Membership memberships = Membership.None, CancellationToken cancellationToken = default)
        => InternalHelpers.RunHtmlScrapeAsync(
            this, Website.RobertsAnimeCornerStore, bookTitle, bookType, masterDataList, masterLinkList, errors, curRegion, cancellationToken,
            useLastLink: true);

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => SiteHealth.IsReachableAsync(BASE_URL, cancellationToken);
    
    private string GenerateWebsiteUrl(string bookTitle)
    {
        if (string.IsNullOrWhiteSpace(bookTitle))
        {
            throw new ArgumentException("Book title cannot be null or empty.", nameof(bookTitle));
        }

        // Gets the starting page based on first letter and checks if we are looking for the 1st webpage (false) or 2nd webpage containing the actual item data (true)
        string key = char.ToLower(bookTitle[0]) switch
        {
            'a' or 'b' or (>= '0' and <= '9') => "mangalitenovab", // https://www.animecornerstore.com/mangalitenovab.html
            'c' or 'd' => "mangalitenovcd", // https://www.animecornerstore.com/mangalitenovcd.html
            'e' or 'f' => "mangalitenovef", // https://www.animecornerstore.com/mangalitenovef.html
            'g' or 'h' => "mangalitenovgh", // https://www.animecornerstore.com/mangalitenovgh.html
            'i' or 'j' or 'k' => "mangalitenovik", // https://www.animecornerstore.com/mangalitenovik.html
            'l' or 'm' => "mangalitenovlm", // https://www.animecornerstore.com/mangalitenovlm.html
            'n' or 'o' => "mangalitenovno", // https://www.animecornerstore.com/mangalitenovno.html
            'p' or 'q' => "mangalitenovpq", // https://www.animecornerstore.com/mangalitenovpq.html
            'r' or 's' => "mangalitenovrs", // https://www.animecornerstore.com/mangalitenovrs.html
            't' or 'u' => "mangalitenovtu", // https://www.animecornerstore.com/mangalitenovtu.html
            'v' or 'w' => "mangalitenovvw", // https://www.animecornerstore.com/mangalitenovvw.html
            'x' or 'y' or 'z'=> "mangalitenovxz", // https://www.animecornerstore.com/mangalitenovxz.html
            _ => throw new ArgumentOutOfRangeException(nameof(bookTitle), $"{bookTitle} Starts w/ Unknown Character")
        };

        string url = $"{BASE_URL}/{key}.html";
        _logger.UrlGenerated(url);
        return url;
    }

    internal static string CleanAndParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        // Spans for zero-alloc checks
        ReadOnlySpan<char> titleSpan = entryTitle.AsSpan();
        ReadOnlySpan<char> omnibusLit = "Omnibus".AsSpan();
        ReadOnlySpan<char> specialEdLit = "Special Edition".AsSpan();
        ReadOnlySpan<char> boxSetLit = "Box Set".AsSpan();
        ReadOnlySpan<char> collectionLit = "Collection".AsSpan();
        ReadOnlySpan<char> digitChars = "0123456789".AsSpan();
        ReadOnlySpan<char> deluxeEdLit = "Deluxe Edition".AsSpan();

        // Single builder, sized generously for typical extra text
        StringBuilder curTitle = new(entryTitle.Length + 64);

        // ——— 1) Omnibus path ———
        if (titleSpan.IndexOf(omnibusLit, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Match omnibusMatch = OmnibusVolNumberRegex().Match(entryTitle);
            if (omnibusMatch.Success)
            {
                int parsedVol = int.Parse(omnibusMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                int newVol = (int)Math.Ceiling(parsedVol / 3m);

                string filtered = OmnibusTitleFilterRegex().Replace(entryTitle, string.Empty).Trim();
                curTitle.Append(filtered.AsSpan());

                curTitle.Replace("Colossal Omnibus Edition", "Colossal Edition");
                curTitle.Replace("Omnibus Edition", "Omnibus");

                // ensure " Vol" suffix — use the SB extension to avoid materializing curTitle.
                if (curTitle.IndexOfOrdinal(" Vol") < 0)
                {
                    curTitle.Append(" Vol");
                }

                curTitle.Append(' ').Append(newVol.ToString(CultureInfo.InvariantCulture));
            }
            // No match → curTitle stays empty. (Old code did Append(string.Empty), a no-op.)
        }
        else
        {
            // ——— 2) Default title filtering ———
            string filtered = TitleFilterRegex().Replace(entryTitle, string.Empty).Trim();
            curTitle.Append(filtered.AsSpan());

            // "Deluxe Edition" → "Deluxe Vol" — check against `filtered` directly (it's the
            // current SB content verbatim, no need to materialize curTitle.ToString() again).
            if (filtered.AsSpan().IndexOf(deluxeEdLit, StringComparison.Ordinal) >= 0)
            {
                curTitle.Replace("Deluxe Edition", "Deluxe Vol");
            }

            // Box Set fallback: no digits & no "Collection"
            ReadOnlySpan<char> appended = filtered.AsSpan();
            if (appended.IndexOf(boxSetLit, StringComparison.OrdinalIgnoreCase) >= 0
                && appended.IndexOf(collectionLit, StringComparison.OrdinalIgnoreCase) < 0
                && appended.IndexOfAny(digitChars) < 0)
            {
                curTitle.Append(" 1");
            }
        }

        // ——— 3) Shared cleanups ———
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");

        // ——— 4) Special Edition injection ———
        if (titleSpan.IndexOf(specialEdLit, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            curTitle.Replace(" Special Edition", string.Empty);

            // SB-native IndexOf avoids a ToString() snapshot just to scan for "Vol".
            int volIdx = curTitle.IndexOfOrdinal("Vol");
            if (volIdx >= 0)
            {
                curTitle.Insert(volIdx, "Special Edition ");
            }
        }

        // ——— 5) LightNovel vs Manga tweaks ———
        if (bookType == BookType.LightNovel)
        {
            string temp = curTitle.ToString().Trim();
            ReadOnlySpan<char> tempSpan = temp.AsSpan();

            // If there's a vol number but no “Vol” keyword
            if (tempSpan.IndexOf("Vol".AsSpan(), StringComparison.Ordinal) < 0
                && FindVolNumRegex().IsMatch(temp)
                && !FindVolNumRegex().IsMatch(bookTitle))
            {
                Match volMatch = FindVolNumRegex().Match(temp);
                temp = temp.Replace(
                    " Novel",
                    string.Empty,
                    StringComparison.Ordinal);
                if (volMatch.Success)
                {
                    temp = temp.Insert(volMatch.Index, " Vol ");
                }
            }

            temp = temp.Trim();
            if (temp.Contains("Vol"))
            {
                temp = temp.Replace(
                    "Vol",
                    "Novel Vol",
                    StringComparison.Ordinal);
            }
            else if (temp.IndexOf("Novel", StringComparison.Ordinal) < 0)
            {
                temp = string.Concat(temp, " Novel");
            }

            curTitle.Clear();
            curTitle.Append(temp.AsSpan());
        }
        else if (bookType == BookType.Manga)
        {
            InternalHelpers.ReplaceTextInEntryTitle(
                ref curTitle,
                bookTitle,
                "The Manga",
                "Manga");
        }

        // ——— 6) Collapse multiple spaces in one final pass ———
        string result = MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ");
        return result;
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America, CancellationToken cancellationToken = default)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            // Start scraping the URL where the data is found
            HtmlWeb html = HtmlFactory.CreateWeb();

            HtmlDocument doc = await html.LoadFromWebAsync(GenerateWebsiteUrl(bookTitle));
            doc.ConfigurePerf();

            int bookTitleSpaceCount = bookTitle.AsSpan().Count(' ');
            HtmlNodeCollection? seriesData = doc.DocumentNode.SelectNodes(SeriesTitleXPath);
            if (seriesData is null)
            {
                return (data, links);
            }

            foreach (HtmlNode series in seriesData)
            {
                // Cache InnerText — the property builds a fresh string from all descendants.
                // Old code called it twice per iteration.
                string innerSeriesText = series.InnerText;

                // Cheap pre-filter for Manga searches: this landing page mixes manga ("Graphic
                // Novels") and light novels in one list. Skip the expensive regex/Similar walk
                // on entries that can't possibly match the requested book type.
                if (bookType == BookType.Manga && !innerSeriesText.Contains("Graphic Novels"))
                {
                    continue;
                }

                string seriesText = MasterScrape.MultipleWhiteSpaceRegex()
                    .Replace(innerSeriesText.Replace("Graphic Novels", string.Empty).Replace("Novels", string.Empty), " ")
                    .Trim();

                bool matchesByName = seriesText.Contains(bookTitle, StringComparison.OrdinalIgnoreCase)
                    || InternalHelpers.Similar(bookTitle, seriesText,
                        ((string.IsNullOrWhiteSpace(seriesText) || bookTitle.Length > seriesText.Length)
                            ? bookTitle.Length / 6
                            : seriesText.Length / 6) + bookTitleSpaceCount) != -1;

                if (matchesByName)
                {
                    links.Add($"https://www.animecornerstore.com/{series.GetAttributeValue("href", "Url Error")}");
                }
            }

            if (links.Count != 0)
            {
                bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
                foreach (string link in links)
                {
                    _logger.UrlGenerated(link);
                    // Async fetch — was synchronous html.Load(link), which blocked the thread.
                    doc = await html.LoadFromWebAsync(link);

                    List<HtmlNode> titleData = doc.DocumentNode
                        .SelectNodes(TitleXPath)?
                        .AsValueEnumerable()
                        .Where(title => !string.IsNullOrWhiteSpace(title.InnerText))
                        .ToList() ?? [];
                    HtmlNodeCollection? priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    if (priceData is null || titleData.Count == 0)
                    {
                        continue;
                    }

                    int pairCount = Math.Min(titleData.Count, priceData.Count);
                    for (int x = 0; x < pairCount; x++)
                    {
                        // titleData was built from already-trimmed InnerText below; no need to
                        // Trim again per iteration.
                        string entryTitle = titleData[x].InnerText.Trim();
                        bool containsGraphicNovel = entryTitle.Contains("Graphic Novel");

                        bool isMangaWithGraphicNovel = bookType == BookType.Manga && containsGraphicNovel
                            && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "berserk", entryTitle, "Berserk With Darkness Ink");

                        bool isLightNovel = bookType == BookType.LightNovel && !containsGraphicNovel;

                        // Combine the conditions for title and book type checks
                        bool isValidTitle =
                            (InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle) ||
                            (isMangaWithGraphicNovel && InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Spoof")))
                            && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                            && (isMangaWithGraphicNovel || isLightNovel);

                        if (isValidTitle)
                        {
                            StockStatus status = entryTitle.Contains("Pre Order") ? StockStatus.PO :
                                entryTitle.Contains("Backorder") ? StockStatus.BO :
                                StockStatus.IS;

                            data.Add(new EntryModel(
                                CleanAndParseTitle(entryTitle, bookTitle, bookType),
                                priceData[x].InnerText.Trim(),
                                status,
                                TITLE));
                        }
                        else
                        {
                            _logger.EntryRemovedSimple(entryTitle);
                        }
                    }
                }
            }
        }
        finally
        {
            data.TrimExcess();
            links.TrimExcess();
            data.SortByVolume();
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, _logger);
        }
        return (data, links);
    }
}