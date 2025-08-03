using System.Collections.Frozen;
using System.Net;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class ForbiddenPlanet : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
    
    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='full']/ul/li/section/header/div[2]/h3/a");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='clr-price']");
    private static readonly XPathExpression MinorPriceXPath = XPathExpression.Compile("(//div[@class='full']/ul/li/section/header/div[2])/p/span[2]");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='full']/ul/li/section/header/div/ul");
    private static readonly XPathExpression BookFormatXPath = XPathExpression.Compile("(//div[@class='full']/ul/li/section/header/div[1])/p[1]");
    private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//a[@class='product-list__pagination__next'])[1]");

    [GeneratedRegex(@"The Manga|\(Hardcover\)|:(?:.*):|\(.*\)", RegexOptions.IgnoreCase)] internal static partial Regex CleanAndParseTitleRegex();
    [GeneratedRegex(@"\(Hardcover\)|The Manga", RegexOptions.IgnoreCase)] internal static partial Regex ColorCleanAndParseTitleRegex();
    [GeneratedRegex(@":.*(?:(?:3|2)-In-1|(?:3|2) In 1) Edition\s{0,3}:|:.*(?:(?:3|2)-In-1|(?:3|2) In 1)\s{0,3}:|(?:(?:3|2)-In-1|(?:3|2) In 1)\s{0,3} Edition:|\(Omnibus Edition\)|Omnibus\s{0,}(\d{1,3}|\d{1,3}.\d{1})(?:\s{0,}|:\s{0,})(?:\(.*\)|Vol \d{1,3}-\d{1,3})|:.*Omnibus:\s+(\d{1,3}).*|:.*:\s+Vol\s+(\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
    [GeneratedRegex(@"Box Set:|:\s+Box Set|Box Set (\d{1,3}):|:\s+(\d{1,3}) \(Box Set\)|\(Box Set\)|Box Set Part", RegexOptions.IgnoreCase)] private static partial Regex BoxSetFixRegex();
    [GeneratedRegex(@"(\d{1,3})-\d{1,3}")] private static partial Regex BoxSetVolFindRegex();
    [GeneratedRegex(@"(?<=(?:Vol|Box Set)\s+(?:\d{1,3}|\d{1,3}.\d{1}))[^\d{1,3}.]+.*")] private static partial Regex RemoveAfterVolNumRegex();
    [GeneratedRegex(@"\((.*?Anniversary.*?)\)")] private static partial Regex AnniversaryMatchRegex();
    [GeneratedRegex(@"Volumes|Volume|Vol\.|Volumr", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

    /// <inheritdoc />
    public const string TITLE = "Forbidden Planet";

    /// <inheritdoc />
    public const string BASE_URL = "https://forbiddenplanet.com";

    /// <inheritdoc />
    public const Region REGION = Region.Britain;
    
    private static readonly FrozenSet<string> DescRemovalStrings = ["novel", "original stories", "collecting issues"];

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, Browser browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(browser, true);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, driver);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.ForbiddenPlanet, Links[0]);
        });
    }
    private string GenerateWebsiteUrl(BookType bookType, string entryTitle, bool isSecondCategory)
    {
        // https://forbiddenplanet.com/catalog/?q=Naruto&show_out_of_stock=on&sort=release-date-asc&page=1
        string url = $"{BASE_URL}/catalog/{(!isSecondCategory ? "manga" : "comics-and-graphic-novels")}/?q={(bookType == BookType.Manga ? InternalHelpers.FilterBookTitle(entryTitle) : $"{InternalHelpers.FilterBookTitle(entryTitle)}%20light%20novel")}&show_out_of_stock=on&sort=release-date-asc&page=1";
        LOGGER.Info($"Url = {url}");
        return url;
    }

    private static string CleanAndParseTitle(string bookTitle, string entryTitle, BookType bookType)
    {
        entryTitle = FixVolumeRegex().Replace(entryTitle.Trim(), " Vol");
        StringBuilder curTitle;
        if (entryTitle.EndsWith("(Colour Edition Hardcover)"))
        {
            entryTitle = entryTitle.Replace("(Colour Edition Hardcover)", string.Empty).Trim();
            entryTitle = entryTitle.Insert(entryTitle.IndexOf("Vol"), "In Color ");
        }

        if (!entryTitle.Contains("Anniversary") && entryTitle.ContainsAny(["3-In-1", "3 In 1", "Omnibus"]))
        {
            // LOGGER.Debug("(0) {}", entryTitle);
            entryTitle = OmnibusFixRegex().Replace(entryTitle, $" Omnibus $1$2$3");
            curTitle = new StringBuilder(entryTitle);

            if (!entryTitle.Contains("Omnibus"))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
            }

            if (!entryTitle.Contains("Vol"))
            {
                Match volMatch = MasterScrape.FindVolNumRegex().Match(entryTitle);
                if (volMatch.Success)
                {
                    curTitle.Insert(volMatch.Index, "Vol ");
                };
            }
            curTitle.TrimEnd();
            // LOGGER.Debug("(1) {}", curTitle.ToString());

            if (!char.IsDigit(curTitle.ToString()[^1]))
            {
                curTitle.Replace("Omnibus", string.Empty);
                curTitle.TrimEnd();
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
            }
            // LOGGER.Debug("(2) {}", curTitle.ToString());
        }
        else if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            // entryTitle = entryTitle.Replace("Part", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            if (BoxSetVolFindRegex().IsMatch(entryTitle))
            {
                curTitle = new StringBuilder(BoxSetVolFindRegex().Replace(BoxSetFixRegex().Replace(entryTitle, " Box Set $1"), string.Empty));
                if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || entryTitle.IndexOf("Vol") < entryTitle.IndexOf("Box Set"))
                {
                    curTitle.Append($" {BoxSetVolFindRegex().Match(entryTitle).Groups[1].Value}");
                }
            }
            else
            {
                curTitle = new StringBuilder(BoxSetFixRegex().Replace(entryTitle, " Box Set $2"));
                if (!char.IsDigit(entryTitle[^1]))
                {
                    curTitle.Replace("Box Set", string.Empty);
                    curTitle.TrimEnd();
                    if (!char.IsDigit(curTitle.ToString()[^1])) curTitle.Append(" 1");
                    
                    if (entryTitle.Contains("Part") && !bookTitle.Contains("Part"))
                    {
                        curTitle.Insert(curTitle.Length, " Box Set");
                    }
                    else curTitle.Insert(curTitle.Length - 1, " Box Set ");
                }
            }
            curTitle.Replace("Vol", string.Empty);
            if (!bookTitle.Contains("One", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("One", "1");
            if (!bookTitle.Contains("Two", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Two", "2");
            if (!bookTitle.Contains("Three", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Three", "3");

            if (InternalHelpers.RemoveNonWordsRegex().Replace(bookTitle, string.Empty).Contains("Attackontitan", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("The Final Season") && entryTitle.Contains("Final Season"))
            {   
                curTitle.Insert("Attack On Titan".Length, " The");
            }
        }
        else
        {
            curTitle = new StringBuilder(entryTitle);
        }

        if (entryTitle.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace("Deluxe: ", "Deluxe Edition").Replace("Deluxe Edition: ", "Deluxe Edition");
            string snapshot = curTitle.ToString();
            if (!snapshot.Contains("Deluxe Edition Vol"))
            {
                int index = snapshot.AsSpan().IndexOf("Vol");
                if (index != -1)
                {
                    curTitle.Insert(index, "Deluxe Edition ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Deluxe Edition");
                }
            }
        }

        curTitle.Replace(",", string.Empty);
        if (!bookTitle.Contains(':') && !entryTitle.ContainsAny(["Year", "Oh", "Edition", "Boruto"]))
        {
            entryTitle = MasterScrape.MultipleWhiteSpaceRegex().Replace(CleanAndParseTitleRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
        }
        else
        {
            entryTitle = MasterScrape.MultipleWhiteSpaceRegex().Replace(ColorCleanAndParseTitleRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
        }
        curTitle = new StringBuilder(entryTitle);
        curTitle.Replace(":", string.Empty);

        if (bookType == BookType.LightNovel)
        {
            curTitle.Replace(" (Light Novel)", string.Empty).Replace(" (Light Novel Hardcover)", string.Empty);
            string snapshot = curTitle.ToString();
            LOGGER.Debug("Snapshot = {}", snapshot);
            if (!snapshot.Contains("Novel"))
            {
                int index = snapshot.AsSpan().IndexOf("Vol");
                if (index != -1)
                {
                    curTitle.Insert(index, "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }
        }
        else
        {
            if (bookTitle.Contains("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(":", string.Empty);
                curTitle.Replace("Naruto Next Generations", string.Empty);
            }
        }

        entryTitle = curTitle.ToString();
        if (entryTitle.Contains("Anniversary") && entryTitle.Contains("Vol"))
        {
            curTitle.Insert(entryTitle.AsSpan().IndexOf("Vol"), $"{AnniversaryMatchRegex().Match(entryTitle).Groups[1].Value} ");
        }
        else if (entryTitle.Contains("Special") && entryTitle.Contains("Edition"))
        {
            curTitle.Insert(entryTitle.AsSpan().IndexOf("Vol"), "Special Edition ");
        }
        // LOGGER.Debug("(3) {}", curTitle.ToString());

        if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Omnibus") && !entryTitle.Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
        {
            curTitle.Insert(curTitle.ToString().Trim().AsSpan().IndexOf("Vol"), "Stray God ");
        }

        if (!entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            Match volMatch = MasterScrape.FindVolNumRegex().Match(entryTitle);
            if (volMatch.Success)
            {
                curTitle.Insert(volMatch.Index, "Vol ");
            };
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, WebDriver? driver = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            WebDriverWait wait = new(driver!, TimeSpan.FromSeconds(60));
            HtmlDocument doc = new();
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

            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            bool isSecondCategory = false;

            string url = GenerateWebsiteUrl(bookType, bookTitle, isSecondCategory);
            links.Add(url);
            driver!.Navigate().GoToUrl(url);
            wait.Until(driver => driver.FindElement(By.CssSelector("div[class='full']")));

            while (true)
            {
                doc.LoadHtml(driver.PageSource);
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                HtmlNodeCollection minorPriceData = doc.DocumentNode.SelectNodes(MinorPriceXPath);
                HtmlNodeCollection bookFormatData = doc.DocumentNode.SelectNodes(BookFormatXPath);
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
               // LOGGER.Debug("{} | {} | {} | {} | {}", titleData.Count, priceData.Count, minorPriceData.Count, bookFormatData.Count, stockStatusData.Count);

                for (int x = 0; x < titleData.Count; x++)
                {
                    string bookFormat = bookFormatData[x].InnerText;
                    string entryTitle = WebUtility.HtmlDecode(titleData[x].GetDirectInnerText());
                    LOGGER.Debug("(-1) {} | {} | {}", entryTitle, bookFormat, minorPriceData[x].InnerText);

                    if (
                        InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && (
                                (
                                bookType == BookType.Manga
                                && (
                                        !entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                        && (bookFormat.Equals("Manga") || bookFormat.Equals("Graphic Novel"))
                                        && !(
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony", "Berserker")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto", "Itachi", "Family Day", "Naruto: Shikamaru's Story", "Naruto: Kakashi's Story")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "fullmetal alchemist", entryTitle, "Under The Faraway Sky")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "FLY")
                                        )
                                    )
                                )
                                || bookType == BookType.LightNovel
                            )
                        && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "Unimplemented")
                        )
                    {
                        bool descIsValid = true;
                        if (bookType == BookType.Manga && (entryTitle.Contains("Hardcover") || !entryTitle.ContainsAny(["Vol", "Box Set", "Comic"])))
                        {
                            HtmlNodeCollection descData = (await _html.LoadFromWebAsync($"https://forbiddenplanet.com{doc.DocumentNode.SelectSingleNode($"(//a[@class='block one-whole clearfix dfbx dfbx--fdc link-banner link--black'])[{x + 1}]").GetAttributeValue("href", string.Empty)}")).DocumentNode.SelectNodes("//div[@id='product-description']/p");
                            StringBuilder desc = new();
                            foreach (HtmlNode node in descData) { desc.AppendLine(node.InnerText); }
                            LOGGER.Debug("Checking Desc {} => {}", entryTitle, desc.ToString());
                            descIsValid = !desc.ToString().ContainsAny(DescRemovalStrings);
                        }

                        if (descIsValid)
                        {
                            string finalTitle = CleanAndParseTitle(bookTitle, entryTitle, bookType);
                            LOGGER.Debug("Final Title = {}", finalTitle);
                            data.Add(
                                new EntryModel(
                                    finalTitle,
                                    $"{priceData[x].GetDirectInnerText()}{minorPriceData[x].InnerText}",
                                    stockStatusData[x].InnerText switch
                                    {
                                        "Pre-Order" => StockStatus.PO,
                                        "Currently Unavailable" => StockStatus.OOS,
                                        _ => StockStatus.IS
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

                if (pageCheck != null)
                {
                    // driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("(//a[@class='product-list__pagination__next'])[1]"))));
                    driver.Navigate().GoToUrl($"https://forbiddenplanet.com{wait.Until(driver => driver.FindElement(By.XPath("(//a[@class='product-list__pagination__next'])[1]"))).GetDomAttribute("href")}");
                    LOGGER.Info("Next Page => {}", driver.Url);
                    wait.Until(driver => driver.FindElement(By.CssSelector("div[class='full']")));
                }
                else if (!isSecondCategory)
                {
                    isSecondCategory = true;
                    LOGGER.Info("Checking Comics & Graphic Novel Cateogry");
                    url = GenerateWebsiteUrl(bookType, bookTitle, isSecondCategory);
                    links.Add(url);

                    driver.Navigate().GoToUrl(url);
                    LOGGER.Info("Next Page (2nd Category) => {}", driver.Url);
                    wait.Until(driver => driver.FindElement(By.CssSelector("div[class='full']")));
                }
                else
                {
                    break;
                }
            }

            data.TrimExcess();
            links.TrimExcess();
            data.Sort(EntryModel.VolumeSort);
            data.RemoveDuplicates(LOGGER);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        catch (Exception ex)
        {
            LOGGER.Error(ex, "{Title} ({BookType}) Error @ {TITLE}", bookTitle, bookType, TITLE);
        }
        finally
        {
            driver?.Quit();
        }

        return (data, links);
    }
}