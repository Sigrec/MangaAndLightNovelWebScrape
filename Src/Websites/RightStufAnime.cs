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
            string url = $"https://www.rightstufanime.com/category/{(book == Book.Manga ? "Manga" : "Novels")}?page={currPageNum}&show=96&keywords={FilterBookTitle(bookTitle)}";
            Logger.Debug(url);
            RightStufAnimeLinks.Add(url);
            return url;
        }

        private static string TitleParse(string curTitle, Book book)
        {
            curTitle = OmnibusRegex().Replace(FormatRemovalRegex().Replace(curTitle.Replace("Volume", "Vol"), ""), "Omnibus").Trim();

            if (book == Book.Manga)
            {
                if (curTitle.Contains("Omnibus Edition"))
                {
                    curTitle = curTitle.Replace("Omnibus Edition", "Omnibus");
                }
                else if (curTitle.Contains("Deluxe"))
                {
                    curTitle = curTitle.Replace("Omnibus ", "").Replace("Deluxe Edition", "Deluxe");
                }
            }
            else if (book == Book.LightNovel && !curTitle.Contains("Novel"))
            {
                if (curTitle.IndexOf("Vol") != -1)
                {
                    curTitle = curTitle.Insert(curTitle.IndexOf("Vol"), "Novel ");
                }
                else
                {
                    curTitle = curTitle.Insert(curTitle.Length, " Novel");
                }
            }
            return curTitle;
        }

        public static List<EntryModel> GetRightStufAnimeData(string bookTitle, Book book, bool memberStatus, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);

            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));
                decimal priceVal;
                string curTitle;
                bool anotherPage = true;
                while (anotherPage)
                {
                    driver.Navigate().GoToUrl(GetUrl(book, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//span[@itemprop='name']")));

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
                        curTitle = TitleParseRegex().Replace(titleData[x].InnerText, "").Trim();           
                        if(!curTitle.Contains("Imperfect") && MasterScrape.TitleContainsBookTitle(bookTitle, curTitle.ToString()) && !MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", curTitle.ToString(), "of Gluttony"))
                        {
                            priceVal = Convert.ToDecimal(priceData[x].InnerText.Trim());
                            RightStufAnimeData.Add(
                                new EntryModel
                                (
                                    TitleParse(curTitle, book),
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