using System.Text;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class RightStufAnime
    {
        private static List<string> RightStufAnimeLinks = new();
        private static List<EntryModel> RightStufAnimeData = new();
        public const string WEBSITE_TITLE = "RightStufAnime";
        private const decimal GOT_ANIME_DISCOUNT = 0.1M;
        private static readonly Logger Logger = LogManager.GetLogger("RightStufAnimeLogs");
        [GeneratedRegex("\\(.*?\\)")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(" Manga|,|:")] private static partial Regex FormatRemovalRegex();
        [GeneratedRegex("3 [iI]n 1|2 [iI]n 1")] private static partial Regex OmnibusRegex();

        private static string FilterBookTitle(string bookTitle){
            char[] trimedChars = {' ', '\'', '!', '-'};
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), "%" + Convert.ToByte(var).ToString("x2"));
            }
            return bookTitle;
        }

        public static void ClearData()
        {
            RightStufAnimeLinks.Clear();
            RightStufAnimeData.Clear();
        }

        public static List<string> GetUrlLinks()
        {
            return RightStufAnimeLinks;
        }

        private static string GetUrl(Book book, byte currPageNum, string bookTitle){
            StringBuilder url = new StringBuilder("https://www.rightstufanime.com/category/");
            url.Append(book == Book.Manga ? "Manga" : "Novels").Append("?page=").Append(currPageNum).Append("&show=96&keywords=").Append(FilterBookTitle(bookTitle));
            Logger.Debug(url.ToString());
            RightStufAnimeLinks.Add(url.ToString());
            return url.ToString();
        }

        private static string TitleParse(string titleText, Book book)
        {
            StringBuilder curTitle = new StringBuilder(OmnibusRegex().Replace(FormatRemovalRegex().Replace(titleText, ""), "Omnibus"));
            curTitle.Replace("Volume", "Vol").ToString();

            if (book == Book.Manga)
            {
                curTitle.Replace("Omnibus Edition", "Omnibus");
                if (titleText.Contains("Deluxe"))
                {
                    curTitle.Replace("Omnibus ", "").Replace("Deluxe Edition", "Deluxe");
                }
            }
            else if (book == Book.LightNovel && !titleText.Contains("Novel"))
            {
                if (titleText.IndexOf("Vol") != -1)
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

        public static List<EntryModel> GetRightStufAnimeData(string bookTitle, Book book, bool memberStatus, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);

            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));
                decimal priceVal;
                string titleText;
                bool anotherPage = true;
                while (anotherPage)
                {
                    driver.Navigate().GoToUrl(GetUrl(book, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='shopping-layout-content']")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//span[@itemprop='name']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//span[@itemprop='price']");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='product-line-stock-container '] | //span[@class='product-line-stock-msg-out-text']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//li[@class='global-views-pagination-next']");

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

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        titleText = TitleParseRegex().Replace(titleData[x].InnerText, "").Trim();           
                        if(!titleText.Contains("Imperfect") && MasterScrape.TitleContainsBookTitle(bookTitle, titleText.ToString()) && !MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText.ToString(), "of Gluttony"))
                        {
                            priceVal = Convert.ToDecimal(priceData[x].InnerText.Trim());
                            RightStufAnimeData.Add(
                                new EntryModel
                                (
                                    TitleParse(titleText, book),
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
                }
                RightStufAnimeData.Sort(new VolumeSort());
            }
            catch (Exception ex)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ RightStufAnime {ex}");
            }

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