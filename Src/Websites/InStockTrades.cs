using System.Net;

namespace MangaAndLightNovelWebScrape.Websites;

public partial class InStockTrades : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile(".//div[@class='title']/a/text()");
    private static readonly XPathExpression DetailsXPath = XPathExpression.Compile("//div[@class='detail clearfix']");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//div[@class='price']/text()");
    private static readonly XPathExpression PageCheckXPath= XPathExpression.Compile("//input[@id='currentpage']");

    [GeneratedRegex(@" GN| TP| HC| Manga|(?<=Vol).*|(?<=Box Set).*|\(.*\)")]  private static partial Regex TitleRegex();
    [GeneratedRegex(@" HC| TP|\(.*\)|(?<=Vol (?:\d{1,3}|\d{1,3}.\d{1,3}) ).*|(?<=Box Set (?:\d{1,3}|\d{1,3}.\d{1,3}) ).*")]  private static partial Regex CleanTitleRegex();
    [GeneratedRegex(@"(?:Vol|GN)\s+(\d{1,3}|\d{1,3}.\d{1,3})|Box Set\s+\s+(\d{1,3}|\d{1,3}.\d{1,3})|Box Set\s+Part\s+\s+(\d{1,3}|\d{1,3}.\d{1,3})")] private static partial Regex VolNumberRegex();
    [GeneratedRegex(@"3In1 (?:Ed TP|TP|Ed)|3In1|Omnibus TP")] private static partial Regex OmnibusRegex();
    
    /// <inheritdoc />
    public const string TITLE = "InStockTrades";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.instocktrades.com/";

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
            masterLinkList.TryAdd(Website.Crunchyroll, Links[0]);
        });
    }

    // https://www.instocktrades.com/search?term=world+trigger
    // https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
    // https://www.instocktrades.com/search?title=overlord+novel&publisher=&writer=&artist=&cover=&ps=true
    private static string GenerateWebsiteUrl(uint currPageNum, string bookTitle)
    {
        string url = $"{BASE_URL}/search?pg={currPageNum}&title={bookTitle.Replace(' ', '+')}&publisher=&writer=&artist=&cover=&ps=true";
        LOGGER.Info(url);
        return url;
    }

    // TODO Recheck Toilet Bound scrape
    private static string ParseAndCleanTitle(string bookTitle, string entryTitle, BookType bookType)
    {
        string volGroup;
        StringBuilder curTitle = new(entryTitle);

        if (entryTitle.Contains("Box Set")) 
        { 
            curTitle.Replace("Vol ", string.Empty);
            Match match = VolNumberRegex().Match(curTitle.ToString());
            volGroup = !string.IsNullOrWhiteSpace(match.Groups[3].Value) ? match.Groups[3].Value : match.Groups[2].Value;

            if (entryTitle.Contains("Season"))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Box Set"), $"Part {volGroup.TrimStart('0')} ");
            }
        }
        else
        {
            if (bookTitle.Equals("Overlord", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains(" Og "))
            {
                curTitle.Replace("Og", "Oh");
            }
            if (!entryTitle.Contains("Vol") && ((entryTitle.Contains("Special Ed") && !bookTitle.Contains("Special Ed")) || char.IsDigit(entryTitle[^1]) || entryTitle[^1] == ')'))
            {
                LOGGER.Debug("CHECK 1 {}", curTitle.ToString());
                Match volMatch = VolNumberRegex().Match(curTitle.ToString());
                if (string.IsNullOrWhiteSpace(volMatch.Groups[1].Value))
                {
                    curTitle.Insert(volMatch.Index, $"Vol {volMatch.Groups[1]}");
                }
                else if (string.IsNullOrWhiteSpace(volMatch.Groups[2].Value))
                {
                    curTitle.Insert(volMatch.Index, $"Vol {volMatch.Groups[2]}");
                }
            }

            if (bookType == BookType.LightNovel && !entryTitle.Contains("Novel"))
            {
                curTitle.Append(" Novel");
            }
        }

        if (bookType == BookType.Manga)
        {
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Vol GN", "Vol");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, " GN", string.Empty);
        }
        else
        {

            InternalHelpers.ReplaceMultipleTextInEntryTitle(ref curTitle, bookTitle, ["Light Novel", "Novel Sc", "L Novel"], "Novel");
        }

        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "One", "1");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Color HC Ed", "In Color");
        InternalHelpers.ReplaceMultipleTextInEntryTitle(ref curTitle, bookTitle, ["Ed Vol", "Ed HC Vol"], "Edition Vol");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Colossal Ed", "Colossal Edition");
        InternalHelpers.ReplaceMultipleTextInEntryTitle(ref curTitle, bookTitle, [" Annv Book", " Ann"], " Anniversary Edition");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Deluxe Edition", "Deluxe");

        if (!curTitle.ToString().Contains("Special Edition") && (entryTitle.Contains("Special Ed") || entryTitle.Contains("Sp Ed")))
        {
            curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Special Edition ");
        }

        entryTitle = curTitle.ToString();
        if (entryTitle.EndsWith(" Sc") || entryTitle.Contains(" Sc "))
        {
            curTitle.Remove(curTitle.ToString().LastIndexOf(" Sc"), 3);
        }

        entryTitle = WebUtility.HtmlDecode(OmnibusRegex().Replace(curTitle.ToString(), "Omnibus"));
        string finalTitle = TitleRegex().Replace(entryTitle, string.Empty).Trim();
        volGroup = VolNumberRegex().Match(curTitle.ToString()).Groups[1].Value;
        if (volGroup.StartsWith('0'))
        {
            return $"{finalTitle} {volGroup.TrimStart('0')}";
        }
        else if (entryTitle.Contains("Season") && !bookTitle.Contains("Season"))
        {
            return finalTitle;
        }
        else
        {
            return CleanTitleRegex().Replace(entryTitle, string.Empty).Trim();
        }
    }

    public static (List<EntryModel> Data, List<string> Links) GetData(string bookTitle, BookType bookType, WebDriver? driver = null, bool isMember = false)
    {
        uint maxPages = 0;
        uint curPageNum = 1;
        bool isOneShot = false;
        
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
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

            HtmlDocument doc;
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);

            string url = GenerateWebsiteUrl(curPageNum, bookTitle);
            links.Add(url);
            doc = web.Load(url);
            HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
            if (pageCheck != null)
            {
                maxPages = Convert.ToUInt32(pageCheck.GetAttributeValue("data-max", "Page Num Error"));
            }

            while (true)
            {
                // Get the page data from the HTML doc
                HtmlNodeCollection detailsData = doc.DocumentNode.SelectNodes(DetailsXPath);
                List<HtmlNode> titleData = [.. detailsData
                    .AsValueEnumerable()
                    .Select(node => node.SelectSingleNode(TitleXPath))
                    .Where(titleNode => titleNode != null)];

                isOneShot = titleData.Count == 1 && !titleData.AsValueEnumerable().Any(title => title.InnerText.Contains("Vol") || title.InnerText.Contains("Box Set") || title.InnerText.Contains("Manga"));

                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);

                string entryTitle;
                data.Capacity += titleData.Count;
                for (int x = 0; x < titleData.Count; x++)
                {
                    entryTitle = bookTitle.Contains("Adv")
                        ? titleData[x].InnerText
                        : titleData[x].InnerText.Replace(" Adv ", " Adventure ");

                    bool isBookTitleValid = InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) &&
                    !detailsData[x].InnerText.Contains("Damaged") &&
                    !InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck;

                    bool isMangaEntryValid = bookType == BookType.Manga &&
                        !entryTitle.ContainsAny([" Novel ", " Sc "]) &&
                        !entryTitle.EndsWith("Novel") &&
                        !entryTitle.EndsWith("Sc") &&
                        (isOneShot ||
                        entryTitle.ContainsAny([" GN", "Vol", "Box Set", "Manga", "Special Ed", " HC"])) &&
                        !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony") &&
                        !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto");

                    bool isNovelEntryValid = bookType == BookType.LightNovel &&
                            !entryTitle.ContainsAny(["Manga", " GN", " Ed TP"]) &&
                            entryTitle.ContainsAny(["Novel", "Sc"]) &&
                            !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "Joined Party");
                    // || !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "fullmetal alchemist", entryTitle, "Fullmetal Alchemist 20th Annv Book HC");

                    if (isBookTitleValid && (isMangaEntryValid || isNovelEntryValid))
                    {
                        string cleanedTitle = ParseAndCleanTitle(bookTitle, entryTitle, bookType);
                        string trimmedPrice = priceData[x].InnerText.Trim();
                        data.Add(
                            new EntryModel(cleanedTitle, trimmedPrice, StockStatus.IS, TITLE)
                        );
                    }
                    else
                    {
                        LOGGER.Info("Removed {} | {} | {} | {}", entryTitle, isBookTitleValid, isMangaEntryValid, isNovelEntryValid);
                    }
                }

                if (maxPages != 0 && curPageNum != maxPages)
                {
                    url = GenerateWebsiteUrl(++curPageNum, bookTitle);
                    links.Add(url);
                    doc = web.Load(url);
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            LOGGER.Error(ex, "{Title} ({BookType}) Error @ {Website}", bookTitle, bookType, TITLE);
        }
        finally
        {
            data.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        return (data, links);
    }
}