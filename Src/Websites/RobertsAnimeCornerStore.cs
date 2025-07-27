using System.Net;

namespace MangaAndLightNovelWebScrape.Websites;

public partial class RobertsAnimeCornerStore : IWebsite
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

    /// <inheritdoc />
    public const bool NEEDS_WEB_DRIVER = false;

    /// <inheritdoc />
    public const bool NEEDS_USER_AGENT = false;

    /// <inheritdoc />
    public const bool HAS_MEMBERSHIP = false;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, Browser? browser = null)
    {
        return Task.Run(() =>
        {
            (List<EntryModel> Data, List<string> Links) = GetData(bookTitle, bookType);
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
        LOGGER.Info($"Initial Url = {url}");
        return url;
    }

    private static string CleanAndParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        StringBuilder curTitle;
        bool specialEditionCheck = entryTitle.Contains("Special Edition", StringComparison.OrdinalIgnoreCase);

        if (entryTitle.Contains("Omnibus"))
        {
            Match match = OmnibusVolNumberRegex().Match(entryTitle);
            if (match.Success)
            {
                uint volNum = Convert.ToUInt32(match.Groups[1].Value);
                curTitle = new StringBuilder(OmnibusTitleFilterRegex().Replace(entryTitle, string.Empty).Trim());
                curTitle.Replace("Colossal Omnibus Edition", "Colossal Edition");
                curTitle.Replace("Omnibus Edition", "Omnibus");

                if (!curTitle.ToString().Contains(" Vol"))
                {
                    curTitle.Append(" Vol");
                }
                curTitle.AppendFormat(" {0}", Math.Ceiling((decimal)volNum / 3));
            }
            else
            {
                curTitle = new StringBuilder("");
            }
        }
        else
        {
            curTitle = new StringBuilder(TitleFilterRegex().Replace(entryTitle, string.Empty).Trim());
            curTitle.Replace("Deluxe Edition", "Deluxe Vol");
            if (entryTitle.Contains("Box Set") && !entryTitle.Contains("Collection") && !entryTitle.Any(char.IsDigit))
            {
                curTitle.Append(" 1");
            }
        }

        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");

        if (specialEditionCheck)
        {
            curTitle.Replace(" Special Edition", string.Empty);
            curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Special Edition ");
        }

        if (bookType == BookType.LightNovel)
        {
            entryTitle = curTitle.ToString().Trim();
            if (!entryTitle.Contains("Vol") && FindVolNumRegex().IsMatch(entryTitle) && !FindVolNumRegex().IsMatch(bookTitle))
            {
                curTitle.Replace(" Novel", string.Empty);
                curTitle.Insert(FindVolNumRegex().Match(entryTitle).Index, " Vol ");
            }

            entryTitle = curTitle.ToString().Trim();
            if (entryTitle.Contains("Vol"))
            {
                curTitle.Replace("Vol", "Novel Vol");
            }
            else if (!entryTitle.Contains("Novel"))
            {
                curTitle.Insert(curTitle.Length, " Novel");
            }
        }
        else if (bookType == BookType.Manga)
        {
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "The Manga", "Manga");
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ");
    }
    
    // TODO - Need to add special edition check (AoT)
    public static (List<EntryModel> Data, List<string> Links) GetData(string bookTitle, BookType bookType, WebDriver? driver = null, bool isMember = false)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            // Start scraping the URL where the data is found
            HtmlWeb web = new()
            {
                UsingCacheIfExists = true,
                AutoDetectEncoding = false,
                OverrideEncoding = Encoding.UTF8,
                UseCookies = false,
                PreRequest = request =>
                {
                    HttpWebRequest http = request;
                    http.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    http.KeepAlive = true;
                    http.Timeout = 10_000;
                    return true;
                }
            };

            HtmlDocument doc = web.Load(GenerateWebsiteUrl(bookTitle));
            doc.OptionCheckSyntax = false;
            doc.OptionFixNestedTags = false;
            doc.OptionAutoCloseOnEnd = true;

            int bookTitleSpaceCount = bookTitle.AsSpan().Count(" ");
            HtmlNodeCollection? seriesData = doc.DocumentNode.SelectNodes(SeriesTitleXPath);

            foreach (HtmlNode series in seriesData!)
            {
                string innerSeriesText = series.InnerText;
                string seriesText = MasterScrape.MultipleWhiteSpaceRegex()
                    .Replace(series.InnerText.Replace("Graphic Novels", string.Empty).Replace("Novels", string.Empty), " ")
                    .Trim();

                if ((seriesText.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) ||
                        EntryModel.Similar(bookTitle, seriesText,
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
                    doc = web.Load(link);

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
                            (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) ||
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
            data.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }           
        return (data, links);
    }
}