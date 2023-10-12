namespace MangaLightNovelWebScrape.Websites.Japan
{
    public partial class CDJapan
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        public const Region WEBSITE_REGION = Region.Japan;
        public const string WEBSITE_TITLE = "CDJapan";
        private List<string> CDJapanLinks = new List<string>();
        private List<EntryModel> CDJapanData = new List<EntryModel>();

        [GeneratedRegex(@"\(.*?\)|\[.*?\]")] private static partial Regex TitleParseRegex();

        internal async Task CreateCDJapanTask(string bookTitle, BookType book, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetCDJapanData(bookTitle, book, driver));
            });
        }
        
        internal void ClearData()
        {
            if (this != null)
            {
                CDJapanLinks.Clear();
                CDJapanData.Clear();
            }
        }

        internal string GetUrl()
        {
            return CDJapanLinks.Count != 0 ? CDJapanLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private  string GetUrl(string bookTitle, BookType bookType)
        {
            // Light Novel
            // https://www.cdjapan.co.jp/searchuni?term.media_format=BOOK&q=classroom+of+the+elite+novel&opt.exclude_eoa=on&opt.exclude_prx=on
            // https://www.cdjapan.co.jp/searchuni?fq.category=UD%3A11&term.media_format=BOOK&q=classroom+of+the+elite&opt.exclude_eoa=on&opt.exclude_prx=on

            // Manga
            // https://www.cdjapan.co.jp/searchuni?fq.category=UD%3A14&term.media_format=BOOK&q=classroom+of+the+elite&opt.exclude_eoa=on&opt.exclude_prx=on
            // https://www.cdjapan.co.jp/searchuni?fq.category=UD%3A14&term.media_format=BOOK&q=world+trigger&opt.exclude_eoa=on&opt.exclude_prx=on
            string url = $"https://www.cdjapan.co.jp/searchuni?fq.category={(bookType == BookType.Manga ? "UD%3A14" : "UD%3A11")}&term.media_format=BOOK&q={bookTitle}&opt.exclude_eoa=on&opt.exclude_prx=on";
            CDJapanLinks.Add(url);
            LOGGER.Info(url);
            return url;
        }

        private static string TitleParse(string bookTitle, BookType bookType)
        {
            bookTitle = TitleParseRegex().Replace(bookTitle, "");
            return bookTitle;
        }

        internal List<EntryModel> GetCDJapanData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                driver.Navigate().GoToUrl(GetUrl(bookTitle, bookType));

                // Initialize the html doc for crawling
                HtmlDocument doc = new();
                doc.LoadHtml(driver.PageSource);

                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//ul[@id='js-search-result']//div[@class='title']//span[@class='title-text']");
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//ul[@id='js-search-result']/li/a/div[@class='item-body']/div/div/span[@itemprop='price']");
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//ul[@id='js-search-result']/li/a/div[@class='item-body']/div/ul/li[2]/span/text()");

                // In stock
                // In Stock at Supplier:Usually ships in 2-4 days
                // Backorder:Usually ships in 1-3 weeks
                for (int x = 0; x < titleData.Count; x++)
                {
                    string titleText = titleData[x].InnerText;
                    if (MasterScrape.TitleContainsBookTitle(bookTitle, titleText) && !titleText.Contains("Manga Set"))
                    {
                        CDJapanData.Add(
                            new EntryModel
                            (
                                TitleParse(titleText, bookType),
                                priceData[x].InnerText.Replace("yen", "¥").Trim(),
                                stockStatusData[x].InnerText.Trim() switch
                                {
                                    string curStatus when curStatus.Contains("In Stock", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                    string curStatus when curStatus.Contains("Out of Stock") => StockStatus.OOS,
                                    string curStatus when curStatus.Contains("Pre-Order") => StockStatus.PO,
                                    _ => StockStatus.NA,
                                },
                                WEBSITE_TITLE
                            )
                        );
                    }
                }
            }
            catch (Exception e)
            {
                driver.Close();
                driver.Quit();
                LOGGER.Error($"{bookTitle} Does Not Exist @ Crunchyroll \n{e}");
            }
            return CDJapanData;
        }
    }
}