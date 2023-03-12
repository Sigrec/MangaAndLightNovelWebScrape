namespace MangaLightNovelWebScrape.Src.Websites
{
    class RightStufAnime
    {
        public static List<string> rightStufAnimeLinks = new List<string>();
        private static List<string[]> rightStufAnimeDataList = new List<string[]>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("RightStufAnimeLogs");

        private static string FilterBookTitle(string bookTitle){
            char[] trimedChars = {' ', '\'', '!', '-'};
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), "%" + Convert.ToByte(var).ToString("x2").ToString());
            }
            return bookTitle;
        }

        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url = "https://www.rightstufanime.com/category/" + (bookType == 'M' ? "Manga" : "Novels") + "?page=" + currPageNum + "&show=96&keywords=" + FilterBookTitle(bookTitle);
            Logger.Debug(url);
            rightStufAnimeLinks.Add(url);
            return url;
        }

        public static List<string[]> GetRightStufAnimeData(string bookTitle, char bookType, bool memberStatus, byte currPageNum, EdgeOptions edgeOptions)
        {
            EdgeDriver edgeDriver = new EdgeDriver(Path.GetFullPath(@"DriverExecutables/Edge"), edgeOptions);
            WebDriverWait wait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(30));

            double GotAnimeDiscount = 0.05;
            decimal priceVal;
            string priceTxt, stockStatus, currTitle;
            Regex removeWords = new Regex(@"[^\w+]");

            try
            {
                while (true)
                {
                    edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//span[@itemprop='name']")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(edgeDriver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//span[@itemprop='name']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//span[@itemprop='price']");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='product-line-stock-container '] | //span[@class='product-line-stock-msg-out-text']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//li[@class='global-views-pagination-next']");

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        currTitle = Regex.Replace(titleData[x].InnerText, @"\(.*?\)", "").Trim();                  
                        if(!titleData[x].InnerText.Contains("Imperfect") && removeWords.Replace(currTitle, "").Contains(removeWords.Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase))
                        {
                            priceVal = System.Convert.ToDecimal(priceData[x].InnerText.Substring(1));
                            priceTxt = memberStatus ? "$" + (priceVal - (priceVal * (decimal)GotAnimeDiscount)).ToString("0.00") : priceData[x].InnerText;

                            stockStatus = stockStatusData[x].InnerText;
                            if (stockStatus.IndexOf("In Stock") != -1)
                            {
                                stockStatus = "IS";
                            }
                            else if (stockStatus.IndexOf("Out of Stock") != -1)
                            {
                                stockStatus = "OOS";
                            }
                            else if (stockStatus.IndexOf("Pre-Order") != -1)
                            {
                                stockStatus = "PO";
                            }
                            else
                            {
                                stockStatus = "OOP";
                            }

                            rightStufAnimeDataList.Add(new string[]{Regex.Replace(currTitle.Replace("Volume", "Vol"), @" Manga| Edition", "").Trim(), priceTxt.Trim(), stockStatus.Trim(), "RightStufAnime"});
                        }
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                    }
                    else
                    {
                        edgeDriver.Quit();
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is WebDriverTimeoutException || ex is NoSuchElementException)
            {
                Logger.Error($"{bookTitle} Does Not Exist @ BooksAMillion\n{ex}");
            }

            rightStufAnimeDataList.Sort(new VolumeSort(bookTitle));

            using (StreamWriter outputFile = new StreamWriter(@"Data\RightStufAnimeData.txt"))
            {
                if (rightStufAnimeDataList.Count != 0)
                {
                    foreach (string[] data in rightStufAnimeDataList)
                    {
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else
                {
                    outputFile.WriteLine(bookTitle + " Does Not Exist at RightStufAnime");
                }
            } 

            return rightStufAnimeDataList;
        }
    }
}