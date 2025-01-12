using System.Net;

namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class ForbiddenPlanet
    {
        private List<string> ForbiddenPlanetLinks = new List<string>();
        private List<EntryModel> ForbiddenPlanetData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Forbidden Planet";
        public const Region REGION = Region.Britain;
        private static readonly List<string> DescRemovalStrings = [ "novel", "original stories", "collecting issues"];
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='full']/ul/li/section/header/div[2]/h3/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='clr-price']");
        private static readonly XPathExpression MinorPriceXPath = XPathExpression.Compile("(//div[@class='full']/ul/li/section/header/div[2])/p/span[2]");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='full']/ul/li/section/header/div/ul");
        private static readonly XPathExpression BookTypeXPath = XPathExpression.Compile("(//div[@class='full']/ul/li/section/header/div[1])/p");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//a[@class='product-list__pagination__next'])[1]");

        [GeneratedRegex(@" The Manga|\(Hardcover\)|:(?:.*):|\(.*\)", RegexOptions.IgnoreCase)] internal static partial Regex TitleParseRegex();
        [GeneratedRegex(@"\(Hardcover\)", RegexOptions.IgnoreCase)] internal static partial Regex ColorTitleParseRegex();
        [GeneratedRegex(@":.*(?:(?:3|2)-In-1|(?:3|2) In 1) Edition\s{0,3}:|:.*(?:(?:3|2)-In-1|(?:3|2) In 1)\s{0,3}:|(?:(?:3|2)-In-1|(?:3|2) In 1)\s{0,3} Edition:|\(Omnibus Edition\)|Omnibus\s{0,}(\d{1,3}|\d{1,3}.\d{1})(?:\s{0,}|:\s{0,})(?:\(.*\)|Vol \d{1,3}-\d{1,3})|:.*Omnibus:\s+(\d{1,3}).*|:.*:\s+Vol\s+(\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
        [GeneratedRegex(@"Box Set:|:\s+Box Set|Box Set (\d{1,3}):|:\s+(\d{1,3}) \(Box Set\)|\(Box Set\)|Box Set Part", RegexOptions.IgnoreCase)] private static partial Regex BoxSetFixRegex();
        [GeneratedRegex(@"(\d{1,3})-\d{1,3}")] private static partial Regex BoxSetVolFindRegex();
        [GeneratedRegex(@"(?<=(?:Vol|Box Set)\s+(?:\d{1,3}|\d{1,3}.\d{1}))[^\d{1,3}.]+.*")] private static partial Regex RemoveAfterVolNumRegex();
        [GeneratedRegex(@"\((.*?Anniversary.*?)\)")] private static partial Regex AnniversaryMatchRegex();
        [GeneratedRegex(@"Volumes|Volume|Vol\.|Volumr", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

        protected internal async Task CreateForbiddenPlanetTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetForbiddenPlanetData(bookTitle, bookType, driver));
            });
        }

        protected internal string GetUrl()
        {
            return ForbiddenPlanetLinks.Count != 0 ? ForbiddenPlanetLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }
        
        protected internal void ClearData()
        {
            ForbiddenPlanetLinks.Clear();
            ForbiddenPlanetData.Clear();
        }

        private string GenerateWebsiteUrl(BookType bookType, string entryTitle, bool isSecondCategory)
        {
            // https://forbiddenplanet.com/catalog/?q=Naruto&show_out_of_stock=on&sort=release-date-asc&page=1
            string url = $"https://forbiddenplanet.com/catalog/{(!isSecondCategory ? "manga" : "comics-and-graphic-novels")}/?q={(bookType == BookType.Manga ? InternalHelpers.FilterBookTitle(entryTitle) : $"{InternalHelpers.FilterBookTitle(entryTitle)}%20light%20novel")}&show_out_of_stock=on&sort=release-date-asc&page=1";
            ForbiddenPlanetLinks.Add(url);
            LOGGER.Info($"Url = {url}");
            return url;
        }

        private static string TitleParse(string bookTitle, string entryTitle, BookType bookType)
        {
            entryTitle = FixVolumeRegex().Replace(entryTitle.Trim(), " Vol");
            StringBuilder curTitle;
            if (entryTitle.EndsWith("(Colour Edition Hardcover)"))
            {
                entryTitle = entryTitle.Replace("(Colour Edition Hardcover)", string.Empty).Trim();
                entryTitle = entryTitle.Insert(entryTitle.IndexOf("Vol"), "In Color ");
            }

            if (!entryTitle.Contains("Anniversary") && (entryTitle.Contains("3-In-1", StringComparison.OrdinalIgnoreCase) || entryTitle.Contains("3 In 1", StringComparison.OrdinalIgnoreCase) || entryTitle.Contains("Omnibus", StringComparison.OrdinalIgnoreCase)))
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
                    curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index, "Vol ");
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
                curTitle.Replace("Deluxe: ", "Deluxe Edition");
                curTitle.Replace("Deluxe Edition: ", "Deluxe Edition");
                if (!curTitle.ToString().Contains("Deluxe Edition Vol"))
                {
                    int index = curTitle.ToString().AsSpan().IndexOf("Vol");
                    if (index != -1) curTitle.Insert(index, "Deluxe Edition ");
                    else curTitle.Insert(curTitle.Length, " Deluxe Edition");
                }
            }

            if (bookType == BookType.LightNovel)
            {
                curTitle.Replace(" (Light Novel)", string.Empty);
                curTitle.Replace(" (Light Novel Hardcover)", string.Empty);
                if (!curTitle.ToString().Contains("Novel"))
                {
                    int index = curTitle.ToString().AsSpan().IndexOf("Vol");
                    if (index != -1) curTitle.Insert(index, "Novel ");
                    else curTitle.Insert(curTitle.Length, " Novel");
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

            curTitle.Replace(",", string.Empty);
            if (!bookTitle.Contains(':'))
            {
                curTitle.Replace(":", string.Empty);
                entryTitle = MasterScrape.MultipleWhiteSpaceRegex().Replace(TitleParseRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
            }
            else
            {
                entryTitle = MasterScrape.MultipleWhiteSpaceRegex().Replace(ColorTitleParseRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
            }
            curTitle = new StringBuilder(entryTitle);

            if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Omnibus")  && !entryTitle.Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
            {
                curTitle.Insert(curTitle.ToString().Trim().AsSpan().IndexOf("Vol"), "Stray God ");
            }
            return curTitle.ToString();
        }

        private List<EntryModel> GetForbiddenPlanetData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                HtmlDocument doc = new HtmlDocument();
                HtmlWeb web = new()
                {
                    UsingCacheIfExists = true,
                    UseCookies = true
                };
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                bool isSecondCategory = false;

                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookType, bookTitle, isSecondCategory));
                wait.Until(driver => driver.FindElement(By.CssSelector("div[class='full']")));

                while (true)
                {
                    doc.LoadHtml(driver.PageSource);
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection minorPriceData = doc.DocumentNode.SelectNodes(MinorPriceXPath);
                    HtmlNodeCollection bookFormatData = doc.DocumentNode.SelectNodes(BookTypeXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                    // LOGGER.Debug("{} | {} | {} | {} | {}", titleData.Count, priceData.Count, minorPriceData.Count, bookFormatData.Count, stockStatusData.Count);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string bookFormat = bookFormatData[x].InnerText;
                        string entryTitle = titleData[x].GetDirectInnerText();
                        // LOGGER.Debug("(-1) {} | {} | {}", entryTitle, bookFormat, minorPriceData[x].InnerText);
                        if (
                            InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            &&  (
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
                                            )
                                        )
                                    )
                                    || bookType == BookType.LightNovel
                                )
                            && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "Unimplemented")
                            )
                        {
                            bool descIsValid = true;
                            if (bookType == BookType.Manga && (entryTitle.Contains("Hardcover") || !entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set") && !entryTitle.Contains("Comic", StringComparison.OrdinalIgnoreCase)))
                            {
                                // LOGGER.Debug("{} Checking Desc", entryTitle);
                                HtmlNodeCollection descData = web.Load($"https://forbiddenplanet.com{doc.DocumentNode.SelectSingleNode($"(//a[@class='block one-whole clearfix dfbx dfbx--fdc pt'])[{x + 1}]").GetAttributeValue("href", string.Empty)}").DocumentNode.SelectNodes("//div[@id='product-description']/p");
                                StringBuilder desc = new StringBuilder();
                                foreach (HtmlNode node in descData) { desc.AppendLine(node.InnerText); }
                                // LOGGER.Debug("Checking Desc {} => {}", entryTitle, desc.ToString());
                                descIsValid = !desc.ToString().ContainsAny(DescRemovalStrings);
                            }

                            if (descIsValid)
                            {
                                ForbiddenPlanetData.Add(
                                    new EntryModel(
                                        WebUtility.HtmlDecode(TitleParse(bookTitle, entryTitle, bookType)),
                                        $"{priceData[x].GetDirectInnerText()}{minorPriceData[x].InnerText}", 
                                        stockStatusData[x].InnerText switch
                                        {
                                            "Pre-Order" => StockStatus.PO,
                                            "Currently Unavailable" => StockStatus.OOS,
                                             _ => StockStatus.IS
                                        },
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                            else { LOGGER.Info("Removed (2) {}", entryTitle); }
                        }
                        else { LOGGER.Info("Removed (1) {}", entryTitle); }
                    }

                    if (pageCheck != null)
                    {
                        driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("(//a[@class='product-list__pagination__next'])[1]"))));
                        wait.Until(driver => driver.FindElement(By.CssSelector("div[class='full']")));
                        LOGGER.Info($"Url = {driver.Url}");
                    }
                    else if (!isSecondCategory)
                    {
                        isSecondCategory = true;
                        LOGGER.Info("Checking Comics & Graphic Novel Cateogry");
                        driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookType, bookTitle, isSecondCategory));
                        wait.Until(driver => driver.FindElement(By.CssSelector("div[class='full']")));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error($"{bookTitle} | {bookType} Does Not Exist @ {WEBSITE_TITLE} \n{e}");
            }
            finally
            {
                driver?.Quit();
                ForbiddenPlanetData.Sort(EntryModel.VolumeSort);
                ForbiddenPlanetData.RemoveDuplicates(LOGGER);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, bookType, ForbiddenPlanetData, LOGGER);
            }
            return ForbiddenPlanetData;
        }
    }
}