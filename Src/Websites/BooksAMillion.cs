namespace MangaLightNovelWebScrape.Src.Websites
{
    public class BooksAMillion
    {
        public static List<string> BooksAMillionLinks = new List<string>();
        private static List<string[]> BooksAMillionDataList = new List<string[]>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("BooksAMillionLogs");
        private static bool boxsetCheck = false, boxsetValidation = false;
        private static string FilterBookTitle(string bookTitle){
            char[] trimedChars = {' ', '\'', '!', '-'};
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), "%" + Convert.ToByte(var).ToString("x2").ToString());
            }
            return bookTitle;
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
            string filterRegex;
            if (!inputTitle.Any(Char.IsDigit))
            {
                filterRegex = @"(?<=\d{1,3})[^\d{1,3}.]+.*";
            }
            else
            {
                filterRegex = @"(?<=\d{1,3}.$)[^\d{1,3}]+.*";
            }
            return Regex.Replace(Regex.Replace(bookTitle.Replace(",", "").Replace("(Omnibus Edition)", "Omnibus"), @"Vol\.|Volume", "Vol"), filterRegex, "").Trim();
        }

        public static List<string[]> GetBooksAMillionData(string bookTitle, char bookType, bool memberStatus, byte currPageNum, EdgeOptions edgeOptions)
        {
            WebDriver dummyDriver = new EdgeDriver(@"DriverExecutables/Edge", edgeOptions);
            edgeOptions.AddArgument("user-agent=" + dummyDriver.ExecuteScript("return navigator.userAgent").ToString().Replace("Headless", ""));
            dummyDriver.Quit();

            EdgeDriver edgeDriver = new EdgeDriver(Path.GetFullPath(@"DriverExecutables/Edge"), edgeOptions);
            WebDriverWait wait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(5));

            string stockStatus, priceTxt, currTitle;
            decimal priceVal, discount = 0.1M;
            HtmlDocument doc;
            HtmlNodeCollection titleData, priceData, stockStatusData;
            Regex removeWords = new Regex(@"[^\w+]");

            try
            {
                // edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                while(true)
                {
                    edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='search-item-title']//a")));

                    // Initialize the html doc for crawling
                    doc = new HtmlDocument();
                    doc.LoadHtml(edgeDriver.PageSource);

                    // Get the page data from the HTML doc
                    titleData = doc.DocumentNode.SelectNodes("//div[@class='search-item-title']//a");
                    priceData = doc.DocumentNode.SelectNodes("//span[@class='our-price']");
                    stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='availability_search_results']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//ul[@class='search-page-list']//a[@title='Next']");

                    for(int x = 0; x < titleData.Count; x++)
                    {
                        currTitle = TitleParse(titleData[x].InnerText, bookType, bookTitle);
                        // Logger.Debug(removeWords.Replace(currTitle, "") + " | " + removeWords.Replace(bookTitle, "") + " | " + (removeWords.Replace(currTitle, "").Contains(removeWords.Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase) && currTitle.Any(Char.IsDigit)));
                        if (currTitle.Contains("Box Set") && !boxsetValidation)
                        {
                            boxsetValidation = true;
                            Logger.Debug("Found Boxset");
                            continue;
                        }
                        
                        if (removeWords.Replace(currTitle, "").Contains(removeWords.Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase) && currTitle.Any(Char.IsDigit))
                        {
                            stockStatus = stockStatusData[x].InnerText;
                            if (stockStatus.Contains("In Stock"))
                            {
                                stockStatus = "IS";
                            }
                            else if (stockStatus.Contains("Preorder"))
                            {
                                stockStatus ="PO";
                            }
                            else
                            {
                                stockStatus = "OOS";
                            }

                            if (currTitle.Contains("4"))
                            {
                                BooksAMillionDataList.Add(new string[]{currTitle, "$1.00", stockStatus, "BooksAMillion"});
                                continue;
                            }

                            priceVal = System.Convert.ToDecimal(priceData[x].InnerText.Trim().Substring(1));
                            priceTxt = memberStatus ? "$" + (priceVal - (priceVal * discount)).ToString("0.00") : priceData[x].InnerText;

                            BooksAMillionDataList.Add(new string[]{currTitle, priceTxt.Trim(), stockStatus, "BooksAMillion"});
                        }
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                        //wait.Until(driver => driver.FindElement(By.XPath("//ul[@class='search-page-list']//a[@title='Next']"))).Click();
                    }
                    else
                    {
                        //edgeDriver.Quit();
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
            catch (Exception ex) when (ex is WebDriverTimeoutException || ex is NoSuchElementException)
            {
                Logger.Error($"{bookTitle} Does Not Exist @ BooksAMillion\n{ex}");
            }

            BooksAMillionDataList.Sort(new VolumeSort(bookTitle));

            using (StreamWriter outputFile = new StreamWriter(@"Data\BooksAMillionData.txt"))
            {
                if (BooksAMillionDataList.Count != 0)
                {
                    foreach (string[] data in BooksAMillionDataList)
                    {
                        Logger.Debug("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else
                {
                    outputFile.WriteLine(bookTitle + " Does Not Exist at BooksAMillion");
                }
            } 

            return BooksAMillionDataList;
        }
    }
}