using Helpers;

namespace MangaLightNovelWebScrape.Websites
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
        private static readonly XPathExpression BookTypeXPAth = XPathExpression.Compile("//li[@class='block right crsr-txt pa--tr pa pqr pql mq type-tag inline-list__item']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//a[@class='product-list__pagination__next'])[1]");

        [GeneratedRegex(@"\(Hardcover\)|:(?:.*):|:", RegexOptions.IgnoreCase)] internal static partial Regex TitleParseRegex();
        [GeneratedRegex(@"(?:3-In-1|3 In 1) Edition|Omnibus:|\(Omnibus Edition\):|Omni 3-In-1|(?:3-In-1|3 In 1)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
        [GeneratedRegex(@"(?:3-In-1|3 In 1)|\(.*\)|(?<=Omnibus \d{1,3})[^\d].*", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRemovalRegex();
        [GeneratedRegex(@"Omnibus (\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex OmnibusMissingVolRegex();
        [GeneratedRegex(@"Box Set:|:\s+Box Set|Box Set (\d{1,3}):|:\s+(\d{1,3}) \(Box Set\)|\(Box Set\)|Box Set Part", RegexOptions.IgnoreCase)] private static partial Regex BoxSetFixRegex();
        [GeneratedRegex(@"(\d{1,3})-\d{1,3}")] private static partial Regex BoxSetVolFindRegex();
        [GeneratedRegex(@"\d{1,3}$")] private static partial Regex FindVolNumRegex();
        [GeneratedRegex(@"(?<=Vol\s+?\d{1,3})[^\d].*|(?<=Box Set\s+?\d{1,3})[^\d].*")] private static partial Regex RemoveAfterVolNumRegex();
        [GeneratedRegex(@"\((.*?Anniversary.*?)\)")] private static partial Regex AnniversaryMatchRegex();


        internal async Task CreateForbiddenPlanetTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetForbiddenPlanetData(bookTitle, bookType, driver));
            });
        }

        internal string GetUrl()
        {
            return ForbiddenPlanetLinks.Count != 0 ? ForbiddenPlanetLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }
        
        internal void ClearData()
        {
            ForbiddenPlanetLinks.Clear();
            ForbiddenPlanetData.Clear();
        }

        private string GetUrl(BookType bookType, string titleText)
        {
            // https://forbiddenplanet.com/catalog/comics-and-graphic-novels/?q=Naruto&show_out_of_stock=on&sort=release-date-asc&page=1
            // https://forbiddenplanet.com/catalog/comics-and-graphic-novels/?q=classroom%20of%20the%20elite%20light%20novel&sort=release-date-asc&page=1
            string url = $"https://forbiddenplanet.com/catalog/comics-and-graphic-novels/?q={(bookType == BookType.Manga ? MasterScrape.FilterBookTitle(titleText) : $"{MasterScrape.FilterBookTitle(titleText)}%20light%20novel")}&show_out_of_stock=on&sort=release-date-asc&page=1";
            ForbiddenPlanetLinks.Add(url);
            LOGGER.Info(url);
            return url;
        }

        // TODO Refactor this so AoT is like CR "Attack on Titan Season 3 Part 2 Box Set"
        private static string TitleParse(string bookTitle, string titleText, BookType bookType)
        {
            titleText = MasterScrape.FixVolumeRegex().Replace(titleText.Trim(), " Vol");
            StringBuilder curTitle;
            if (titleText.EndsWith("(Colour Edition Hardcover)"))
            {
                titleText = titleText.Replace("(Colour Edition Hardcover)", "").Trim();
                titleText = titleText.Insert(titleText.IndexOf("Vol"), "In Color ");
            }
            if (!titleText.Contains("Anniversary") && (titleText.Contains("3-In-1", StringComparison.OrdinalIgnoreCase) || titleText.Contains("3 In 1", StringComparison.OrdinalIgnoreCase) || titleText.Contains("Edition", StringComparison.OrdinalIgnoreCase) || titleText.Contains("Omnibus", StringComparison.OrdinalIgnoreCase)))
            {
                titleText = OmnibusRemovalRegex().Replace(OmnibusFixRegex().Replace(titleText, "Omnibus"), "");
                curTitle = new StringBuilder(titleText);
                curTitle.Replace("Deluxe Edition:", "Edition");

                if (!titleText.Contains("Omnibus"))
                {
                    curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
                }

                if (!titleText.Contains("Vol"))
                {
                    curTitle.Insert(FindVolNumRegex().Match(curTitle.ToString()).Index, "Vol ");
                }
                curTitle.TrimEnd();

                if (!char.IsDigit(curTitle.ToString()[^1]))
                {
                    curTitle.Replace("Omnibus", "");
                    curTitle.TrimEnd();
                    curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
                }
            }
            else if (titleText.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                // titleText = titleText.Replace("Part", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (BoxSetVolFindRegex().IsMatch(titleText))
                {
                    curTitle = new StringBuilder(BoxSetVolFindRegex().Replace(BoxSetFixRegex().Replace(titleText, " Box Set $1"), ""));
                    if (titleText.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || titleText.IndexOf("Vol") < titleText.IndexOf("Box Set"))
                    {
                        curTitle.Append($" {BoxSetVolFindRegex().Match(titleText).Groups[1].Value}");
                    }
                }
                else
                {
                    curTitle = new StringBuilder(BoxSetFixRegex().Replace(titleText, " Box Set $2"));
                    if (!char.IsDigit(titleText[^1]))
                    {
                        curTitle.Replace("Box Set", "");
                        curTitle.TrimEnd();
                        if (!char.IsDigit(curTitle.ToString()[^1]))
                        {   
                            curTitle.Append(" 1");
                        }
                        curTitle.Insert(curTitle.Length - 1, " Box Set ");
                    }
                }
                curTitle.Replace("Vol", "");
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

                if (MasterScrape.RemoveNonWordsRegex().Replace(bookTitle, "").Contains("Attackontitan", StringComparison.OrdinalIgnoreCase) && !titleText.Contains("The Final Season") && titleText.Contains("Final Season"))
                {   
                    curTitle.Insert("Attack On Titan".Length, " The");
                }
            }
            else
            {
                curTitle = new StringBuilder(titleText);
            }

            if (bookType == BookType.LightNovel)
            {
                curTitle.Replace("(Light Novel)", "Novel");
            }

            if (curTitle.ToString().Contains("Anniversary"))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), $"{AnniversaryMatchRegex().Match(curTitle.ToString()).Groups[1].Value} ");
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(TitleParseRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), ""), ""), " ").Trim();
        }

        private List<EntryModel> GetForbiddenPlanetData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                // {
                //     PollingInterval = TimeSpan.FromMilliseconds(500),
                // };

                driver.Navigate().GoToUrl(GetUrl(bookType, bookTitle));
                // wait.Until(e => e.FindElement(By.ClassName("full")));
                driver.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//button[@class='button--brand button--lg brad mql mt']"))); // Get rid of cookies popup
                //wait.Until(e => e.FindElement(By.ClassName("full")));
                wait.Until(driver => driver.FindElement(By.XPath("//h3[@class='h4 clr-black mqt ord--03 one-whole txt-left dtb--fg owl-off']")));

                HtmlDocument doc = new HtmlDocument();
                while (true)
                {
                    doc.LoadHtml(driver.PageSource);

                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection minorPriceData = doc.DocumentNode.SelectNodes(MinorPriceXPath);
                    HtmlNodeCollection bookFormatData = doc.DocumentNode.SelectNodes(BookTypeXPAth);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string bookFormat = stockStatusData[x].FirstChild.InnerText;
                        string titleText = titleData[x].GetDirectInnerText();
                        if (
                            MasterScrape.TitleContainsBookTitle(bookTitle, titleText)
                            && !MasterScrape.EntryRemovalRegex().IsMatch(titleText)
                            &&  (
                                    (
                                    bookType == BookType.Manga 
                                    && (
                                            !titleText.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                            && (bookFormat.Equals("Manga") || bookFormat.Equals("Graphic Novel"))
                                            && (
                                                    titleText.AsParallel().Any(char.IsDigit)
                                                    || (!titleText.AsParallel().Any(char.IsDigit) && titleText.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
                                                )
                                            && !(
                                                MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText, "of Gluttony")
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Boruto")
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Itachi")
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Bleach", titleText, "Can't Fear")
                                            )
                                        )
                                    )
                                || bookType == BookType.LightNovel
                                )
                            )
                        {
                            titleText = TitleParse(bookTitle, titleText, bookType);
                            if (!ForbiddenPlanetData.Exists(entry => entry.Entry.Equals(titleText)))
                            ForbiddenPlanetData.Add(
                                new EntryModel(
                                    titleText,
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
                    }

                    if (pageCheck != null)
                    {
                        driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("(//a[@class='product-list__pagination__next'])[1]"))));
                        wait.Until(driver => driver.FindElement(By.XPath("//form[@class='product-list main__content centered one-whole']")));
                        LOGGER.Info(driver.Url);
                    }
                    else
                    {
                        break;
                    }
                }

                ForbiddenPlanetData.Sort(MasterScrape.VolumeSort);

                if (MasterScrape.IsDebugEnabled)
                {
                    using (StreamWriter outputFile = new(@"Data\ForbiddenPlanetData.txt"))
                    {
                        if (ForbiddenPlanetData.Count != 0)
                        {
                            foreach (EntryModel data in ForbiddenPlanetData)
                            {
                                outputFile.WriteLine(data);
                                LOGGER.Debug(data);
                            }
                        }
                        else
                        {
                            LOGGER.Error($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                            outputFile.WriteLine($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                        }
                    } 
                }
            }
            catch (Exception e)
            {
                driver.Close();
                driver.Quit();
                LOGGER.Error($"{bookTitle} Does Not Exist @ {WEBSITE_TITLE} \n{e}");
            }
            return ForbiddenPlanetData;
        }
    }
}