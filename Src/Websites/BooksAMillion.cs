namespace MangaLightNovelWebScrape.Websites
{
    public partial class BooksAMillion
    {
        public static List<string> BooksAMillionLinks = new();
        public static List<EntryModel> BooksAMillionData = new();
        public const string WEBSITE_TITLE = "Books-A-Million";
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("BooksAMillionLogs");
        private static bool boxsetCheck = false, boxsetValidation = false;
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        [GeneratedRegex("Vol\\.|Volume")] private static partial Regex ParseTitleVolRegex();
        [GeneratedRegex("(?<=\\d{1,3})[^\\d{1,3}.]+.*")] private static partial Regex FilterNoDigitRegex();
        [GeneratedRegex("(?<=\\d{1,3}.$)[^\\d{1,3}]+.*")] private static partial Regex FilterWithDigitRegex();
        
        private static string FilterBookTitle(string bookTitle){
            char[] trimedChars = {' ', '\'', '!', '-'};
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), $"%{Convert.ToByte(var).ToString("x2")}");
            }
            return bookTitle;
        }

        public static void ClearData()
        {
            BooksAMillionLinks.Clear();
            BooksAMillionData.Clear();
        }

        //Manga
        // https://booksamillion.com/search?query=world%20trigger;filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=1
        // https://booksamillion.com/search?query=world%20trigger%20manga;filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=1

        //https://booksamillion.com/search?query=07-Ghost&filter=product_type%3Abooks%7Cbook_categories%3ACGN&sort=date
        //https://booksamillion.com/search?query=07-Ghost&filter=product_type%3Abooks%7Cbook_categories%3ACGN%7Cseries%3A000379727&sort=date
        // https://booksamillion.com/search?query=one%20piece&filter=product_type%3Abooks%7Cseries%3A000277494%7Clanguage%3AENG
        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url;
            if (!boxsetCheck)
            {
                url = $"https://booksamillion.com/search?query={FilterBookTitle(bookTitle)};filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page={currPageNum}%7Clanguage%3AENG";
            }
            else
            {
                url = $"https://booksamillion.com/search?query={FilterBookTitle(bookTitle)}%20box%20set&filter=product_type%3Abooks&page=1%7Clanguage%3AENG&sort=date";
            }
            Logger.Debug(url);
            BooksAMillionLinks.Add(url);
            return url;
        }

        public static string TitleParse(string bookTitle, char bookType, string inputTitle)
        {
            string parsedTitle;
            if (!inputTitle.Any(char.IsDigit))
            {
                parsedTitle = FilterNoDigitRegex().Replace(ParseTitleVolRegex().Replace(bookTitle.Replace(",", "").Replace("(Omnibus Edition)", "Omnibus"), "Vol"), "");
            }
            else
            {
                parsedTitle = FilterWithDigitRegex().Replace(ParseTitleVolRegex().Replace(bookTitle.Replace(",", "").Replace("(Omnibus Edition)", "Omnibus"), "Vol"), "");
            }

            if (!parsedTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Insert(MasterScrape.FindVolNumRegex().Match(parsedTitle).Index, "Vol ");
            }

            return parsedTitle.Trim();
        }

        public static List<EntryModel> GetBooksAMillionData(string bookTitle, char bookType, bool memberStatus, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(true);
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(30));

                string currTitle;
                decimal priceVal;
                HtmlDocument doc;
                HtmlNodeCollection titleData, priceData, stockStatusData;
                while(true)
                {
                    driver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='search-item-title']//a")));

                    // Initialize the html doc for crawling
                    doc = new HtmlDocument();
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    titleData = doc.DocumentNode.SelectNodes("//div[@class='search-item-title']//a");
                    priceData = doc.DocumentNode.SelectNodes("//span[@class='our-price']");
                    stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='availability_search_results']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//ul[@class='search-page-list']//a[@title='Next']");

                    for(int x = 0; x < titleData.Count; x++)
                    {
                        currTitle = TitleParse(titleData[x].InnerText, bookType, bookTitle);
                        if (currTitle.Contains("Box Set") && !boxsetValidation)
                        {
                            boxsetValidation = true;
                            Logger.Debug("Found Boxset");
                            continue;
                        }
                               
                        if (MasterScrape.RemoveNonWordsRegex().Replace(currTitle, "").Contains(MasterScrape.RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase) && !currTitle.Contains("Guide") && currTitle.Any(char.IsDigit))
                        {
                            priceVal = Convert.ToDecimal(priceData[x].InnerText.Trim()[1..]);
                            BooksAMillionData.Add(
                                new EntryModel
                                (
                                    currTitle,
                                    $"${(memberStatus ? EntryModel.ApplyDiscount(priceVal, MEMBERSHIP_DISCOUNT) : priceData[x].InnerText)}".Trim(),
                                    stockStatusData[x].InnerText switch
                                    {
                                        string curStatus when curStatus.Contains("In Stock") => "IS",
                                        string curStatus when curStatus.Contains("Preorder") => "PO",
                                        _ => "OOS",
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                        //wait.Until(driver => driver.FindElement(By.XPath("//ul[@class='search-page-list']//a[@title='Next']"))).Click();
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        if (boxsetValidation && !boxsetCheck)
                        {
                            boxsetCheck = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ BooksAMillion\n{ex}");
            }

            BooksAMillionData.Sort(new VolumeSort(bookTitle));

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\BooksAMillionData.txt"))
                {
                    if (BooksAMillionData.Count != 0)
                    {
                        foreach (EntryModel data in BooksAMillionData)
                        {
                            Logger.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        outputFile.WriteLine(bookTitle + " Does Not Exist at BooksAMillion");
                    }
                }
            }

            return BooksAMillionData;
        }
    }
}