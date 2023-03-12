using System;
namespace MangaLightNovelWebScrape.Src.Websites
{
    public class AmazonUSA
    {
        public static List<string> AmazonUSALinks = new List<string>();
        private static List<string[]> AmazonUSADataList = new List<string[]>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("AmazonUSALogs");

        private static readonly List<string> SeriesBypass = new List<string>(){ "Jujutsu Kaisen" };

        // Manga
        // https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011&s=date-desc-rank&dc&page=1qid=1678483439&rnid=3291435011&ref=sr_pg_1
        // https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011&s=date-desc-rank&dc&page=2qid=1678483439&rnid=3291435011&ref=sr_pg_1
        // https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_feature_eighteen_browse-bin%3A7421484011&s=date-desc-rank&dc&qid=1678640079&rnid=7421483011&ref=sr_nr_p_n_feature_eighteen_browse-bin_4&ds=v1%3A2Mz9FtKEbfnRYBqWWDBLiLP7Cqp2QIW3b2u6A53s4hw
        // https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011&s=date-desc-rank&dc&qid=1678483439&rnid=3291435011&ref=sr_pg_1
        // https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_feature_eighteen_browse-bin%3A7421484011&s=date-desc-rank&dc&page=5&qid=1678563476&rnid=7421483011&ref=sr_pg_5
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

            if (!inputTitle.Any(Char.IsDigit))
            {
                parsedTitle = Regex.Replace(bookTitle.Replace("Vol.", "Vol").Replace("(Omnibus Edition)", "Omnibus"), @"(?<=\d{1,3})[^\d{1,3}.]+.*|\,|Manga ", "");
            }
            else
            {
                parsedTitle = Regex.Replace(bookTitle.Replace("Vol.", "Vol").Replace("(Omnibus Edition)", "Omnibus"), @"(?<=\d{1,3}.$)[^\d{1,3}]+.*|\,|Manga ", "");
            }

            if (!parsedTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Insert(Regex.Match(parsedTitle, @"\d{1,3}").Index, "Vol ");
            }
            return parsedTitle.Trim();
        }

        public static List<string[]> GetAmazonUSAData(string bookTitle, char bookType, byte currPageNum, EdgeOptions edgeOptions)
        {
            EdgeDriver edgeDriver = new EdgeDriver(Path.GetFullPath(@"DriverExecutables/Edge"), edgeOptions);
            edgeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            WebDriverWait wait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(30));

            string stockStatus, currTitle;
            Regex removeWords = new Regex(@"[^\w+]");
            bool foundPaperback = false, foundHardcover = false;

            try
            {
                edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                while (true)
                {
                    HardcoverRestart:
                    wait.Until(e => e.FindElement(By.XPath("//span[@class='a-size-medium a-color-base a-text-normal']")));

                    if (!foundPaperback && edgeDriver.FindElements(By.XPath("//div[@id='p_n_feature_eighteen_browse-bin-title']/following-sibling::ul//span[contains(text(), 'Paperback')]")).Count == 1)
                    {
                        Logger.Debug("Clicking Paperback");
                        wait.Until(driver => driver.FindElement(By.XPath("//div[@id='p_n_feature_eighteen_browse-bin-title']/following-sibling::ul//span[contains(text(), 'Paperback')]"))).Click();
                        foundPaperback = true;
                        
                    }
                    Thread.Sleep(1000);

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(edgeDriver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='a-section a-spacing-none a-spacing-top-micro s-price-instructions-style']//a[@class='a-size-base a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal' or @class='a-size-mini a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal']//span[@class='a-price']//span[@class='a-offscreen']/ancestor::div[@class='s-card-container s-overflow-hidden aok-relative puis-include-content-margin puis s-latency-cf-section s-card-border']//span[@class='a-size-medium a-color-base a-text-normal']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='a-section a-spacing-none a-spacing-top-micro s-price-instructions-style']//a[@class='a-size-base a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal' or @class='a-size-mini a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal']//span[@class='a-price']//span[@class='a-offscreen']");// a-row a-spacing-mini
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='a-section a-spacing-none a-spacing-top-micro s-price-instructions-style']//a[@class='a-size-base a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal' or @class='a-size-mini a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal']//span[@class='a-price']//span[@class='a-offscreen']/ancestor::div[@class='sg-col sg-col-4-of-12 sg-col-4-of-16 sg-col-4-of-20 sg-col-4-of-24']//div[@class='a-section a-spacing-none a-spacing-top-micro s-price-instructions-style']"); // Issue with One Piece
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@class='s-pagination-item s-pagination-next s-pagination-button s-pagination-separator']");

                    // Logger.Debug(titleData.Count + " | " + priceData.Count + " | " + stockStatusData.Count);
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if (titleData[x].InnerText.Contains("Vol", StringComparison.OrdinalIgnoreCase) || titleData[x].InnerText.Contains("Volume", StringComparison.OrdinalIgnoreCase) || titleData[x].InnerText.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || SeriesBypass.Any(titleData[x].InnerText.Contains))
                        {
                            currTitle = TitleParse(titleData[x].InnerText.Trim(), bookType, bookTitle);
                            if(removeWords.Replace(currTitle, "").Contains(removeWords.Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase))
                            {
                                if (stockStatusData[x].InnerText.Contains("Pre-order"))
                                {
                                    stockStatus = "PO";
                                }
                                else{
                                    stockStatus = "IS";
                                }

                                AmazonUSADataList.Add(new string[]{currTitle, priceData[x].InnerText.Trim(), stockStatus, "AmazonUSA"});
                            }
                        }
                    }

                    if (pageCheck != null)
                    {
                        edgeDriver.FindElement(By.XPath("//a[@class='s-pagination-item s-pagination-next s-pagination-button s-pagination-separator']")).Click();
                        AmazonUSALinks.Add(edgeDriver.Url);
                        Logger.Debug($"Next Page = {edgeDriver.Url}");
                    }
                    else
                    {
                        // Check for hardcover Format before quitting
                        if (!foundHardcover && edgeDriver.FindElements(By.XPath("//div[@id='p_n_feature_eighteen_browse-bin-title']/following-sibling::ul//span[contains(text(), 'Hardcover')]")).Count == 1)
                        {
                            Logger.Debug("Clicking Hardcover");
                            wait.Until(driver => driver.FindElement(By.XPath("//div[@id='p_n_feature_eighteen_browse-bin-title']/following-sibling::ul//span[contains(text(), 'Hardcover')]"))).Click();
                            wait.Until(driver => driver.FindElement(By.XPath("//title[contains(text(), 'Hardcover')]")));
                            Logger.Debug($"Next Page = {edgeDriver.Url}");
                            AmazonUSALinks.Add(edgeDriver.Url);
                            foundHardcover = true;
                            goto HardcoverRestart;
                        } 
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
                        Logger.Debug("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else
                {
                    Logger.Error($"{bookTitle} Does Not Exist @ AmazonUSA");
                    outputFile.WriteLine($"{bookTitle} Does Not Exist @ AmazonUSA");
                }
            } 

            return AmazonUSADataList;
        }
    }
}