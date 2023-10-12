namespace MangaLightNovelWebScrape.Websites
{
    public partial class Crunchyroll
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("CrunchyrollLogs");
        private List<string> CrunchyrollLinks = new List<string>();
        private List<EntryModel> CrunchyrollData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Crunchyroll";
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        private const Region WEBSITE_REGION = Region.America | Region.Canada;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='pdp-link']/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='sales']/span");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[contains(@class, 'sash-content')]");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//a[@class='right-arrow']");
        
        [GeneratedRegex(@"\(.*?\)| Manga|,|:")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(@"(?:3 in 1|2 in 1|Omnibus) Edition", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

        internal void ClearData()
        {
            CrunchyrollLinks.Clear();
            CrunchyrollData.Clear();
        }

        internal async Task CreateCrunchyrollTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetCrunchyrollData(bookTitle, book, isMember, 1, driver));
            });
        }

        internal string GetUrl()
        {
            return CrunchyrollLinks.Count != 0 ? CrunchyrollLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GetUrl(BookType bookType, string bookTitle, int nextPage)
        {
            // https://store.crunchyroll.com/search?q=jujutsu%20kaisen&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga
            // https://store.crunchyroll.com/search?q=overlord&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga
            // https://store.crunchyroll.com/search?q=overlord&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Novels
            
            String url = $"https://store.crunchyroll.com/search?q={MasterScrape.FilterBookTitle(bookTitle)}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2={(bookType == BookType.Manga ? "Manga" : "Novels")}&srule=Product%20Name%20(A-Z)&start={nextPage}&sz=100";
            LOGGER.Debug(url);
            CrunchyrollLinks.Add(url);
            return url;
        }

        private static string TitleParse(string titleText, BookType bookType)
        {
            StringBuilder curTitle;
            if (OmnibusRegex().IsMatch(titleText))
            {
                curTitle = new StringBuilder(OmnibusRegex().Replace(titleText, "Omnibus"));
            }
            else
            {
                curTitle = new StringBuilder(titleText);
            }

            if (bookType == BookType.Manga)
            {
                if (titleText.Contains("Deluxe"))
                {
                    curTitle.Replace("Omnibus ", "").Replace("Deluxe Edition", "Deluxe");
                }
            }
            else if (bookType == BookType.LightNovel && !titleText.Contains("Novel"))
            {
                if (titleText.Contains("Vol"))
                {
                    curTitle.Insert(titleText.IndexOf("Vol"), "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }
            return curTitle.ToString().Trim();
        }


        // TODO Need to recalvulate how to add memberhsip discount with new CR membership
        // TODO FMAB not working for some reaosn
        // TODO Add AoT test
        private List<EntryModel> GetCrunchyrollData(string bookTitle, BookType bookType, bool memberStatus, byte currPageNum, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                HtmlDocument doc = new();
                int nextPage = 0;

                while (true)
                {
                    // Initialize the html doc for crawling
                    driver.Navigate().GoToUrl(GetUrl(bookType, bookTitle, nextPage));
                    wait.Until(driver => driver.FindElement(By.CssSelector("div.product-grid.d-flex.flex-wrap")));
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string titleText = titleData[x].InnerText;        
                        if(
                            !titleText.Contains("Imperfect")
                            && MasterScrape.TitleContainsBookTitle(bookTitle, titleText.ToString()) 
                            && !(
                                    bookType == BookType.Manga
                                    && (
                                            MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText.ToString(), "of Gluttony")
                                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText.ToString(), "Boruto")
                                        )
                                )
                        )
                        {
                            StockStatus stockStatus = stockStatusData[x].InnerText.Trim() switch
                            {
                                "IN-STOCK" => StockStatus.IS,
                                "SOLD-OUT" => StockStatus.OOS,
                                "PRE-ORDER" => StockStatus.PO,
                                _ => StockStatus.NA,
                            };

                            if (stockStatus != StockStatus.NA)
                            {
                                CrunchyrollData.Add(
                                    new EntryModel
                                    (
                                        TitleParse(MasterScrape.FixVolumeRegex().Replace(TitleParseRegex().Replace(titleText, "").Trim(), "Vol"), bookType),
                                        memberStatus ? $"${EntryModel.ApplyDiscount(decimal.Parse(priceData[x].InnerText.Trim()[1..]), MEMBERSHIP_DISCOUNT)}" : priceData[x].InnerText.Trim(),
                                        stockStatus,
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                        }
                    }

                    if (pageCheck != null)
                    {
                        // driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("//a[@rel='next']"))));
                        // wait.Until(driver => driver.FindElement(By.XPath("//div[@class='pdp-link']/a"))); //div[@class='pdp-link']/a
                        // LOGGER.Info($"Next Page {driver.Url}");
                        nextPage += 100;
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        break;
                    }
                }
                CrunchyrollData.Sort(new VolumeSort());
            }
            catch (Exception ex)
            {
                driver.Close();
                driver.Quit();
                LOGGER.Error($"{bookTitle} Does Not Exist @ Crunchyroll \n{ex}");
            }

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\CrunchyrollData.txt"))
                {
                    if (CrunchyrollData.Count != 0)
                    {
                        foreach (EntryModel data in CrunchyrollData)
                        {
                            LOGGER.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        LOGGER.Debug(bookTitle + " Does Not Exist at Crunchyroll");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at Crunchyroll");
                    }
                } 
            }

            return CrunchyrollData;
        }
    }
}