namespace MangaLightNovelWebScrape.Src.Websites
{
    public class BarnesAndNoble
    {
        public static List<string> BarnesAndNobleLinks = new List<string>(); //List of links used to get data from BarnesAndNoble
        private static List<string[]> BarnesAndNobleDataList = new List<string[]>(); //List of all the series data from BarnesAndNoble
        private static bool secondWebsiteCheck = false;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("BarnesAndNobleLogs");

        // https://www.barnesandnoble.com/s/Classroom+of+the+Elite/_/N-1z141tjZ8q8Z1gvk
        //light novel
        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url = "";
            if (!secondWebsiteCheck)
            {
                // https://www.barnesandnoble.com/s/jujutsu+kaisen/_/N-1z141tjZucb/?Nrpp=40&page=1
                // https://www.barnesandnoble.com/s/classroom+of+the+elite/_/N-1z141tjZucb/?Nrpp=40&page=1
                // https://www.barnesandnoble.com/s/world+trigger/_/N-1z141tjZ8q8Zucb/?Nrpp=40&page=1
                url = $"https://www.barnesandnoble.com/s/{bookTitle.Replace(" ", "+")}/_/N-1z141tjZucb/?Nrpp=40&page={currPageNum}";
            }
            else
            {
                // https://www.barnesandnoble.com/s/classroom+of+the+elite+manga/_/N-1z141tjZucb/?Nrpp=40&page=1
                url = $"https://www.barnesandnoble.com/s/{bookTitle.Replace(" ", "+")}+manga/_/N-1z141tjZucb/?Nrpp=40&page={currPageNum}";
            }
            Logger.Debug(url);
            BarnesAndNobleLinks.Add(url);
            return url;
        }

        public static string TitleParse(string currTitle, char bookType, string inputTitle){
            string parsedTitle = "";
            currTitle = currTitle.Replace("Vol.", "Vol");

            if (currTitle.Contains("Box Set"))
            {
                parsedTitle = Regex.Replace(currTitle.Replace("(Omnibus Edition)", "Omnibus"), @"(?<=\d{1,3})[^\d{1,3}.]+.*|\,|:| \([^()]*\)", "");
            }
            else
            {
                parsedTitle = Regex.Replace(currTitle.Replace("(Omnibus Edition)", "Omnibus"), @"(?<=Vol \d{1,3})[^\d{1,3}.]+.*|\,|:| \([^()]*\)", "");
            }

            if (!parsedTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Insert(Regex.Match(parsedTitle, @"\d{1,3}").Index, "Vol ");
            }

            return parsedTitle.Trim();
        }

        public static List<string[]> GetBarnesAndNobleData(string bookTitle, char bookType, bool memberStatus, byte currPageNum, EdgeOptions edgeOptions)
        {
            WebDriver dummyDriver = new EdgeDriver(@"DriverExecutables/Edge", edgeOptions);
            edgeOptions.AddArgument("user-agent=" + dummyDriver.ExecuteScript("return navigator.userAgent").ToString().Replace("Headless", ""));
            dummyDriver.Quit();
            
            EdgeDriver edgeDriver = new EdgeDriver(Path.GetFullPath(@"DriverExecutables/Edge"), edgeOptions);
            WebDriverWait wait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(30))
            {
                PollingInterval = TimeSpan.FromMilliseconds(200),
            };
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException));

            Regex removeWords = new Regex(@"[^\w+]");

            restart:
            try
            {
                string currTitle = "";
                while(true)
                {
                    edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='product-shelf-title product-info-title pt-xs']/a")));
                    
                    if (bookType == 'N')
                    {
                        IWebElement novelCheck = edgeDriver.FindElement(By.XPath("//div[@class='product-shelf-title product-info-title pt-xs']//a[contains(@title, 'Novel')]"));
                        Logger.Debug("Check #3");
                        if (novelCheck != null && !secondWebsiteCheck)
                        {
                            Logger.Debug("Trying 2nd URL #1");
                            secondWebsiteCheck = true;
                            goto restart; 
                        }
                    }

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(edgeDriver.PageSource);

                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[@class='product-shelf-title product-info-title pt-xs']/a");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='product-shelf-pricing mt-xs']//div//a//span[2]");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//p[@class='ml-xxs bopis-badge-message mt-0 mb-0' and (contains(text(), 'Online') or contains(text(), 'Pre-order'))]");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//li[@class='pagination__next ']");

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        // currTitle = titleData[x].GetAttributeValue("title", "Title Error");
                        currTitle = TitleParse(titleData[x].GetAttributeValue("title", "Title Error"), bookType, bookTitle);
                        if (removeWords.Replace(currTitle.ToLower(), "").Contains(removeWords.Replace(bookTitle.ToLower(), "")) && (bookType == 'M' && currTitle.Any(Char.IsDigit)))
                        {
                            // if (TitleParse(currTitle, bookType, bookTitle).Contains("7"))
                            // {
                            //     BarnesAndNobleDataList.Add(new string[]{TitleParse(currTitle, bookType, bookTitle), "$2.00", stockStatusData[x].InnerText.Contains("Pre-order") ? "PO" : "IS", "BarnesAndNoble"});
                            //     continue;
                            // }

                            BarnesAndNobleDataList.Add(new string[]{currTitle, priceData[x].InnerText.Trim(), stockStatusData[x].InnerText.Contains("Pre-order") ? "PO" : "IS", "BarnesAndNoble"});

                            //Logger.Debug("[" + BarnesAndNobleDataList[x][0] + ", " + BarnesAndNobleDataList[x][1] + ", " + BarnesAndNobleDataList[x][2] + ", " + BarnesAndNobleDataList[x][3] + "]");
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
                if (!secondWebsiteCheck)
                {
                    Logger.Debug("Trying 2nd URL #2 ? " + ex);
                    secondWebsiteCheck = true;
                    goto restart; 
                }
            }

            BarnesAndNobleDataList.Sort(new VolumeSort(bookTitle));

            using (StreamWriter outputFile = new StreamWriter(@"Data\BarnesAndNobleData.txt"))
            {
                if (BarnesAndNobleDataList.Count != 0)
                {
                    foreach (string[] data in BarnesAndNobleDataList)
                    {
                        Logger.Debug("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else
                {
                    outputFile.WriteLine(bookTitle + " Does Not Exist at BarnesAndNoble");
                }
            }  

            return BarnesAndNobleDataList;
        }
    }
}