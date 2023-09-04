namespace MangaLightNovelWebScrape.Websites
{
    partial class RightStufAnime
    {
        public static List<string> rightStufAnimeLinks = new();
        private static List<EntryModel> rightStufAnimeDataList = new();
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

        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url = "https://www.rightstufanime.com/category/" + (bookType == 'M' ? "Manga" : "Novels") + "?page=" + currPageNum + "&show=96&keywords=" + FilterBookTitle(bookTitle);
            Logger.Debug(url);
            rightStufAnimeLinks.Add(url);
            return url;
        }

        public static List<EntryModel> GetRightStufAnimeData(string bookTitle, char bookType, bool memberStatus, byte currPageNum, EdgeOptions edgeOptions)
        {
            WebDriver edgeDriver = new EdgeDriver(edgeOptions);
            WebDriverWait wait = new(edgeDriver, TimeSpan.FromSeconds(30));

            double GotAnimeDiscount = 0.05;
            decimal priceVal;
            string priceTxt, stockStatus, currTitle;

            try
            {
                while (true)
                {
                    edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//span[@itemprop='name']")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(edgeDriver.PageSource);

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
                            priceVal = Convert.ToDecimal(priceData[x].InnerText[1..]);
                            priceTxt = "$" + (memberStatus ? (priceVal - priceVal * (decimal)GotAnimeDiscount).ToString("0.00") : priceData[x].InnerText);

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

                            rightStufAnimeDataList.Add(new EntryModel(FormatRemovalRegex().Replace(currTitle.Replace("Volume", "Vol"), "").Replace("Deluxe Omnibus", "Deluxe").Trim(), priceTxt.Trim(), stockStatus.Trim(), "RightStufAnime"));
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
                Logger.Error($"{bookTitle} Does Not Exist @ RightStufAnime\n{ex}");
            }

            rightStufAnimeDataList.Sort(new VolumeSort(bookTitle));

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\RightStufAnimeData.txt"))
                {
                    if (rightStufAnimeDataList.Count != 0)
                    {
                        foreach (EntryModel data in rightStufAnimeDataList)
                        {
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        outputFile.WriteLine(bookTitle + " Does Not Exist at RightStufAnime");
                    }
                } 
            }

            return rightStufAnimeDataList;
        }
    }
}