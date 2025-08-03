using System.Collections.Frozen;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class KinokuniyaUSA : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
    
    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//span[@class='underline']");
    private static readonly XPathExpression MemberPriceXPath = XPathExpression.Compile("//li[@class='price'][2]/span");
    private static readonly XPathExpression NonMemberPriceXPath = XPathExpression.Compile("//li[@class='price'][1]/span");
    private static readonly XPathExpression DescXPath = XPathExpression.Compile("//p[@class='description']");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//li[@class='status']");
    private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//div[@class='categoryPager']/ul/li[last()]/a");
    
    [GeneratedRegex(@"\((?:Omnibus |3\s*In\s*1 |2\s*In\s*1 )Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"\(Light Novel\)|Light Novel|Novel", RegexOptions.IgnoreCase)] private static partial Regex NovelRegex();
    [GeneratedRegex(@"\((.*?)\)+", RegexOptions.IgnoreCase)] private static partial Regex TitleCaptureRegex();
    [GeneratedRegex(@"^[^\(]+", RegexOptions.IgnoreCase)] private static partial Regex CleanInFrontTitleRegex();
    [GeneratedRegex(@"(?<=\d{1,3})[^\d{1,3}].*", RegexOptions.IgnoreCase)] private static partial Regex CleanBehindTitleRegex();
    [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\({1,}.*?\){1,}|<.*?>|w/DVD|<|>|(?<=\d{1,3})\s+:.*", RegexOptions.IgnoreCase)] private static partial Regex MangaTitleFixRegex();
    [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\({1,}.*?\){1,}|(?<=\d{1,3})\s?:.*|<.*?[^\d+]>|w/DVD|<|>", RegexOptions.IgnoreCase)] private static partial Regex NovelTitleFixRegex();
    [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"\d{1,3}\.\d{1,3}|\d{1,3}")] internal static partial Regex FindVolNumRegex();

    /// <inheritdoc />
    public const string TITLE = "Kinokuniya USA";

    /// <inheritdoc />
    public const string BASE_URL = "https://united-states.kinokuniya.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    private static readonly int STATUS_START_INDEX = "Availability Status : ".Length;
    private static readonly FrozenSet<string> SkipBookTitles = ["Attack on Titan"];

    // Manga English Search
    //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=world+trigger&taxon=2&x=39&y=4&page=1&per_page=100&form_taxon=109
    // https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=Skeleton+Knight+in+Another+World&taxon=2&x=39&y=11&page=1&per_page=100

    // Light Novel English Search
    //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=overlord+novel&taxon=&x=33&y=8&per_page=100&form_taxon=109
    //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=classroom+of+the+elite&taxon=&x=33&y=8&per_page=100&form_taxon=109

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, Browser browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships)
    {
        return Task.Run(async () =>
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(browser);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, driver, memberships.IsKinokuniyaUSAMember);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.KinokuniyaUSA, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(string bookTitle, BookType bookType)
    {
        string url = $"{BASE_URL}/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords={bookTitle.Replace(" ", "+")}{(bookType == BookType.LightNovel ? "+novel" : string.Empty)}&taxon=2&x=39&y=11&page=1&per_page=100";
        LOGGER.Info($"Url = {url}");
        return url;
    }

    private static void WaitForPageLoad(WebDriverWait wait)
    {
        wait.Until(d =>
        {
            try
            {
                IWebElement element = d.FindElement(By.Id("loading"));
                string? style = element.GetDomAttribute("style");
                return style != null && style.Contains("display: none;");
            }
            catch (NoSuchElementException)
            {
                LOGGER.Warn("Loading Failed");
                return true;
            }
        });
    }

    private static string ParseAndCleanTitle(string entryTitle, BookType bookType, string bookTitle, string entryDesc, bool oneShotCheck)
    {
        if (!bookTitle.Contains('-'))
        {
            entryTitle = entryTitle.Replace("-", " ");
        }
        
        string parseCheckTitle = TitleCaptureRegex().Match(entryTitle).Groups[1].Value;
        string checkBeforeText = CleanInFrontTitleRegex().Match(entryTitle).Value;
        if (!SkipBookTitles.Contains(bookTitle, StringComparer.OrdinalIgnoreCase) && parseCheckTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) && !checkBeforeText.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) && entryTitle.Any(char.IsDigit) && !bookTitle.Any(char.IsDigit))
        {
            // LOGGER.Debug("{} | {} | {} | {} | {}", checkBeforeText, parseCheckTitle, entryTitle, CleanInFrontTitleRegex().Replace(entryTitle, string.Empty), CleanInFrontTitleRegex().Replace(entryTitle, string.Empty).Insert(0, $"{parseCheckTitle} "));
            entryTitle = CleanInFrontTitleRegex().Replace(entryTitle, string.Empty).Insert(0, $"{parseCheckTitle} ");
        }

        if (!oneShotCheck)
        {
            string newEntryTitle;
            if (bookType == BookType.LightNovel)
            {
                entryTitle = NovelRegex().Replace(entryTitle, "Novel");
                newEntryTitle = NovelTitleFixRegex().Replace(OmnibusRegex().Replace(FixVolumeRegex().Replace(entryTitle, "Vol"), "Omnibus"), "$1");
            }
            else
            {
                newEntryTitle = MangaTitleFixRegex().Replace(OmnibusRegex().Replace(FixVolumeRegex().Replace(entryTitle, "Vol"), "Omnibus"), "$1");
            }

            StringBuilder curTitle = new StringBuilder(newEntryTitle).Replace(",", string.Empty);
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            newEntryTitle = curTitle.ToString().Trim();

            if (bookType == BookType.LightNovel)
            {
                if (!newEntryTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Insert(0, $"{char.ToUpper(bookTitle[0])}{bookTitle.AsSpan(1)} ");
                }

                newEntryTitle = curTitle.ToString();
                bool containsNovel = newEntryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase);
                bool containsVol = newEntryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase);
                if (!containsNovel && !containsVol)
                {
                    curTitle = new StringBuilder(CleanBehindTitleRegex().Replace(newEntryTitle, string.Empty));
                }
                else if (!containsNovel && containsVol)
                {
                    curTitle.Insert(newEntryTitle.IndexOf("Vol"), "Novel ");
                }

                newEntryTitle = curTitle.ToString();
                if (!containsNovel && !newEntryTitle.Any(char.IsDigit) && !bookTitle.Any(char.IsDigit))
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }
            else if (bookType == BookType.Manga && !newEntryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !newEntryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                if (MasterScrape.FindVolNumRegex().IsMatch(newEntryTitle) && !bookTitle.AsParallel().Any(char.IsDigit))
                {
                    curTitle.Insert(MasterScrape.FindVolNumRegex().Match(newEntryTitle).Index, "Vol ");
                }
                else if (entryDesc.Contains("Collection", StringComparison.OrdinalIgnoreCase) && entryDesc.Contains("volumes", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Insert(curTitle.Length, " Box Set");
                }
            }

            if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace("Naruto Next Generations", string.Empty);
            }

            if (entryTitle.AsParallel().Any(char.IsDigit) && !curTitle.ToString().Contains("Vol") && !entryTitle.Contains("Box Set")  && !entryTitle.Contains("Anniversary"))
            {
                Match volNum = FindVolNumRegex().Match(curTitle.ToString());
                if (!string.IsNullOrWhiteSpace(volNum.Value))
                {
                    curTitle.Remove(volNum.Index, volNum.Value.Length);
                    curTitle.AppendFormat("{0} Vol {1}", bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel") ? " Novel" : string.Empty, volNum.Value);
                }
            }

            if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Omnibus")  && !curTitle.ToString().Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Stray God ");
            }
            
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Manga", string.Empty).ToString().Trim(), " ");
        }

        if (bookType == BookType.Manga)
        {
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(MangaTitleFixRegex().Replace(FixVolumeRegex().Replace(entryTitle.Replace("Manga", string.Empty).Replace(",", string.Empty), "Vol"), "$1").Trim(), " ");
        }
        else
        {
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(NovelTitleFixRegex().Replace(FixVolumeRegex().Replace(entryTitle.Replace("Manga", string.Empty).Replace(",", string.Empty), "Vol"), "$1").Trim(), " ");
        }
    }
    
    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, WebDriver? driver = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            int maxPageCount = -1, curPageNum = 1;
            bool oneShotCheck = false;
            string entryTitle, entryDesc;
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            HtmlDocument doc = new();

            WebDriverWait wait = new(driver!, TimeSpan.FromSeconds(60));
            string url = GenerateWebsiteUrl(bookTitle, bookType);
            links.Add(url);
            driver!.Navigate().GoToUrl(url);
            WaitForPageLoad(wait);

            // Click the list display mode so it shows stock status data with entry
            driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.LinkText("List"))));
            WaitForPageLoad(wait);
            LOGGER.Info("Clicked List Mode");

            if (bookType == BookType.Manga)
            {
                // Click the Manga
                driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.LinkText("Manga"))));
                WaitForPageLoad(wait);
                LOGGER.Info("Clicked Manga");
            }

            while (true)
            {
                doc.LoadHtml(driver.PageSource);

                // Get the page data from the HTML doc
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                oneShotCheck = curPageNum == 1 && titleData.Count == 1 && !titleData.AsParallel().Any(title => title.InnerText.Contains("Vol", StringComparison.OrdinalIgnoreCase)); // Determine if the series is a one shot or not
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(isMember ? MemberPriceXPath : NonMemberPriceXPath);
                HtmlNodeCollection descData = doc.DocumentNode.SelectNodes(DescXPath);
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                if (maxPageCount == -1) { maxPageCount = Convert.ToInt32(doc.DocumentNode.SelectSingleNode(PageCheckXPath).InnerText); }

                // LOGGER.Debug("{} | {} | {} | {}", titleData.Count, priceData.Count, descData.Count, stockStatusData.Count);

                // Remove all of the novels from the list if user is searching for manga
                for (int x = 0; x < titleData.Count; x++)
                {
                    entryTitle = System.Net.WebUtility.HtmlDecode(titleData[x].InnerText);
                    entryDesc = descData[x].InnerText;

                    if (
                        InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && (
                                (
                                    bookType == BookType.Manga
                                    && (
                                        InternalHelpers.RemoveEntryTitleCheck(bookTitle, entryTitle, "Novel")
                                        || entryTitle.Contains("graphic novel", StringComparison.OrdinalIgnoreCase)
                                        )
                                    && !entryTitle.Contains("Chapter Book", StringComparison.OrdinalIgnoreCase)
                                    && (
                                        oneShotCheck ||
                                        FixVolumeRegex().IsMatch(entryTitle) ||
                                        entryDesc.ContainsAny(["Collection", "volumes", "color edition"]) ||
                                        (entryTitle.Any(char.IsDigit) && !bookTitle.Any(char.IsDigit)))
                                    && !(
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony") ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear Your Own World") ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, ["Itachi's Story", "Boruto"]) ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Attack on Titan", entryTitle, ["Kuklo", "end of the world"]) ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented")
                                    )
                                )
                                ||
                                (
                                    bookType == BookType.LightNovel
                                    && !entryTitle.Contains("graphic novel", StringComparison.OrdinalIgnoreCase)
                                    && entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                )
                            )
                        )
                    {
                        entryTitle = ParseAndCleanTitle(entryTitle, bookType, bookTitle, entryDesc, oneShotCheck);
                        if (!data.Any(entry => entry.Entry.Equals(entryTitle, StringComparison.OrdinalIgnoreCase)))
                        {
                            data.Add(
                                new EntryModel(
                                    entryTitle,
                                    priceData[x].InnerText.Trim(),
                                    stockStatusData[x].InnerText.Trim().AsSpan(STATUS_START_INDEX) switch
                                    {
                                        "In stock at the Fulfilment Center." => StockStatus.IS,
                                        "Available for Pre Order" => StockStatus.PO,
                                        "Out of stock." => StockStatus.OOS,
                                        "Available for order from suppliers." => StockStatus.BO,
                                        _ => StockStatus.NA
                                    },
                                    TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Info("Removed (2) {}", entryTitle);
                        }
                    }
                    else
                    {
                        LOGGER.Info("Removed (1) {}", entryTitle);
                    }
                }
                if (curPageNum != maxPageCount)
                {
                    curPageNum++;
                    LOGGER.Debug("Going to Page {}", curPageNum);
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.ClassName("pagerArrowR"))));
                    WaitForPageLoad(wait);
                    LOGGER.Info("Page {} = {}", curPageNum, driver.Url);
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
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }

        return (data, links);
    }
}