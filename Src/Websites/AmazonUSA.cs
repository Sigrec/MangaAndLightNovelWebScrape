using System;
using OpenQA.Selenium.Firefox;
namespace MangaLightNovelWebScrape.Websites
{
    public partial class AmazonUSA
    {
        public static List<string> AmazonUSALinks = new List<string>();
        public static List<EntryModel> AmazonUSAData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Amazon USA";
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("AmazonUSALogs");
        private static readonly List<string> SeriesBypass = new List<string>(){ "Jujutsu Kaisen" };
        [GeneratedRegex("\\d+-\\d+-(\\d+)")] private static partial Regex OmnibusFixRegex();

        // Manga
        //https://www.amazon.com/s?k=world+trigger&i=stripbooks&rh=n%3A4367%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page=1&qid=1685551243&rnid=1294421011&ref=sr_pg_1
        //https://www.amazon.com/s?k=one+piece&i=stripbooks&rh=n%3A4367%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page=1&qid=1685551243&rnid=1294421011&ref=sr_pg_1
        //https://www.amazon.com/s?k=fruits+basket&i=stripbooks&rh=n%3A4366%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page=2&qid=1685551123&rnid=1294421011&ref=sr_pg_2
        internal static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            // string url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}&i=stripbooks&rh=n%3A7421474011%2Cp_n_condition-type%3A1294423011%2Cp_n_feature_nine_browse-bin%3A3291437011&s=date-desc-rank&dc&page={currPageNum}&qid=1678483439&rnid=3291435011&ref=sr_pg_{currPageNum}";
            string url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}&i=stripbooks&rh=n%3A4367%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page={currPageNum}&qid=1685551243&rnid=1294421011&ref=sr_pg_{currPageNum}";
            Logger.Debug(url);
            AmazonUSALinks.Add(url);
            return url;
        }
        
        public static void ClearData()
        {
            AmazonUSALinks.Clear();
            AmazonUSAData.Clear();
        }

        public static string TitleParse(string bookTitle, char bookType, string inputTitle)
        {
            if (inputTitle.Contains("one piece", StringComparison.OrdinalIgnoreCase) && bookTitle.Equals("One Piece Box Set: East Blue and Baroque Works, Volumes 1-23 (One Piece Box Sets)"))
            {
                return "One Piece Box Set 1";
            }

            string parsedTitle;

            Match omnibusFix = OmnibusFixRegex().Match(bookTitle);
            if (omnibusFix.Success)
            {
                parsedTitle = Regex.Replace(bookTitle, @":(?<=:).*", "");
                return parsedTitle.Insert(parsedTitle.Length, $" Omnibus Vol {int.Parse(omnibusFix.Groups[1].Value) / 3}");
            }

            if (!inputTitle.Any(char.IsDigit))
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

        public static List<EntryModel> GetAmazonUSAData(string bookTitle, char bookType, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(30));

                string currTitle;
                bool foundPaperback = false, foundHardcover = false;
                
                driver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                while (true)
                {
                    HardcoverRestart:
                    wait.Until(e => e.FindElement(By.XPath("//*[@id=\"p_n_feature_eighteen_browse-bin/7421484011\"]/span/a/span")));

                    if (!foundPaperback && driver.FindElements(By.XPath("//*[@id=\"p_n_feature_eighteen_browse-bin/7421484011\"]/span/a/span")).Count == 1)
                    {
                        Logger.Info("Clicking Paperback");
                        wait.Until(driver => driver.FindElement(By.XPath("//*[@id=\"p_n_feature_eighteen_browse-bin/7421484011\"]/span/a/span"))).Click();
                        foundPaperback = true;
                        Logger.Info(driver.Url);
                    }
                    wait.Until(d => d.FindElement(By.XPath("//div[@class='a-section a-spacing-none a-spacing-top-micro puis-price-instructions-style' or @class='a-section a-spacing-small puis-padding-left-small puis-padding-right-small']")));
                    //Thread.Sleep(5000);

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
                        // Logger.Debug(stockStatusData[x].InnerText);
                        if (stockStatusData[x].InnerText.Contains("Kindle") || stockStatusData[x].InnerText.Contains("Comics") || !stockStatusData[x].InnerText.Contains("Paperback $") && !stockStatusData[x].InnerText.Contains("Hardcover $"))
                        {
                            Logger.Debug($"Removing {titleData[x].InnerText} with no Price");
                            titleData.RemoveAt(x);
                            stockStatusData.RemoveAt(x);
                            x--;
                        }
                    }

                    Logger.Debug(titleData.Count + " | " + priceData.Count + " | " + stockStatusData.Count);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if (titleData[x].InnerText.Contains("Vol", StringComparison.OrdinalIgnoreCase) || titleData[x].InnerText.Contains("Volume", StringComparison.OrdinalIgnoreCase) || titleData[x].InnerText.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || Regex.Match(titleData[x].InnerText, @"\d+-\d+-\d+").Success || SeriesBypass.Any(titleData[x].InnerText.Contains))
                        {
                            currTitle = TitleParse(titleData[x].InnerText.Trim(), bookType, bookTitle);
                            if(MasterScrape.RemoveNonWordsRegex().Replace(currTitle, "").Contains(MasterScrape.RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase))
                            {
                                AmazonUSAData.Add(new EntryModel(currTitle, priceData[x].InnerText.Trim(), stockStatusData[x].InnerText.Contains("Pre-order") ? "PO" : "IS", WEBSITE_TITLE));
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

            AmazonUSAData.Sort(new VolumeSort(bookTitle));

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