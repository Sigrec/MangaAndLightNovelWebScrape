namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class AmazonUSA
    {
        private List<string> AmazonUSALinks = new List<string>();
        private List<EntryModel> AmazonUSAData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Amazon USA";
        private const Region WEBSITE_REGION = Region.America;
        private static readonly Logger Logger = LogManager.GetLogger("AmazonUSALogs");
        private static readonly List<string> SeriesBypass = new List<string>(){ "Jujutsu Kaisen" };
        
        [GeneratedRegex("\\d+-\\d+-(\\d+)")] private static partial Regex OmnibusFixRegex();
        [GeneratedRegex("\\d+-\\d+-\\d+")] private static partial Regex VolNumMatchRegex();
        [GeneratedRegex("(?<=\\d{1,3})[^\\d{1,3}.]+.*|\\,|Manga ")] private static partial Regex ParsedTitleNoDigitRegex();
        [GeneratedRegex("(?<=\\d{1,3}.$)[^\\d{1,3}]+.*|\\,|Manga ")] private static partial Regex ParsedTitleWithDigitRegex();
        [GeneratedRegex(":(?<=:).*")]private static partial Regex OmnibusParsedTitleRegex(); 

        protected internal async Task CreateAmazonUSATask(string bookTitle, BookType book, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetAmazonUSAData(bookTitle, book, 1, driver));
            });
        }

        // Manga
        //https://www.amazon.com/s?k=world+trigger&i=stripbooks&rh=n%3A4367%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page=1&qid=1685551243&rnid=1294421011&ref=sr_pg_1
        //https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A4367%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page=1&qid=1685551243&rnid=1294421011&ref=sr_pg_1
        private string GetUrl(BookType bookType, byte currPageNum, string bookTitle)
        {
            // string url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011&s=date-desc-rank&dc&page={currPageNum}&qid=1678483439&rnid=3291435011&ref=sr_pg_{currPageNum}";
            string url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}&i=stripbooks&rh=n%3A4367%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page={currPageNum}&qid=1685551243&rnid=1294421011&ref=sr_pg_{currPageNum}";
            Logger.Debug(url);
            AmazonUSALinks.Add(url);
            return url;
        }

        protected internal string GetUrl()
        {
            return AmazonUSALinks.Count != 0 ? AmazonUSALinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }
        
        protected internal void ClearData()
        {
            AmazonUSALinks.Clear();
            AmazonUSAData.Clear();
        }

        private static string TitleParse(string bookTitle, BookType bookType, string inputTitle)
        {
            if (inputTitle.Contains("one piece", StringComparison.OrdinalIgnoreCase) && bookTitle.Equals("One Piece Box Set: East Blue and Baroque Works, Volumes 1-23 (One Piece Box Sets)"))
            {
                return "One Piece Box Set 1";
            }

            string parsedTitle;

            Match omnibusFix = OmnibusFixRegex().Match(bookTitle);
            if (omnibusFix.Success)
            {
                parsedTitle = OmnibusParsedTitleRegex().Replace(bookTitle, "");
                return parsedTitle.Insert(parsedTitle.Length, $" Omnibus Vol {Math.Ceiling(decimal.Parse(omnibusFix.Groups[1].Value) / 3)}");
            }
  
            if (!inputTitle.Any(char.IsDigit))
            {
                parsedTitle = ParsedTitleNoDigitRegex().Replace(bookTitle.Replace("Vol.", "Vol").Replace("(Omnibus Edition)", "Omnibus"), "");
            }
            else
            {
                parsedTitle = ParsedTitleWithDigitRegex().Replace(bookTitle.Replace("Vol.", "Vol").Replace("(Omnibus Edition)", "Omnibus"), "");
            }

            if (!parsedTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Insert(MasterScrape.FindVolNumRegex().Match(parsedTitle).Index, "Vol ");
            }
            return parsedTitle.Trim();
        }

        private static void CheckForBookSeriesButton(WebDriver driver, WebDriverWait wait, bool foundBookSeries, string bookTitle)
        {
            var bookSeriesElement = driver.FindElements(By.XPath("//*[@id='brandsRefinements']/ul//li/span/a/span"));
            if (!foundBookSeries && bookSeriesElement.Count == 1 && bookSeriesElement[0].Text.Contains(bookTitle, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info("Clicking Book Series");
                wait.Until(driver => driver.FindElement(By.XPath("//*[@id='brandsRefinements']/ul//li/span/a/span"))).Click();
                foundBookSeries = true;
                Logger.Info(driver.Url);
            }
        }

        private List<EntryModel> GetAmazonUSAData(string bookTitle, BookType bookType, byte currPageNum, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));

                string currTitle;
                bool foundPaperback = false, foundHardcover = false, foundBookSeries = false;
                
                driver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                while (true)
                {
                    HardcoverRestart:
                    wait.Until(e => e.FindElement(By.XPath("//*[@id='p_n_feature_eighteen_browse-bin/7421484011']/span/a/span")));

                    CheckForBookSeriesButton(driver, wait, foundBookSeries, bookTitle);
                    var paperBackElement = driver.FindElements(By.XPath("//*[@id='p_n_feature_eighteen_browse-bin/7421484011']/span/a/span"));
                    if (!foundPaperback && paperBackElement.Count == 1)
                    {
                        Logger.Info("Clicking Paperback");
                        wait.Until(driver => paperBackElement[0]).Click();
                        foundPaperback = true;
                        Logger.Info(driver.Url);
                        CheckForBookSeriesButton(driver, wait, foundBookSeries, bookTitle);
                    }

                    wait.Until(d => d.FindElement(By.XPath("//div[@class='a-section a-spacing-none a-spacing-top-micro puis-price-instructions-style' or @class='a-section a-spacing-small puis-padding-left-small puis-padding-right-small']")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='a-section a-spacing-none puis-padding-right-small s-title-instructions-style']/h2//span | //span[@class='a-size-base-plus a-color-base a-text-normal']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='a-section a-spacing-none a-spacing-top-micro puis-price-instructions-style']//div[@class='a-row a-spacing-mini a-size-base a-color-base']//following-sibling::div[1]//span[@class='a-price']//span[@class='a-offscreen'] | //div[@class='a-section a-spacing-small puis-padding-left-small puis-padding-right-small']//a[@class='a-size-base a-link-normal s-no-hover s-underline-text s-underline-link-text s-link-style a-text-normal']//span[@class='a-price']//span[@class='a-offscreen']");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='a-section a-spacing-none a-spacing-top-micro puis-price-instructions-style' or @class='a-section a-spacing-small puis-padding-left-small puis-padding-right-small']"); // Issue with One Piece
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@class='s-pagination-item s-pagination-next s-pagination-button s-pagination-separator']");
                    Logger.Debug(titleData.Count + " | " + priceData.Count + " | " + stockStatusData.Count);

                    // for (int x = 0; x < stockStatusData.Count; x++)
                    // {
                    //     Logger.Debug(stockStatusData[x].InnerText);
                    //     if (!stockStatusData[x].InnerText.Contains("Kindle") && !stockStatusData[x].InnerText.Contains("Comics") && stockStatusData[x].InnerText.Contains("Paperback $"))
                    //     {
                    //         Logger.Debug($"[{titleData[x].InnerText}, {!stockStatusData[x].InnerText.Contains("Kindle") && !stockStatusData[x].InnerText.Contains("Comics") && stockStatusData[x].InnerText.Contains("Paperback $")}, {stockStatusData[x].InnerText}, AmazonUSA");
                    //     }
                    //     else
                    //     {
                    //         Logger.Debug($"Removing {titleData[x].InnerText} with no Price");
                    //         titleData.RemoveAt(x);
                    //         stockStatusData.RemoveAt(x);
                    //         x--;
                    //     }
                    // }

                    for (int x = 0; x < stockStatusData.Count; x++)
                    {
                        if (stockStatusData[x].InnerText.Contains("Kindle") || stockStatusData[x].InnerText.Contains("Comics") || !stockStatusData[x].InnerText.Contains("Paperback $") && !stockStatusData[x].InnerText.Contains("Hardcover $"))
                        {
                            Logger.Debug($"Removing {titleData[x].InnerText} with no Price");
                            titleData.RemoveAt(x);
                            stockStatusData.RemoveAt(x);
                            x--;
                        }
                    }

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if (titleData[x].InnerText.Contains("Vol", StringComparison.OrdinalIgnoreCase) || titleData[x].InnerText.Contains("Volume", StringComparison.OrdinalIgnoreCase) || titleData[x].InnerText.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || VolNumMatchRegex().Match(titleData[x].InnerText).Success || SeriesBypass.Any(titleData[x].InnerText.Contains))
                        {
                            currTitle = TitleParse(titleData[x].InnerText.Trim(), bookType, bookTitle);
                            if(InternalHelpers.RemoveNonWordsRegex().Replace(currTitle, "").Contains(InternalHelpers.RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase))
                            {
                                AmazonUSAData.Add(
                                    new EntryModel(
                                        currTitle, 
                                        priceData[x].InnerText.Trim(), 
                                        stockStatusData[x].InnerText.Contains("Pre-order") ? StockStatus.PO : StockStatus.IS, 
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                        }
                    }

                    if (pageCheck != null)
                    {
                        driver.FindElement(By.XPath("//a[@class='s-pagination-item s-pagination-next s-pagination-button s-pagination-separator']")).Click();
                        AmazonUSALinks.Add(driver.Url);
                        Logger.Debug($"Next Page = {driver.Url}");
                    }
                    else
                    {
                        // Check for hardcover Format before quitting
                        if (!foundHardcover && driver.FindElements(By.XPath("//div[@id='p_n_feature_eighteen_browse-bin-title']/following-sibling::ul//span[contains(text(), 'Hardcover')]")).Count == 1)
                        {
                            Logger.Debug("Clicking Hardcover");
                            wait.Until(driver => driver.FindElement(By.XPath("//div[@id='p_n_feature_eighteen_browse-bin-title']/following-sibling::ul//span[contains(text(), 'Hardcover')]"))).Click();
                            wait.Until(driver => driver.FindElement(By.XPath("//title[contains(text(), 'Hardcover')]")));
                            Logger.Debug($"Next Page = {driver.Url}");
                            AmazonUSALinks.Add(driver.Url);
                            foundHardcover = true;
                            goto HardcoverRestart;
                        } 
                        driver.Close();
                        driver.Quit();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ AmazonUSA {ex}");
            }

            AmazonUSAData.Sort(MasterScrape.VolumeSort);

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\AmazonUSAData.txt"))
                {
                    if (AmazonUSAData.Count != 0)
                    {
                        foreach (EntryModel data in AmazonUSAData)
                        {
                            Logger.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        Logger.Error($"{bookTitle} Does Not Exist @ AmazonUSA");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist @ AmazonUSA");
                    }
                } 
            }

            return AmazonUSAData;
        }
    }
}