using System.Net;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class MangaMart : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='product-item__title text--strong link']");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price' or @class='price price--highlight']/text()[2]");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//a[contains(@class, 'product-item__image-wrapper')]");
    private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//div[@class='pagination__nav']//a)[last()]");
    private static readonly XPathExpression EntryTitleDesc = XPathExpression.Compile("//div[@class='rte text--pull']");

    [GeneratedRegex(@"\b(?:Vols?|Volume)\b\.?", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"(?>,|\([^)]*\)|\s*Includes.*|(?<=Omnibus,\s*Vol\s*\d{1,3}\.).*|(?<=Vol\s*\d{1,3}(?!\d)).*|-The Manga|The Manga|Manga)", RegexOptions.IgnoreCase)] private static partial Regex ParseAndCleanTitleRegex();
    [GeneratedRegex(@"(?<=Box\s*Set\s*\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex BoxSetTitleCleanRegex();
    [GeneratedRegex(@"\((?:Omnibus|\d{1,3}-in-\d{1,3}) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusTitleCleanRegex();

    /// <inheritdoc />
    public const string TITLE = "MangaMart";

    /// <inheritdoc />
    public const string BASE_URL = "https://mangamart.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, Browser browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(() =>
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(browser);
            (List<EntryModel> Data, List<string> Links) = GetData(bookTitle, bookType, driver);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.MangaMart, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(BookType bookType, string bookTitle, uint curPageNum)
    {
        // https://mangamart.com/search?type=product&q=jujutsu+kaisen&page=2
        // https://mangamart.com/search?type=product&q=overlord+novel
        string url = $"{BASE_URL}/search?type=product&q={Uri.EscapeDataString(bookTitle)}{(bookType == BookType.Manga ? string.Empty : "+novel")}&page={curPageNum}";
        LOGGER.Info("URL #{} -> {}", curPageNum, url);
        return url;
    }

    private static string ParseAndCleanTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            entryTitle = BoxSetTitleCleanRegex().Replace(entryTitle, string.Empty);
        }
        else if (OmnibusTitleCleanRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusTitleCleanRegex().Replace(entryTitle, "Omnibus");
        }

        StringBuilder curTitle = new(ParseAndCleanTitleRegex().Replace(entryTitle, string.Empty));

        if (bookType == BookType.LightNovel)
        {
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "(Light Novel)", string.Empty);
        }
        else
        {
            if (bookTitle.Contains("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(" Naruto Next Generations", string.Empty);
            }
        }

        if (bookTitle.Contains(':'))
        {
            InternalHelpers.RemoveAfterLastIfMultiple(ref curTitle, ':');
        }
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        
        curTitle.TrimEnd().AddVolToString();
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    public (List<EntryModel> Data, List<string> Links) GetData(string bookTitle, BookType bookType, WebDriver? driver = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            WebDriverWait wait = new(driver!, TimeSpan.FromSeconds(60));
            HtmlWeb _html = new()
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
            HtmlDocument doc = new();

            // Load the document once after preparation.
            uint curPageNum = 1;
            string url = GenerateWebsiteUrl(bookType, bookTitle, curPageNum);
            links.Add(url);
            driver!.Navigate().GoToUrl(url);
            wait.Until(driver => driver.FindElement(By.CssSelector("div[class='product-list product-list--collection']")));
            doc.LoadHtml(driver.PageSource);

            bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            HtmlNode pageNode = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
            uint maxPageNum = pageNode != null ? pageNode.GetAttributeValue<uint>("data-page", 0) : 0;
            LOGGER.Debug("Max Pages = {}", maxPageNum);

            while (true)
            {
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);

                for (int x = 0; x < titleData.Count; x++)
                {
                    string entryTitle = titleData[x].InnerText.Trim();

                    bool shouldRemoveEntry = !InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle) || (!bookTitleRemovalCheck && InternalHelpers.ShouldRemoveEntry(entryTitle));

                    if (bookType == BookType.Manga)
                    {
                        shouldRemoveEntry = shouldRemoveEntry ||
                            (!bookTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase)) ||
                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, ["Pirate Recipes"]) ||
                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, ["of Gluttony"]) ||
                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, ["Boruto"]) ||
                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, ["Unimplemented"]);

                        if (!shouldRemoveEntry && !entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase))
                        {
                            LOGGER.Debug("Checking {} for Novel", entryTitle);
                            string urlPath = titleData[x].GetAttributeValue<string>("href", string.Empty);
                            if (!string.IsNullOrWhiteSpace(urlPath))
                            {
                                HtmlNode descNode = _html.Load($"https://mangamart.com/{urlPath}").DocumentNode.SelectSingleNode(EntryTitleDesc);
                                string innerText = descNode.InnerText;
                                if (descNode != null && (innerText.Contains("Light Novel", StringComparison.OrdinalIgnoreCase) || innerText.Contains("novels", StringComparison.OrdinalIgnoreCase)))
                                {
                                    LOGGER.Debug("Found Novel entry in Manga Scrape");
                                    shouldRemoveEntry = true;
                                }
                            }
                        }
                    }
                    else if (bookType == BookType.LightNovel)
                    {
                        shouldRemoveEntry = shouldRemoveEntry || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented");
                    }

                    if (!shouldRemoveEntry)
                    {
                        HtmlNode? stockStatusNode = stockStatusData![x].SelectNodes(".//strong")?.FirstOrDefault();
                        StockStatus stockStatus = (stockStatusNode != null ? stockStatusNode.InnerText : string.Empty) switch
                        {
                            "PRE-ORDER" => StockStatus.PO,
                            "BACK-ORDER" => StockStatus.BO,
                            _ => StockStatus.IS,
                        };

                        data.Add(
                            new EntryModel
                            (
                                ParseAndCleanTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                priceData[x].InnerText.Trim(),
                                stockStatus,
                                TITLE
                            )
                        );
                    }
                    else
                    {
                        LOGGER.Debug("Removed {}", entryTitle);
                    }
                }

                if (curPageNum < maxPageNum)
                {
                    curPageNum++;
                    url = GenerateWebsiteUrl(bookType, bookTitle, curPageNum);
                    links.Add(url);
                    driver.Navigate().GoToUrl(url);
                    wait.Until(driver => driver.FindElement(By.CssSelector("div[class='product-list product-list--collection']")));
                    doc.LoadHtml(driver.PageSource);
                }
                else
                {
                    break;
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
            data.RemoveDuplicates(LOGGER);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }

        return (data, links);
    }
}