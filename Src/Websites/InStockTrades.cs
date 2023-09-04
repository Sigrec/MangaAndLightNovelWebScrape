namespace MangaLightNovelWebScrape.Websites
{
    public partial class InStockTrades
    {
        public static List<string> inStockTradesLinks = new(); //List of links used to get data from InStockTrades
        private static List<EntryModel> inStockTradesDataList = new(); //List of all the series data from InStockTrades
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("InStockTradesLogs");

        [GeneratedRegex("GN |TP |(?<=\\d+.).*")]  private static partial Regex TitleRegex();
        [GeneratedRegex("Vol ")] private static partial Regex BoxSetTitleRegex();

        //https://www.instocktrades.com/search?term=world+trigger
        //https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
        private static string GetUrl(byte currPageNum, string bookTitle){
            string url = "https://www.instocktrades.com/search?pg=" + currPageNum +"&title=" + bookTitle.Replace(' ', '+') + "&publisher=&writer=&artist=&cover=&ps=true";
            inStockTradesLinks.Add(url);
            Logger.Debug(url);
            return url;
        }

        public static List<EntryModel> GetInStockTradesData(string bookTitle, byte currPageNum, char bookType, EdgeOptions edgeOptions)
        {
            EdgeDriver edgeDriver = new(edgeOptions);
            WebDriverWait wait = new(edgeDriver, TimeSpan.FromSeconds(30));

            try
            {
                while (true)
                {
                    edgeDriver.Navigate().GoToUrl(GetUrl(currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='title']/a")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(edgeDriver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='title']/a | //div[@class='damage']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='price']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@class='btn hotaction']");
                    
                    if (titleData != null)
                    {
                        string currTitle;
                        int volNumIndex;
                        List<int> entiresToRemove = new();
                        for (int x = 0; x < titleData.Count; x++)
                        {
                            currTitle = TitleRegex().Replace(titleData[x].InnerText.Replace("3In1", "Omnibus").Trim(), "");
                            if (titleData[x].InnerText.Equals("Damaged") || !currTitle.Any(char.IsDigit)) // Remove damaged volume entries
                            {
                                // Logger.Debug("Found Damaged ^ or Novel when Manga Search");
                                titleData.RemoveAt(x);
                                x--;
                                continue;
                            }
                            
                            if (currTitle.Contains("Box Set")) 
                            { 
                                currTitle = BoxSetTitleRegex().Replace(currTitle, "");
                            }

                            if (bookType == 'M' && currTitle.Contains("Novel"))
                            {
                                continue;
                            }

                            volNumIndex = currTitle.IndexOf("Vol") != -1 ? currTitle.IndexOf("Vol") + 4 : currTitle.IndexOf("Set") + 4;

                            inStockTradesDataList.Add(new EntryModel(!currTitle[volNumIndex].Equals('0') ? currTitle : currTitle.Remove(volNumIndex, 1), priceData[x].InnerText.Trim(), "IS", "InStockTrades"));
                            
                            Logger.Debug(inStockTradesDataList[x].ToString());
                        }

                        if (pageCheck != null)
                        {
                            currPageNum++;
                        }
                        else
                        {
                            edgeDriver.Quit();
                            inStockTradesDataList.Sort(new VolumeSort(bookTitle));
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
            catch (Exception e) when (e is WebDriverTimeoutException || e is NoSuchElementException)
            {
                Logger.Debug($"{bookTitle} Does Not Exist @ InStockTrades");
            }

            //Print data to a txt file
            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\InStockTradesData.txt"))
                {
                    if (inStockTradesDataList.Count != 0)
                    {
                        foreach (EntryModel data in inStockTradesDataList)
                        {
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        outputFile.WriteLine(bookTitle + " Does Not Exist @ InStockTrades");
                    }
                }
            }

            return inStockTradesDataList;
        }
    }
}