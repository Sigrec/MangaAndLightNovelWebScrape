namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class ForbiddenPlanet
    {
        private List<string> ForbiddenPlanetLinks = new List<string>();
        private List<EntryModel> ForbiddenPlanetData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Forbidden Planet";
        public const Region WEBSITE_REGION = Region.Britain;
        private static readonly Logger LOGGER = LogManager.GetLogger("ForbiddenPlanetLogs");
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//h3[@class='h4 clr-black mqt ord--03 one-whole txt-left dtb--fg owl-off']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='clr-price']");
        private static readonly XPathExpression MinorPriceXPath = XPathExpression.Compile("//span[@class='t-small clr-price']");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//ul[@class='inline-list inline-list--q ord--02 mqt']");
        private static readonly XPathExpression BookTypeXPath = XPathExpression.Compile("//li[@class='block right crsr-txt pa--tr pa pqr pql mq type-tag inline-list__item']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//a[@class='product-list__pagination__next'])[1]");

        [GeneratedRegex(@"\(Hardcover\)|:(?:.*):", RegexOptions.IgnoreCase)] internal static partial Regex TitleParseRegex();
        [GeneratedRegex(@"\(Hardcover\)", RegexOptions.IgnoreCase)] internal static partial Regex ColonTitleParseRegex();
        [GeneratedRegex(@"(?:3-In-1|3 In 1) Edition|Omnibus:|\(Omnibus Edition\):|Omni 3-In-1|(?:3-In-1|3 In 1)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
        [GeneratedRegex(@"(?:3-In-1|3 In 1)|\(.*\)|(?<=Omnibus \d{1,3})[^\d].*", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRemovalRegex();
        [GeneratedRegex(@"Omnibus (\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex OmnibusMissingVolRegex();
        [GeneratedRegex(@"Box Set:|:\s+Box Set|Box Set (\d{1,3}):|:\s+(\d{1,3}) \(Box Set\)|\(Box Set\)|Box Set Part", RegexOptions.IgnoreCase)] private static partial Regex BoxSetFixRegex();
        [GeneratedRegex(@"(\d{1,3})-\d{1,3}")] private static partial Regex BoxSetVolFindRegex();
        [GeneratedRegex(@"\d{1,3}$")] private static partial Regex FindVolNumRegex();
        [GeneratedRegex(@"(?<=Vol\s+?\d{1,3})[^\d].*|(?<=Box Set\s+?\d{1,3})[^\d].*")] private static partial Regex RemoveAfterVolNumRegex();
        [GeneratedRegex(@"\((.*?Anniversary.*?)\)")] private static partial Regex AnniversaryMatchRegex();
        [GeneratedRegex(@"Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();


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

        private string GenerateWebsiteUrl(BookType bookType, string entryTitle)
        {
            // https://forbiddenplanet.com/catalog/comics-and-graphic-novels/?q=Naruto&show_out_of_stock=on&sort=release-date-asc&page=1
            // https://forbiddenplanet.com/catalog/comics-and-graphic-novels/?q=classroom%20of%20the%20elite%20light%20novel&sort=release-date-asc&page=1
            string url = $"https://forbiddenplanet.com/catalog/{(bookType == BookType.Manga ? "manga" : string.Empty)}/?q={(bookType == BookType.Manga ? InternalHelpers.FilterBookTitle(entryTitle) : $"{InternalHelpers.FilterBookTitle(entryTitle)}%20light%20novel")}&show_out_of_stock=on&sort=release-date-asc&page=1";
            ForbiddenPlanetLinks.Add(url);
            LOGGER.Info($"Url = {url}");
            return url;
        }

        private static void RemoveCookiePopup(WebDriver driver, WebDriverWait wait)
        {
            driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.CssSelector("button[class='button--brand button--lg brad mql mt'")))); // Get rid of cookies popup
            wait.Until(driver => driver.FindElement(By.ClassName("full")));
        }

        // TODO Refactor this so AoT is like CR "Attack on Titan Season 3 Part 2 Box Set"
        private static string TitleParse(string bookTitle, string entryTitle, BookType bookType)
        {
            entryTitle = FixVolumeRegex().Replace(entryTitle.Trim(), " Vol");
            StringBuilder curTitle;
            if (entryTitle.EndsWith("(Colour Edition Hardcover)"))
            {
                entryTitle = entryTitle.Replace("(Colour Edition Hardcover)", string.Empty).Trim();
                entryTitle = entryTitle.Insert(entryTitle.IndexOf("Vol"), "In Color ");
            }
            if (!entryTitle.Contains("Anniversary") && (entryTitle.Contains("3-In-1", StringComparison.OrdinalIgnoreCase) || entryTitle.Contains("3 In 1", StringComparison.OrdinalIgnoreCase) || entryTitle.Contains("Edition", StringComparison.OrdinalIgnoreCase) || entryTitle.Contains("Omnibus", StringComparison.OrdinalIgnoreCase)))
            {
                entryTitle = OmnibusRemovalRegex().Replace(OmnibusFixRegex().Replace(entryTitle, "Omnibus"), string.Empty);
                curTitle = new StringBuilder(entryTitle);
                curTitle.Replace("Deluxe Edition:", "Edition");

                if (!entryTitle.Contains("Omnibus"))
                {
                    curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
                }

                if (!entryTitle.Contains("Vol"))
                {
                    curTitle.Insert(FindVolNumRegex().Match(curTitle.ToString()).Index, "Vol ");
                }
                curTitle.TrimEnd();

                if (!char.IsDigit(curTitle.ToString()[^1]))
                {
                    curTitle.Replace("Omnibus", string.Empty);
                    curTitle.TrimEnd();
                    curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
                }
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
                        if (!char.IsDigit(curTitle.ToString()[^1]))
                        {   
                            curTitle.Append(" 1");
                        }
                        curTitle.Insert(curTitle.Length - 1, " Box Set ");
                    }
                }
                curTitle.Replace("Vol", string.Empty);
                if (!bookTitle.Contains("One", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("One", "1");
                }
                if (!bookTitle.Contains("Two", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Two", "2");
                }
                if (!bookTitle.Contains("Three", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Three", "3");
                }

                if (InternalHelpers.RemoveNonWordsRegex().Replace(bookTitle, string.Empty).Contains("Attackontitan", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("The Final Season") && entryTitle.Contains("Final Season"))
                {   
                    curTitle.Insert("Attack On Titan".Length, " The");
                }
            }
            else
            {
                curTitle = new StringBuilder(entryTitle);
            }

            if (bookType == BookType.LightNovel)
            {
                curTitle.Replace("(Light Novel)", "Novel");
            }

            if (curTitle.ToString().Contains("Anniversary") && curTitle.ToString().Contains("Vol"))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), $"{AnniversaryMatchRegex().Match(curTitle.ToString()).Groups[1].Value} ");
            }

            if (!bookTitle.Contains(':'))
            {
                curTitle.Replace(":", string.Empty);
                return MasterScrape.MultipleWhiteSpaceRegex().Replace(TitleParseRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
            }
            else
            {
                return MasterScrape.MultipleWhiteSpaceRegex().Replace(ColonTitleParseRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
            }
        }

        private List<EntryModel> GetForbiddenPlanetData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
                HtmlDocument doc = new HtmlDocument();
                bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle);

                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookType, bookTitle));
                RemoveCookiePopup(driver, wait);
                doc.LoadHtml(driver.PageSource);
                // LOGGER.Debug(doc.Text);
                while (true)
                {
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection minorPriceData = doc.DocumentNode.SelectNodes(MinorPriceXPath);
                    HtmlNodeCollection bookFormatData = doc.DocumentNode.SelectNodes(BookTypeXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                    LOGGER.Debug("{} | {} | {} | {} | {}", titleData.Count, priceData.Count, minorPriceData.Count, bookFormatData.Count, stockStatusData.Count);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string bookFormat = stockStatusData[x].FirstChild.InnerText;
                        string entryTitle = titleData[x].GetDirectInnerText();
                        if (
                            InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            &&  (
                                    (
                                    bookType == BookType.Manga 
                                    && (
                                            !entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                            && (bookFormat.Equals("Manga") || bookFormat.Equals("Graphic Novel"))
                                            && (
                                                    entryTitle.AsParallel().Any(char.IsDigit)
                                                    || (!entryTitle.AsParallel().Any(char.IsDigit) && entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
                                                )
                                            && !(
                                                InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                                || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                                || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Itachi")
                                                || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear")
                                            )
                                        )
                                    )
                                    || bookType == BookType.LightNovel
                                )
                            )
                        {
                            entryTitle = TitleParse(bookTitle, entryTitle, bookType);
                            if (!ForbiddenPlanetData.Exists(entry => entry.Entry.Equals(entryTitle)))
                            {
                                ForbiddenPlanetData.Add(
                                    new EntryModel(
                                        entryTitle,
                                        $"{priceData[x].GetDirectInnerText()}{minorPriceData[x].InnerText}", 
                                        stockStatusData[x].LastChild.InnerText switch
                                        {
                                            "Pre-Order" => StockStatus.PO,
                                            "Currently Unavailable" => StockStatus.OOS,
                                            _ => StockStatus.IS
                                        },
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                            else { LOGGER.Info("Removed {}", entryTitle); }
                        }
                        else { LOGGER.Info("Removed {}", entryTitle); }
                    }

                    if (pageCheck != null)
                    {
                        driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("(//a[@class='product-list__pagination__next'])[1]"))));
                        RemoveCookiePopup(driver, wait);
                        doc.LoadHtml(driver.PageSource);
                        LOGGER.Info($"Url = {driver.Url}");
                    }
                    else
                    {
                        // driver.Quit();
                        break;
                    }
                }

                ForbiddenPlanetData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, ForbiddenPlanetData, LOGGER);
            }
            catch (Exception e)
            {
                // driver?.Quit();
                LOGGER.Error($"{bookTitle} Does Not Exist @ {WEBSITE_TITLE} \n{e}");
            }
            return ForbiddenPlanetData;
        }
    }
}