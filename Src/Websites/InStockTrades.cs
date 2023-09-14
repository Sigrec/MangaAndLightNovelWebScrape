namespace MangaLightNovelWebScrape.Websites
{
    public partial class InStockTrades
    {
        public static List<string> InStockTradesLinks = new();
        public static List<EntryModel> InStockTradesData = new();
        public const string WEBSITE_TITLE = "InStockTrades";
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("InStockTradesLogs");

        [GeneratedRegex("GN |TP |(?<=\\d+.).*")]  private static partial Regex TitleRegex();
        [GeneratedRegex("Vol ")] private static partial Regex BoxSetTitleRegex();

        //https://www.instocktrades.com/search?term=world+trigger
        //https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
        private static string GetUrl(byte currPageNum, string bookTitle){
            string url = $"https://www.instocktrades.com/search?pg={currPageNum}&title={bookTitle.Replace(' ', '+')}&publisher=&writer=&artist=&cover=&ps=true";
            InStockTradesLinks.Add(url);
            Logger.Debug(url);
            return url;
        }

        public static void ClearData()
        {
            InStockTradesLinks.Clear();
            InStockTradesData.Clear();
        }

        public static List<EntryModel> GetInStockTradesData(string bookTitle, byte currPageNum, char bookType)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);

            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));
                while (true)
                {
                    driver.Navigate().GoToUrl(GetUrl(currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='title']/a")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='title']/a | //div[@class='damage']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='price']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@class='btn hotaction']");
                    
                    if (titleData != null)
                    {
                        string currTitle;
                        int volNumIndex;
                        for (int x = 0; x < titleData.Count; x++)
                        {
                            currTitle = TitleRegex().Replace(titleData[x].InnerText.Replace("3In1", "Omnibus").Trim(), "");
                            if (titleData[x].InnerText.Equals("Damaged") || !currTitle.Any(char.IsDigit)) // Remove damaged volume entries
                            {
                                Logger.Debug("Found Damaged Manga or Novel when Searching for Manga");
                                titleData.RemoveAt(x);
                                x--;
                                continue;
                            }
                            else if (bookType == 'M' && currTitle.Contains("Novel"))
                            {
                                continue;
                            }
                            else if (currTitle.Contains("Box Set")) 
                            { 
                                currTitle = BoxSetTitleRegex().Replace(currTitle, "");
                            }

                            volNumIndex = currTitle.IndexOf("Vol") != -1 ? currTitle.IndexOf("Vol") + 4 : currTitle.IndexOf("Set") + 4;
                            InStockTradesData.Add(new EntryModel(!currTitle[volNumIndex].Equals('0') ? currTitle : currTitle.Remove(volNumIndex, 1), priceData[x].InnerText.Trim(), "IS", WEBSITE_TITLE));
                        }

                        if (pageCheck != null)
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
                    else
                    {
                        Logger.Debug(bookTitle + " Does Not Exist @ InStockTrades");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                driver.Close();
                driver.Quit();
                Logger.Debug($"{bookTitle} Does Not Exist @ InStockTrades -> {e}");
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
                        outputFile.WriteLine(bookTitle + " Does Not Exist @ InStockTrades");
                    }
                }
            }

            return InStockTradesData;
        }
    }
}