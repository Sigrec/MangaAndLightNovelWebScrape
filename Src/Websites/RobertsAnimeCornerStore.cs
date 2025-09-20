using System.Globalization;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class RobertsAnimeCornerStore : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

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

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.RobertsAnimeCornerStore, Links.Last());
        });
    }
    
    private static string GenerateWebsiteUrl(string bookTitle)
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
        LOGGER.Info($"Url = {url}");
        return url;
    }

    // TODO - Need to add special edition check (AoT)
    public static string CleanAndParseTitle(
            string   entryTitle,
            string   bookTitle,
            BookType bookType)
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
                // compute the omnibus volume
                int parsedVol = int.Parse(
                    omnibusMatch.Groups[1].Value,
                    CultureInfo.InvariantCulture);
                int newVol = (int)Math.Ceiling(parsedVol / 3m);

                // strip the omnibus edition text
                string filtered = OmnibusTitleFilterRegex()
                    .Replace(entryTitle, string.Empty)
                    .Trim();
                curTitle.Append(filtered.AsSpan());

                // normalize edition text
                curTitle.Replace(
                    "Colossal Omnibus Edition",
                    "Colossal Edition");
                curTitle.Replace(
                    "Omnibus Edition",
                    "Omnibus");

                // ensure “ Vol”
                if (curTitle.ToString()
                            .IndexOf(" Vol", StringComparison.Ordinal) < 0)
                {
                    curTitle.Append(" Vol");
                }

                // append new vol number
                curTitle.Append(' ');
                curTitle.Append(
                    newVol.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                // no match → empty
                curTitle.Append(string.Empty);
            }
        }
        else
        {
            // ——— 2) Default title filtering ———
            string filtered = TitleFilterRegex()
                .Replace(entryTitle, string.Empty)
                .Trim();
            curTitle.Append(filtered.AsSpan());

            // “Deluxe Edition” → “Deluxe Vol”
            if (curTitle.ToString()
                        .IndexOf(deluxeEdLit, StringComparison.Ordinal) >= 0)
            {
                curTitle.Replace(
                    "Deluxe Edition",
                    "Deluxe Vol");
            }

            // Box Set fallback: no digits & no “Collection”
            ReadOnlySpan<char> appended = filtered.AsSpan();
            if (appended.IndexOf(boxSetLit, StringComparison.OrdinalIgnoreCase) >= 0
                && appended.IndexOf(collectionLit, StringComparison.OrdinalIgnoreCase) < 0
                && appended.IndexOfAny(digitChars) < 0)
            {
                curTitle.Append(" 1");
            }
        }

        // ——— 3) Shared cleanups ———
        InternalHelpers.RemoveCharacterFromTitle(
            ref curTitle,
            bookTitle,
            ':');
        InternalHelpers.ReplaceTextInEntryTitle(
            ref curTitle,
            bookTitle,
            "-",
            " ");

        // ——— 4) Special Edition injection ———
        if (titleSpan.IndexOf(
                specialEdLit,
                StringComparison.OrdinalIgnoreCase) >= 0)
        {
            curTitle.Replace(
                " Special Edition",
                string.Empty);

            int volIdx = curTitle.ToString()
                                .IndexOf("Vol", StringComparison.Ordinal);
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

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            // Start scraping the URL where the data is found
            HtmlWeb html = HtmlFactory.CreateWeb();

            HtmlDocument doc = await html.LoadFromWebAsync(GenerateWebsiteUrl(bookTitle));
            doc.ConfigurePerf();

            int bookTitleSpaceCount = bookTitle.AsSpan().Count(" ");
            HtmlNodeCollection? seriesData = doc.DocumentNode.SelectNodes(SeriesTitleXPath);

            foreach (HtmlNode series in seriesData!)
            {
                string innerSeriesText = series.InnerText;
                string seriesText = MasterScrape.MultipleWhiteSpaceRegex()
                    .Replace(series.InnerText.Replace("Graphic Novels", string.Empty).Replace("Novels", string.Empty), " ")
                    .Trim();

                if ((seriesText.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) ||
                        InternalHelpers.Similar(bookTitle, seriesText,
                            ((string.IsNullOrWhiteSpace(seriesText) || bookTitle.Length > seriesText.Length)
                                ? bookTitle.Length / 6
                                : seriesText.Length / 6) + bookTitleSpaceCount) != -1) &&
                        ((bookType == BookType.Manga && innerSeriesText.Contains("Graphic Novels")) ||
                        bookType == BookType.LightNovel)
                    )
                {
                    links.Add($"https://www.animecornerstore.com/{series.GetAttributeValue("href", "Url Error")}");
                }
            }

            if (links.Count != 0)
            {
                bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
                foreach (string link in links)
                {
                    LOGGER.Info($"Url = {link}");
                    doc = html.Load(link);

                    List<HtmlNode> titleData = doc.DocumentNode
                        .SelectNodes(TitleXPath)?
                        .AsValueEnumerable()
                        .Where(title => !string.IsNullOrWhiteSpace(title.InnerText))
                        .ToList() ?? [];
                    HtmlNodeCollection? priceData = doc!.DocumentNode.SelectNodes(PriceXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();

                        bool isMangaWithGraphicNovel = bookType == BookType.Manga && entryTitle.Contains("Graphic Novel")
                            && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "berserk", entryTitle, "Berserk With Darkness Ink");

                        bool isLightNovel = bookType == BookType.LightNovel && !entryTitle.Contains("Graphic Novel");

                        // Combine the conditions for title and book type checks
                        bool isValidTitle =
                            // Book title matches entry title or passes the volume check for specific manga titles
                            (InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle) ||
                            (isMangaWithGraphicNovel && InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Spoof"))) &&

                            // Ensure entry is not to be removed (via regex or removal flag)
                            (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck) &&

                            // Check if it's a valid manga or light novel title
                            (isMangaWithGraphicNovel || isLightNovel);

                        // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                        if (isValidTitle)
                        {
                            string trimmedEntryTitle = entryTitle.Trim();  // Avoid calling Trim multiple times

                            StockStatus status = trimmedEntryTitle.Contains("Pre Order") ? StockStatus.PO :
                                trimmedEntryTitle.Contains("Backorder") ? StockStatus.BO :
                                StockStatus.IS;

                            data.Add(
                                new EntryModel(
                                    CleanAndParseTitle(trimmedEntryTitle, bookTitle, bookType),
                                    priceData![x].InnerText.Trim(),
                                    status,
                                    TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LOGGER.Error(ex, "{Title} ({BookType}) Error @ {TITLE}", bookTitle, bookType, TITLE);
        }
        finally
        {
            data.TrimExcess();
            links.TrimExcess();
            data.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        return (data, links);
    }
}