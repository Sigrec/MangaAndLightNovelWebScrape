namespace MangaAndLightNovelWebScrape.Websites;

public partial class MangaMart
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
    private readonly List<string> MangaMartLinks = [];
    private readonly List<EntryModel> MangaMartData = [];
    public const string WEBSITE_TITLE = "MangaMart";
    public const string WEBSITE_URL = "https://mangamart.com";
    public const Region REGION = Region.America;

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='product-item__title text--strong link']");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price' or @class='price price--highlight']/text()[2]");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//a[contains(@class, 'product-item__image-wrapper')]");
    private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//div[@class='pagination__nav']//a)[last()]");
    private static readonly XPathExpression EntryTitleDesc = XPathExpression.Compile("//div[@class='rte text--pull']");

    [GeneratedRegex(@"\b(?:Vols?|Volume)\b\.?", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"(?>,|\([^)]*\)|\s*Includes.*|(?<=Omnibus,\s*Vol\s*\d{1,3}\.).*|(?<=Vol\s*\d{1,3}(?!\d)).*|-The Manga|The Manga|Manga)", RegexOptions.IgnoreCase)] private static partial Regex ParseAndCleanTitleRegex();
    [GeneratedRegex(@"(?<=Box\s*Set\s*\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex BoxSetTitleCleanRegex();
    [GeneratedRegex(@"\((?:Omnibus|\d{1,3}-in-\d{1,3}) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusTitleCleanRegex();

    internal void ClearData()
    {
        MangaMartLinks.Clear();
        MangaMartData.Clear();
    }

    internal async Task CreateMangaMartTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> MasterDataList, WebDriver driver)
    {
        await Task.Run(() =>
        {
            MasterDataList.Add(GetMangaMartData(bookTitle, bookType, driver));
        });
    }

    internal string GetUrl()
    {
        return MangaMartLinks.Count != 0 ? MangaMartLinks[0] : $"{WEBSITE_TITLE} Has no Link";
    }

    private string GenerateWebsiteUrl(BookType bookType, string bookTitle, uint curPageNum)
    {
        // https://mangamart.com/search?type=product&q=jujutsu+kaisen&page=2
        // https://mangamart.com/search?type=product&q=overlord+novel
        string url = $"{WEBSITE_URL}/search?type=product&q={Uri.EscapeDataString(bookTitle)}{(bookType == BookType.Manga ? string.Empty : "+novel")}&page={curPageNum}";
        MangaMartLinks.Add(url);
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
        LOGGER.Debug("TITLE = {}", entryTitle);

        StringBuilder curTitle = new StringBuilder(ParseAndCleanTitleRegex().Replace(entryTitle, string.Empty));

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

    internal List<EntryModel> GetMangaMartData(string bookTitle, BookType bookType, WebDriver driver)
    {
        try
        {
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
            HtmlWeb web = new() { UsingCacheIfExists = true, UseCookies = true };
            HtmlDocument doc = new();

            // Load the document once after preparation.
            uint curPageNum = 1;
            driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookType, bookTitle, curPageNum));
            wait.Until(driver => driver.FindElement(By.CssSelector("div[class='product-list product-list--collection']")));
            doc.LoadHtml(driver.PageSource);

            bool bookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
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

                    bool shouldRemoveEntry = !InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) || (!bookTitleRemovalCheck && MasterScrape.EntryRemovalRegex().IsMatch(entryTitle));

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
                                HtmlNode descNode = web.Load($"https://mangamart.com/{urlPath}").DocumentNode.SelectSingleNode(EntryTitleDesc);
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
                        HtmlNode stockStatusNode = stockStatusData[x].SelectNodes(".//strong")?.FirstOrDefault();
                        StockStatus stockStatus = (stockStatusNode != null ? stockStatusNode.InnerText : string.Empty) switch
                        {
                            "PRE-ORDER" => StockStatus.PO,
                            "BACK-ORDER" => StockStatus.BO,
                            _ => StockStatus.IS,
                        };

                        MangaMartData.Add(
                            new EntryModel
                            (
                                ParseAndCleanTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                priceData[x].InnerText.Trim(),
                                stockStatus,
                                WEBSITE_TITLE
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
                    driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookType, bookTitle, curPageNum));
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
            LOGGER.Error("{} ({}) Error @ {} \n{}", bookTitle, bookType, WEBSITE_TITLE, ex);
        }
        finally
        {
            if (!MasterScrape.IsWebDriverPersistent)
            {
                driver?.Quit();
            }
            else
            {
                driver?.Close();
            }
            MangaMartData.Sort(EntryModel.VolumeSort);
            MangaMartData.RemoveDuplicates(LOGGER);
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, bookType, MangaMartData, LOGGER);
        }

        return MangaMartData;
    }
}