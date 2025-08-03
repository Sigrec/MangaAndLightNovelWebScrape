using System.Net;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class MangaMate : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='grid-product__title grid-product__title--body']");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//div[@class='grid-product__price']/text()[3]");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='grid-product__content']/div[1]");
    private static readonly XPathExpression StockStatusXPath2 = XPathExpression.Compile("//div[@class='grid-product__image-mask']/div[1]");
    private static readonly XPathExpression EntryLinkXPath = XPathExpression.Compile("//div[@class='grid__item-image-wrapper']/a");
    private static readonly XPathExpression EntryTypeXPath = XPathExpression.Compile("//div[@class='product-block'][4]/div/span/table//tr[4]/td[2]");

    [GeneratedRegex(@"The Manga|\(.*\)|Manga", RegexOptions.IgnoreCase)] private static partial Regex TitleParseRegex();
    [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

    /// <inheritdoc />
    public const string TITLE = "MangaMate";

    /// <inheritdoc />
    public const string BASE_URL = "https://mangamate.shop";

    /// <inheritdoc />
    public const Region REGION = Region.Australia;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, Browser browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(browser);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, driver);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.MangaMate, Links[0]);
        });
    }

    private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum)
    {
        // https://mangamate.shop/search?q=akane%20banashi&options%5Bprefix%5D=last
        string url = $"{BASE_URL}/search?options%5Bprefix%5D=last&page={pageNum}&q={InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"))}+{(bookType == BookType.Manga ? "manga" : "novel")}";
        LOGGER.Info("Page {} => {}", pageNum, url);
        return url;
    }

    private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (OmnibusRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
        }
        else
        {
            entryTitle = TitleParseRegex().Replace(entryTitle, string.Empty);
        }
        StringBuilder curTitle = new StringBuilder(entryTitle).Replace(",", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, ":", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Color Edition", "In Color");
        if (bookTitle.Equals("boruto", StringComparison.OrdinalIgnoreCase)) { curTitle.Replace(" Naruto Next Generations", string.Empty); }

        Match findVolNumMatch = MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim());
        if (bookType == BookType.Manga && !entryTitle.Contains("Box Set") && !entryTitle.Contains("Vol") && !string.IsNullOrWhiteSpace(findVolNumMatch.Groups[0].Value))
        {
            curTitle.Insert(findVolNumMatch.Index, "Vol ").TrimEnd();
        }
        else if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Stray Stories") && string.IsNullOrWhiteSpace(findVolNumMatch.Groups[0].Value))
        {
            curTitle.Insert(curTitle.Length, " Vol 1");
        }

        string volNum = findVolNumMatch.Groups[0].Value;
        if (volNum.Length > 1 && volNum.StartsWith('0'))
        {
            curTitle.Replace(volNum, volNum.TrimStart('0'));
        }
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    // TODO - Could use perf improvements
    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, WebDriver? driver = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
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
            HtmlDocument doc = new() { OptionCheckSyntax = false };
            WebDriverWait wait = new(driver!, TimeSpan.FromSeconds(30));

            ushort curPageNum = 1;
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            string url = GenerateWebsiteUrl(bookTitle, bookType, curPageNum);
            links.Add(url);

            driver!.Navigate().GoToUrl(url);
            wait.Until(driver => driver.FindElement(By.XPath("(//div[@class='grid grid--uniform'])[2]")));

            ushort maxPageNum = 1;
            maxPageNum = ushort.Parse(driver.FindElement(By.XPath("//span[@class='page'][last()]")).Text.Trim());

            // Open currency dropdown
            driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("//button[@aria-controls='CurrencyList-toolbar']"))));

            // Ensure it's open and clickable
            wait.Until(driver => driver.FindElement(
                By.XPath("//button[@aria-controls='CurrencyList-toolbar']"))
                .GetDomAttribute("aria-expanded")?.Equals("true") ?? false);

            // Click the AUD currency
            driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("//a[@data-value='AU'][1]"))));

            // Wait for page load
            wait.Until(driver => driver.FindElement(By.XPath("(//div[@class='grid grid--uniform'])[2]")));

            // Close the popup
            // driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("//button[@class='recommendation-modal__close-button']"))));
            LOGGER.Info("Clicked AUD Currency");

            doc.LoadHtml(driver.PageSource);
            while (true)
            {
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                HtmlNodeCollection stockstatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                HtmlNodeCollection stockstatusData2 = doc.DocumentNode.SelectNodes(StockStatusXPath2);
                HtmlNodeCollection entryLinkData = doc.DocumentNode.SelectNodes(EntryLinkXPath);

                for (int x = 0; x < titleData.Count; x++)
                {
                    string entryTitle = titleData[x].InnerText.Trim();
                    if (InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && !(
                            bookType == BookType.Manga
                            && (
                                entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                || (
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Story")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear")
                                )
                            )
                        )
                    )
                    {
                        string type = _html.Load($"https://mangamate.shop{entryLinkData[x].GetAttributeValue("href", "error")}").DocumentNode.SelectSingleNode(EntryTypeXPath).InnerText.Trim();
                        // LOGGER.Debug("{} | {}", entryTitle, type);

                        if ((bookType == BookType.Manga && (type.Equals("Manga") || type.Equals("Box Set"))) || (bookType == BookType.LightNovel && (type.Equals("Novel") || type.Equals("Box Set"))))
                        {
                            data.Add(
                                new EntryModel
                                (
                                    ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                    priceData[x].InnerText.Trim(),
                                    stockstatusData2[x].GetAttributeValue("class", "error").Contains("preorder") ? StockStatus.PO : stockstatusData[x].InnerText.Trim() switch
                                    {
                                        "Sold Out" => StockStatus.OOS,
                                        _ => StockStatus.IS
                                    },
                                    TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                        }
                    }
                    else
                    {
                        LOGGER.Info("Removed (1) {}", entryTitle);
                    }
                }

                if (curPageNum < maxPageNum)
                {
                    url = GenerateWebsiteUrl(bookTitle, bookType, ++curPageNum);
                    links.Add(url);
                    driver.Navigate().GoToUrl(url);
                    wait.Until(driver => driver.FindElement(By.XPath("(//div[@class='grid grid--uniform'])[2]")));
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
            driver?.Quit();
            data.TrimExcess();
            links.TrimExcess();
            data = InternalHelpers.RemoveDuplicateEntries(data);
            data.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        return (data, links);
    }
}