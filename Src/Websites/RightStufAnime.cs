namespace MangaLightNovelWebScrape.Websites
{
    partial class RightStufAnime
    {
        public static List<string> RightStufAnimeLinks = new();
        public static List<EntryModel> RightStufAnimeData = new();
        public const string WEBSITE_TITLE = "RightStufAnime";
        private const decimal GOT_ANIME_DISCOUNT = 0.1M;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("RightStufAnimeLogs");
        [GeneratedRegex("\\(.*?\\)")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(" Manga| Edition")] private static partial Regex FormatRemovalRegex();

        private static string FilterBookTitle(string bookTitle){
            char[] trimedChars = {' ', '\'', '!', '-'};
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), "%" + Convert.ToByte(var).ToString("x2").ToString());
            }
            return bookTitle;
        }

        public static void ClearData()
        {
            RightStufAnimeLinks.Clear();
            RightStufAnimeData.Clear();
        }

        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url = $"https://www.rightstufanime.com/category/{(bookType == 'M' ? "Manga" : "Novels")}?page={currPageNum}&show=96&keywords={FilterBookTitle(bookTitle)}";
            Logger.Debug(url);
            RightStufAnimeLinks.Add(url);
            return url;
        }

        public static List<EntryModel> GetRightStufAnimeData(string bookTitle, char bookType, bool memberStatus, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);

            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(30));
                decimal priceVal;
                string currTitle;
                while (true)
                {
                    driver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//span[@itemprop='name']")));

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
                        currTitle = TitleParseRegex().Replace(titleData[x].InnerText, "").Trim();                  
                        if(!titleData[x].InnerText.Contains("Imperfect") && MasterScrape.RemoveNonWordsRegex().Replace(currTitle, "").Contains(MasterScrape.RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase))
                        {
                            priceVal = Convert.ToDecimal(priceData[x].InnerText.Trim());

                            RightStufAnimeData.Add(
                                new EntryModel
                                (
                                    FormatRemovalRegex().Replace(currTitle.Replace("Volume", "Vol"), "").Replace("Deluxe Omnibus", "Deluxe").Trim(),
                                    $"${(memberStatus ? EntryModel.ApplyDiscount(priceVal, GOT_ANIME_DISCOUNT) : priceVal)}",
                                    stockStatusData[x].InnerText switch
                                    {
                                        string curStatus when curStatus.Contains("In Stock") => "IS",
                                        string curStatus when curStatus.Contains("Out of Stock") => "OOS",
                                        string curStatus when curStatus.Contains("Pre-Order") => "PO",
                                        _ => "Error",
                                    },
                                    WEBSITE_TITLE
                                )
                            );
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
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is WebDriverTimeoutException || ex is NoSuchElementException)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ RightStufAnime\n{ex}");
            }

            RightStufAnimeData.Sort(new VolumeSort(bookTitle));

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\RightStufAnimeData.txt"))
                {
                    if (RightStufAnimeData.Count != 0)
                    {
                        foreach (EntryModel data in RightStufAnimeData)
                        {
                            Logger.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        Logger.Debug(bookTitle + " Does Not Exist at RightStufAnime");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at RightStufAnime");
                    }
                } 
            }

            return RightStufAnimeData;
        }
    }
}