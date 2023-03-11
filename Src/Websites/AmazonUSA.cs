using System;
namespace MangaLightNovelWebScrape.Src.Websites
{
    public class AmazonUSA
    {
        public static List<string> AmazonUSALinks = new List<string>();
        private static List<string[]> AmazonUSADataList = new List<string[]>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("AmazonUSALogs");

        // Manga
        // https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011&s=date-desc-rank&dc&page=1qid=1678483439&rnid=3291435011&ref=sr_pg_1
        // https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011&s=date-desc-rank&dc&page=2qid=1678483439&rnid=3291435011&ref=sr_pg_1
        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011&s=date-desc-rank&dc&page={currPageNum}&qid=1678483439&rnid=3291435011&ref=sr_pg_{currPageNum}";
            Logger.Debug(url);
            AmazonUSALinks.Add(url);
            return url;
        }

        public static string TitleParse(string bookTitle, char bookType, string inputTitle)
        {
            if (inputTitle.Contains("one piece", StringComparison.OrdinalIgnoreCase) && bookTitle.Equals("One Piece Box Set: East Blue and Baroque Works, Volumes 1-23 (One Piece Box Sets)"))
            {
                return "One Piece Box Set 1";
            }

            string parsedTitle;

            Match omnibusFix = Regex.Match(bookTitle, @"\d+-\d+-(\d+)");
            if (omnibusFix.Success)
            {
                parsedTitle = Regex.Replace(bookTitle, @":(?<=:).*", "");
                return parsedTitle.Insert(parsedTitle.Length, $" Omnibus Vol {Int32.Parse(omnibusFix.Groups[1].Value) / 3}");
            }

            parsedTitle = Regex.Replace(bookTitle.Replace("Vol.", "Vol").Replace("(Omnibus Edition)", "Omnibus"), @"(?<=\d{1,3})[^\d{1,3}.]+.*|\,|Manga ", "");
            if (!parsedTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Insert(Regex.Match(parsedTitle, @"\d{1,3}").Index, "Vol ");
            }
            return parsedTitle.Trim();
        }

        public static List<string[]> GetAmazonUSAData(string bookTitle, char bookType, byte currPageNum, EdgeOptions edgeOptions)
        {
            EdgeDriver edgeDriver = new EdgeDriver(Path.GetFullPath(@"DriverExecutables/Edge"), edgeOptions);
            WebDriverWait wait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(30));

            string stockStatus, currTitle;
            Regex removeWords = new Regex(@"[^\w+]");

            try
            {
                while (true)
                {
                    edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//span[@class='a-size-medium a-color-base a-text-normal']")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(edgeDriver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='a-section a-spacing-none a-spacing-top-micro s-price-instructions-style']//a[@class='a-size-base a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal' or @class='a-size-mini a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal']//span[@class='a-price']//span[@class='a-offscreen']/ancestor::div[@class='s-card-container s-overflow-hidden aok-relative puis-include-content-margin puis s-latency-cf-section s-card-border']//span[@class='a-size-medium a-color-base a-text-normal']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='a-section a-spacing-none a-spacing-top-micro s-price-instructions-style']//a[@class='a-size-base a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal' or @class='a-size-mini a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal']//span[@class='a-price']//span[@class='a-offscreen']");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[contains(@class, 'a-section a-spacing-none a-spacing-top-mini')]//span[contains(@aria-label, 'This title') or contains(text(), 'Available')]"); // Issue with One Piece
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@class='s-pagination-item s-pagination-next s-pagination-button s-pagination-separator']");

                    Logger.Debug(titleData.Count + " | " + priceData.Count + " | " + stockStatusData.Count);
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if (titleData[x].InnerText.Contains("Vol", StringComparison.OrdinalIgnoreCase) || titleData[x].InnerText.Contains("Volume", StringComparison.OrdinalIgnoreCase) || titleData[x].InnerText.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
                        {
                            currTitle = TitleParse(titleData[x].InnerText.Trim(), bookType, bookTitle);
                            if(removeWords.Replace(currTitle, "").Contains(removeWords.Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase))
                            {
                                stockStatus = stockStatusData[x].InnerText;
                                if (stockStatus.Contains("Available"))
                                {
                                    stockStatus = "IS";
                                }
                                else if (stockStatus.Contains("This title will be released on"))
                                {
                                    stockStatus = "PO";
                                }
                                else{
                                    stockStatus = "OOS";
                                }

                                AmazonUSADataList.Add(new string[]{currTitle, priceData[x].InnerText.Trim(), stockStatus, "Amazon USA"});

                                Logger.Debug("[" + AmazonUSADataList[x][0] + ", " + AmazonUSADataList[x][1] + ", " + AmazonUSADataList[x][2] + ", " + AmazonUSADataList[x][3] + "]");
                            }
                        }
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                        Logger.Debug("Next Page");
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
                Logger.Error($"{bookTitle} Does Not Exist @ AmazonUSA\n{ex}");
            }

            AmazonUSADataList.Sort(new VolumeSort(bookTitle));

            using (StreamWriter outputFile = new StreamWriter(@"Data\AmazonUSAData.txt"))
            {
                if (AmazonUSADataList.Count != 0)
                {
                    foreach (string[] data in AmazonUSADataList)
                    {
                        // Logger.Debug("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else
                {
                    outputFile.WriteLine(bookTitle + " Does Not Exist at AmazonUSA");
                }
            } 

            return AmazonUSADataList;
        }
    }
}