namespace MangaLightNovelWebScrape.Websites.America
{
    public partial class InStockTrades
    {
        public static List<string> InStockTradesLinks = new();
        public static List<EntryModel> InStockTradesData = new();
        public const string WEBSITE_TITLE = "InStockTrades";
        private static readonly Logger Logger = LogManager.GetLogger("InStockTradesLogs");
        private const Region WEBSITE_REGION = Region.America;

        [GeneratedRegex(" GN| TP| HC| Manga|(?<=Vol).*|(?<=Box Set).*")]  private static partial Regex TitleRegex();
        [GeneratedRegex("Vol (\\d+)|Box Set (\\d+)")] private static partial Regex VolNumberRegex();
        [GeneratedRegex("3In1 Ed|3In1")] private static partial Regex OmnibusRegex();


        //https://www.instocktrades.com/search?term=world+trigger
        //https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
        // https://www.instocktrades.com/search?title=overlord+novel&publisher=&writer=&artist=&cover=&ps=true
        private static string GetUrl(byte currPageNum, string bookTitle){
            string url = $"https://www.instocktrades.com/search?pg={currPageNum}&title={bookTitle.Replace(' ', '+')}&publisher=&writer=&artist=&cover=&ps=true";
            InStockTradesLinks.Add(url);
            Logger.Debug(url);
            return url;
        }

        public static string GetUrl()
        {
            return InStockTradesLinks.Count != 0 ? InStockTradesLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        public static void ClearData()
        {
            InStockTradesLinks.Clear();
            InStockTradesData.Clear();
        }

        private static string TitleParse(string bookTitle, string titleText, BookType bookType)
        {
            Group volGroup;
            StringBuilder curTitle = new StringBuilder(titleText);
            if (titleText.Contains("Box Set")) 
            { 
                curTitle.Replace("Vol ", "");
                volGroup = VolNumberRegex().Match(curTitle.ToString()).Groups[2];
            }
            else
            {
                if (bookTitle.Equals("Overlord", StringComparison.OrdinalIgnoreCase) && titleText.Contains(" Og "))
                {
                    curTitle.Replace("Og", "Oh");
                }

                if (titleText.EndsWith(" Sc") || titleText.Contains(" Sc "))
                {
                    curTitle.Remove(titleText.LastIndexOf(" Sc"), 3);
                }

                curTitle.Replace(" Ann", " Anniversary");
                curTitle.Replace("Light Novel", "Novel");
                curTitle.Replace("Deluxe Edition", "Deluxe");

                if (bookType == BookType.Manga && !titleText.Contains("Vol"))
                {
                    curTitle.Append(" Vol 1");
                }
                else if (bookType == BookType.LightNovel && !titleText.Contains("Novel"))
                {
                    curTitle.Append(" Novel");
                }
                volGroup = VolNumberRegex().Match(curTitle.ToString()).Groups[1];
            }
            return System.Net.WebUtility.HtmlDecode($"{TitleRegex().Replace(OmnibusRegex().Replace(curTitle.ToString(), "Omnibus"), "")} {volGroup.Value.TrimStart('0')}".Trim());
        }

        public static List<EntryModel> GetInStockTradesData(string bookTitle, BookType bookType, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);
            ushort maxPages = 0;
            bool oneShotCheck = false;
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));
                while (true)
                {
                    driver.Navigate().GoToUrl(GetUrl(currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("/html/body/div[2]")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("/html/body/div[2]/div/div[3]/div/div[2][not(div[@class='damage'])]/div/a");
                    oneShotCheck = !titleData.AsParallel().Any(title => title.InnerText.Contains("Vol") || title.InnerText.Contains("Box Set") || title.InnerText.Contains("Manga"));
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("/html/body/div[2]/div/div[3]/div/div[2][not(div[@class='damage'])]/div/div[1]/div[2]");
                    if (maxPages == 0)
                    {
                        HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("/html/body/div[2]/div/div[4]/span/input");
                        if (pageCheck != null)
                        {
                            maxPages = Convert.ToUInt16(pageCheck.GetAttributeValue("data-max", "Page Num Error"));
                        }
                    }

                    string titleText;
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        titleText = titleData[x].InnerText;
                        if (
                            !titleText.Contains("Artbook")
                            && !titleText.Contains("Character Bk")
                            && (   
                                (
                                    bookType == BookType.Manga 
                                    && ( // Ensure manga entry contains valid indentifier
                                            oneShotCheck
                                            || (
                                                    !oneShotCheck
                                                    && (
                                                            titleText.Contains("Vol") 
                                                            || titleText.Contains("Box Set") 
                                                            || titleText.Contains("Manga")
                                                        )
                                                )
                                        )
                                    && !titleText.Contains(" Novel", StringComparison.OrdinalIgnoreCase)
                                    && !( // Remove unintended volumes from specific series
                                            MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText.ToString(), "of Gluttony")
                                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText.ToString(), "Boruto")
                                        )
                                )
                                || 
                                ( // Ensure novel entry doesn't contain Manga & Contains Novel or doesn't contain "Vol" identifier
                                    bookType == BookType.LightNovel 
                                    && !titleText.Contains("Manga") 
                                    && (
                                            titleText.Contains(" Novel", StringComparison.OrdinalIgnoreCase) 
                                            || !titleText.Contains("Vol")
                                        )
                                )
                            )
                        )
                        {
                            Logger.Debug(titleText);
                            InStockTradesData.Add(
                                new EntryModel
                                (
                                    TitleParse(bookTitle, titleText, bookType),
                                    priceData[x].InnerText.Trim(), 
                                    StockStatus.IS, 
                                    WEBSITE_TITLE
                                )
                            );
                        }
                    }

                    if (maxPages != 0 && currPageNum != maxPages)
                    {
                        currPageNum++;
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        InStockTradesData.Sort(new VolumeSort());
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                driver.Close();
                driver.Quit();
                Logger.Debug($"{bookTitle} Does Not Exist @ InStockTrades \n{e}");
            }

            //Print data to a txt file
            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\InStockTradesData.txt"))
                {
                    if (InStockTradesData.Count != 0)
                    {
                        foreach (EntryModel data in InStockTradesData)
                        {
                            Logger.Debug(data);
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        Logger.Debug($"{bookTitle} Does Not Exist at InStockTrades");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist at InStockTrades");
                    }
                }
            }

            return InStockTradesData;
        }
    }
}