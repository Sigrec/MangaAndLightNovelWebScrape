namespace MangaLightNovelWebScrape.Websites.America
{
    public partial class RightStufAnime
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("RightStufAnimeLogs");
        private List<string> RightStufAnimeLinks = new List<string>();
        private List<EntryModel> RightStufAnimeData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "RightStufAnime";
        private const decimal GOT_ANIME_DISCOUNT = 0.1M;
        private const Region WEBSITE_REGION = Region.America | Region.Canada;
        
        [GeneratedRegex("\\(.*?\\)")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(" Manga|,|:")] private static partial Regex FormatRemovalRegex();
        [GeneratedRegex("3 [iI]n 1|2 [iI]n 1")] private static partial Regex OmnibusRegex();

        internal void ClearData()
        {
            RightStufAnimeLinks.Clear();
            RightStufAnimeData.Clear();
        }

        internal async Task CreateRightStufAnimeTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetRightStufAnimeData(bookTitle, book, isMember, 1, driver));
            });
        }

        internal string GetUrl()
        {
            return RightStufAnimeLinks.Count != 0 ? RightStufAnimeLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GetUrl(BookType bookType, byte currPageNum, string bookTitle)
        {
            StringBuilder url = new StringBuilder("https://www.rightstufanime.com/category/");
            url.Append(bookType == BookType.Manga ? "Manga" : "Novels").Append("?page=").Append(currPageNum).Append("&show=96&keywords=").Append(MasterScrape.FilterBookTitle(bookTitle));
            LOGGER.Debug(url.ToString());
            RightStufAnimeLinks.Add(url.ToString());
            return url.ToString();
        }

        private static string TitleParse(string titleText, BookType bookType)
        {
            StringBuilder curTitle = new StringBuilder(OmnibusRegex().Replace(FormatRemovalRegex().Replace(titleText, ""), "Omnibus"));
            curTitle.Replace("Volume", "Vol").ToString();

            if (bookType == BookType.Manga)
            {
                curTitle.Replace("Omnibus Edition", "Omnibus");
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

        private List<EntryModel> GetRightStufAnimeData(string bookTitle, BookType bookType, bool memberStatus, byte currPageNum, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                string titleText;
                decimal priceVal;
                StockStatus stockStatus;
                bool anotherPage = true;
                while (anotherPage)
                {
                    driver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='shopping-layout-content']")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//span[@itemprop='name']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//span[@itemprop='price']");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='product-line-stock-container '] | //span[@class='product-line-stock-msg-out-text']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//li[@class='global-views-pagination-next']");

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        titleText = TitleParseRegex().Replace(titleData[x].InnerText, "").Trim();          
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
                            priceVal = Convert.ToDecimal(priceData[x].InnerText.Trim());
                            stockStatus = stockStatusData[x].InnerText switch
                                        {
                                            string curStatus when curStatus.Contains("In Stock") => StockStatus.IS,
                                            string curStatus when curStatus.Contains("Out of Stock") => StockStatus.OOS,
                                            string curStatus when curStatus.Contains("Pre-Order") => StockStatus.PO,
                                            _ => StockStatus.NA,
                                        };
                            if (stockStatus != StockStatus.NA)
                            {
                                RightStufAnimeData.Add(
                                    new EntryModel
                                    (
                                        TitleParse(titleText, bookType),
                                        $"${(memberStatus ? EntryModel.ApplyDiscount(priceVal, GOT_ANIME_DISCOUNT) : priceVal)}",
                                        stockStatus,
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                        }
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        anotherPage = false;
                    }
                }
                RightStufAnimeData.Sort(new VolumeSort());
            }
            catch (Exception ex)
            {
                driver.Close();
                driver.Quit();
                LOGGER.Error($"{bookTitle} Does Not Exist @ RightStufAnime \n{ex}");
            }

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\RightStufAnimeData.txt"))
                {
                    if (RightStufAnimeData.Count != 0)
                    {
                        foreach (EntryModel data in RightStufAnimeData)
                        {
                            LOGGER.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        LOGGER.Debug(bookTitle + " Does Not Exist at RightStufAnime");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at RightStufAnime");
                    }
                } 
            }

            return RightStufAnimeData;
        }
    }
}