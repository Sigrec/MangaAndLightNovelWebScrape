namespace MangaLightNovelWebScrape.Websites
{
    public class InStockTrades
    {
        public static List<string> inStockTradesLinks = new List<string>(); //List of links used to get data from InStockTrades
        private static List<string[]> inStockTradesDataList = new List<string[]>(); //List of all the series data from InStockTrades

        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("InStockTradesLogs");

        //https://www.instocktrades.com/search?term=world+trigger
        //https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
        private static string GetUrl(byte currPageNum, string bookTitle){
            string url = "https://www.instocktrades.com/search?pg=" + currPageNum +"&title=" + bookTitle.Replace(' ', '+') + "&publisher=&writer=&artist=&cover=&ps=true";
            inStockTradesLinks.Add(url);
            Logger.Debug(url);
            return url;
        }

        public static List<string[]> GetInStockTradesData(string bookTitle, byte currPageNum, char bookType, EdgeOptions edgeOptions)
        {
            EdgeDriver edgeDriver = new EdgeDriver(Path.GetFullPath(@"DriverExecutables/Edge"), edgeOptions);
            WebDriverWait wait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(5));

            while (true)
            {
                edgeDriver.Navigate().GoToUrl(GetUrl(currPageNum, bookTitle));
                wait.Until(e => e.FindElement(By.XPath("//div[@class='title']/a")));

                // Initialize the html doc for crawling
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(edgeDriver.PageSource);

                // Get the page data from the HTML doc
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='title']/a | //div[@class='damage']");
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='price']");
                HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@class='btn hotaction']");
                
                if (titleData != null)
                {
                    string currTitle;
                    int volNumIndex;
                    List<int> entiresToRemove = new List<int>();
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if (titleData[x].InnerText.Equals("Damaged")) // Remove damaged volume entries
                        {
                            //Logger.Debug("Found Damaged ^");
                            titleData.RemoveAt(x);
                            x--;
                            entiresToRemove.Add(x);
                            continue;
                        }
                        currTitle = Regex.Replace(titleData[x].InnerText.Replace("3In1", "Omnibus").Trim(), @"GN |TP |(?<=\d+.).*", "");
                        if (currTitle.Contains("Box Set")) 
                        { 
                            currTitle = Regex.Replace(currTitle, "Vol ", "");
                        }

                        if (bookType == 'M' && currTitle.Contains("Novel"))
                        {
                            continue;
                        }

                        volNumIndex = currTitle.IndexOf("Vol") != -1 ? currTitle.IndexOf("Vol") + 4 : currTitle.IndexOf("Set") + 4;
                        inStockTradesDataList.Add(new string[]{!currTitle[volNumIndex].Equals('0') ? currTitle : currTitle.Remove(volNumIndex, 1), priceData[x].InnerText.Trim(), "IS", "InStockTrades"});
                        // Logger.Debug("[" + inStockTradesDataList[x][0] + ", " + inStockTradesDataList[x][1] + ", " + inStockTradesDataList[x][2] + ", " + inStockTradesDataList[x][3] + "]");
                    }

                    if (pageCheck != null){
                        currPageNum++;
                    }
                    else{
                        edgeDriver.Quit();
                        entiresToRemove.ForEach(index => inStockTradesDataList.RemoveAt(index));
                        inStockTradesDataList.Sort(new VolumeSort(bookTitle));
                        break;
                    }
                }
                else
                {
                    Logger.Debug(bookTitle + " Does Not Exist at InStockTrades");
                    break;
                }
            }

            //Print data to a txt file
            using (StreamWriter outputFile = new StreamWriter(@"Data\InStockTradesData.txt"))
            {
                if (inStockTradesDataList.Count != 0)
                {
                    foreach (string[] data in inStockTradesDataList)
                    {
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else{
                    outputFile.WriteLine(bookTitle + " Does Not Exist at InStockTrades");
                }
            } 

            return inStockTradesDataList;
        }
    }
}