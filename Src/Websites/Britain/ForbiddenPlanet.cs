using System.ComponentModel.Design;
using System.Collections.ObjectModel;

namespace MangaLightNovelWebScrape.Websites.Britain
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
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//div[@class='load-more-navigation pt']/button[1]");

        [GeneratedRegex(@":(?:.*):|\(Hardcover\)|(?<=Vol \d{1,3})[^\d].*")] internal static partial Regex TitleParseRegex();
        [GeneratedRegex(@"(?:3-In-1|3 In 1) Edition:|Omnibus:|\(Omnibus Edition\):", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();


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

        private static string TitleParse(string bookTitle, string titleText, BookType bookType)
        {
            titleText = MasterScrape.FixVolumeRegex().Replace(titleText, "Vol");
            StringBuilder curTitle;
            if (titleText.Contains("3-In-1", StringComparison.OrdinalIgnoreCase) || titleText.Contains("Edition") || titleText.Contains("Omnibus"))
            {
                curTitle = new StringBuilder(OmnibusFixRegex().Replace(titleText, "Omnibus")).Replace("3-In-1", "");
                curTitle.Replace("Deluxe Edition:", "Edition");
                if (!curTitle.ToString().Contains("Omnibus"))
                {
                    curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
                }
            }
            else
            {
                string test = null;
                curTitle = new StringBuilder(titleText);
                if (titleText.Contains("Box Set"))
                {
                    curTitle.Replace("Box Set:", "Box Set");
                    test = "m";
                }

                if (bookType == BookType.LightNovel)
                {
                    curTitle.Replace("(Light Novel)", "Novel");
                    return test;
                }
            }

            if (!bookTitle.Contains(':'))
            {
                curTitle.Replace(":", "");
            }
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(TitleParseRegex().Replace(curTitle.ToString(), ""), " ");
        }

        private List<EntryModel> GetForbiddenPlanetData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                driver.Navigate().GoToUrl(GetUrl(bookType, bookTitle));
                // wait.Until(e => e.FindElement(By.ClassName("full")));
                driver.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//button[@class='button--brand button--lg brad mql mt']"))); // Get rid of cookies popup
                //wait.Until(e => e.FindElement(By.ClassName("full")));
                wait.Until(e => e.FindElement(By.XPath("//form[@class='product-list main__content centered one-whole']")));

                ReadOnlyCollection<IWebElement> pageCheck = driver.FindElements(By.CssSelector(".load-more-navigation"));
                while (pageCheck.Count == 1) // Need better logic here as it loops to many times
                {
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("//div[@class='load-more-navigation pt']/button[1]"))));
                    wait.Until(e => e.FindElement(By.XPath("//form[@class='product-list main__content centered one-whole']")));
                    pageCheck = driver.FindElements(By.CssSelector(".load-more-navigation"));
                    LOGGER.Info($"Loading More Entries {pageCheck.Count}");
                }

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(driver.PageSource);

                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                HtmlNodeCollection minorPriceData = doc.DocumentNode.SelectNodes(MinorPriceXPath);
                HtmlNodeCollection bookFormatData = doc.DocumentNode.SelectNodes(BookTypeXPAth);
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);

                for (int x = 0; x < titleData.Count; x++)
                {
                    string bookFormat = stockStatusData[x].FirstChild.InnerText;
                    string titleText = titleData[x].GetDirectInnerText();
                    if (
                        MasterScrape.TitleContainsBookTitle(bookTitle, titleText)
                        && !MasterScrape.EntryRemovalRegex().IsMatch(titleText)
                        && (
                            (
                                bookType == BookType.Manga 
                                && (
                                        !titleText.Contains("Light Novel")
                                        && (bookFormat.Equals("Manga") || bookFormat.Equals("Graphic Novel"))
                                        && titleText.Any(char.IsDigit)
                                        && !(
                                            MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText, "of Gluttony")
                                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Boruto")
                                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Itachi")
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
                                TitleParse(bookTitle, titleText, bookType),
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
                ForbiddenPlanetData.Sort(new VolumeSort());

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
                // driver.Close();
                // driver.Quit();
                LOGGER.Error($"{bookTitle} Does Not Exist @ {WEBSITE_TITLE} \n{e}");
            }
            return ForbiddenPlanetData;
        }
    }
}