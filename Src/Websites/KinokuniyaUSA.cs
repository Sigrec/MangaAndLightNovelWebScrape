namespace MangaLightNovelWebScrape.Src.Websites
{
    public class KinokuniyaUSA
    {
        public static List<string> KinokuniyaUSALinks = new List<string>(); //List of links used to get data from KinokuniyaUSA
        private static List<string[]> KinokuniyaUSADataList = new List<string[]>(); //List of all the series data from KinokuniyaUSA

        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("KinokuniyaUSALogs");
        // Manga English Search
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=world+trigger&taxon=2&x=39&y=4&page=1&per_page=100&form_taxon=109
        // https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=Skeleton+Knight+in+Another+World&taxon=2&x=39&y=11&page=1&per_page=100

        // Light Novel English Search
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=overlord+novel&taxon=&x=33&y=8&per_page=100&form_taxon=109
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=classroom+of+the+elite&taxon=&x=33&y=8&per_page=100&form_taxon=109
        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url = $"https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords={bookTitle.Replace(" ", "+")}{(bookType == 'N' ? "+novel" : "")}&taxon=2&x=39&y=11&page={currPageNum}&per_page=100";
            Logger.Debug(url);
            KinokuniyaUSALinks.Add(url);
            return url;
        }

        public static string TitleParse(string bookTitle, char bookType, string inputTitle)
        {
            string parsedTitle;
            if (!inputTitle.Any(Char.IsDigit))
            {
                parsedTitle = Regex.Replace(bookTitle.Replace("(Omnibus Edition)", "Omnibus"), @"(?<=\d{1,3})[^\d{1,3}]+.*|,| \([^()]*\)|LIGHT NOVEL ", "");
            }
            else
            {
                parsedTitle = Regex.Replace(bookTitle.Replace("(Omnibus Edition)", "Omnibus"), @"(?<=\d{1,3}.$)[^\d{1,3}]+.*|,| \([^()]*\)|LIGHT NOVEL ", "");
            }

            // Check to see if after parsing, the volume number is still there if not then fix/add it back
            if (!MasterScrape.Similar(parsedTitle, inputTitle))
            {
                parsedTitle = Regex.Replace(new Regex(@"\((.*?)\)").Match(bookTitle).Groups[0].ToString(), @"\(|\)", "");
                if (!Char.IsDigit(parsedTitle, parsedTitle.Length - 1))
                {
                    parsedTitle += $" {new Regex(@"(\d+)").Match(bookTitle).Groups[0].ToString()}";
                }
                // Console.WriteLine("Fixed Title 1 = " + parsedTitle);
            }

            if (parsedTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = Regex.Replace(parsedTitle, "MANGA", "", RegexOptions.IgnoreCase);
            }
            else if (parsedTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = Regex.Replace(parsedTitle, "NOVEL", "", RegexOptions.IgnoreCase);
                // Console.WriteLine("Fixed Title 2 = " + parsedTitle);
            }
            

            if (!parsedTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Insert(Regex.Match(parsedTitle, @"\d{1,3}").Index, "Vol ");
                // Console.WriteLine("Fixed Title 3 = " + parsedTitle);
            }
            else if (!parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Replace("Vol.", "Vol");
                // Console.WriteLine("Fixed Title 3 = " + parsedTitle);
            }

            if (bookType == 'N')
            {
                parsedTitle = parsedTitle.Insert(parsedTitle.IndexOf("Vol"), "Novel ");
            }
            else if (bookType == 'M' && !parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && !MasterScrape.Similar(parsedTitle.Substring(0, parsedTitle.IndexOf("Vol") - 1), inputTitle))
            {
                parsedTitle = parsedTitle.Remove(0, parsedTitle.IndexOf("Vol")).Insert(0, char.ToUpper(inputTitle[0]) + inputTitle.ToLower().Substring(1) + " ");
            }

            return parsedTitle.Trim();
        }

        public static List<string[]> GetKinokuniyaUSAData(string bookTitle, char bookType, bool memberStatus, byte currPageNum, EdgeOptions edgeOptions)
        {
            WebDriver dummyDriver = new EdgeDriver(@"DriverExecutables/Edge", edgeOptions);
            edgeOptions.AddArgument("user-agent=" + dummyDriver.ExecuteScript("return navigator.userAgent").ToString().Replace("Headless", ""));
            dummyDriver.Quit();

            EdgeDriver edgeDriver = new EdgeDriver(@"DriverExecutables/Edge", edgeOptions);
            WebDriverWait wait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(30));

            try
            {
                while(true)
                {
                    edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    if (bookType == 'M')
                    {
                        // Click the Manga button so it only shows manga and wait for DOM to fully load
                        wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));
                        edgeDriver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("//p[contains(text(), 'English Books')]/following-sibling::ul//a[contains(text(), 'Manga')]"))));
                    }
                    wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));
                    wait.Until(driver => driver.FindElement(By.XPath("//ul[@class='sortMenu']//li//a[contains(text(), 'List')]"))).Click();
                    wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(edgeDriver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//span[@class='underline']");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//li[@class='price']/span");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//li[@class='status']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//p[@class='pagerArrowR']");
                    
                    // for (int x = 1; x < priceData.Count; x+=2)
                    // {
                    //     Logger.Debug($"[{titleData[x / 2].InnerText}, {priceData[x - 1].InnerText.Trim()}, {priceData[x ].InnerText.Trim()}, {stockStatusData[x / 2].InnerText}, KinokuniyaUSA");
                    // }

                    // Remove all of the novels from the list if user is searching for manga
                    if (titleData != null)
                    {
                        for (int x = 1; x < priceData.Count; x+=2)
                        {
                            if (titleData[x / 2].InnerText.Contains("Novel", StringComparison.OrdinalIgnoreCase) && bookType == 'M' || !titleData[x / 2].InnerText.Any(Char.IsDigit) || titleData[x / 2].InnerText.Contains("Volume", StringComparison.OrdinalIgnoreCase))
                            {
                                titleData.RemoveAt(x / 2);
                                stockStatusData.RemoveAt(x / 2);
                                priceData.RemoveAt(x);
                                priceData.RemoveAt(x - 1);
                                x-=2;
                            }
                        }
                    }
                    else
                    {
                        Logger.Warn($"{bookTitle} Does Not Exist at KinokuniyaUSA");
                    }

                    string stockStatus = "";
                    if (memberStatus) // If the user is a member only take the discounted prices (10%)
                    {
                        for (int x = 1; x < priceData.Count; x+=2)
                        {
                            if (stockStatusData[x / 2].InnerText.Contains("Out of stock"))
                            {
                                stockStatus = "OOS";
                            }
                            else if (stockStatusData[x / 2].InnerText.Contains("Pre Order"))
                            {
                                stockStatus = "PO";
                            }
                            else if (stockStatusData[x / 2].InnerText.Contains("In stock"))
                            {
                                stockStatus = "IS";
                            }

                            if (titleData[x / 2].InnerText.Contains("3"))
                            {
                                Logger.Debug("Check" + TitleParse(titleData[x / 2].InnerText, bookType, bookTitle));
                                KinokuniyaUSADataList.Add(new string[]{TitleParse(titleData[x / 2].InnerText, bookType, bookTitle), "$4.00", stockStatus, "KinokuniyaUSA"});
                                continue;
                            }

                            KinokuniyaUSADataList.Add(new string[]{TitleParse(titleData[x / 2].InnerText, bookType, bookTitle), priceData[x].InnerText.Trim(), stockStatus, "KinokuniyaUSA"});
                        }
                    }
                    else
                    {
                        for (int x = 0; x < priceData.Count; x+=2)
                        {
                            if (stockStatusData[x / 2].InnerText.Contains("Out of stock"))
                            {
                                stockStatus = "OOS";
                            }
                            else if (stockStatusData[x / 2].InnerText.Contains("Pre Order"))
                            {
                                stockStatus = "PO";
                            }
                            else if (stockStatusData[x / 2].InnerText.Contains("In stock"))
                            {
                                stockStatus = "IS";
                            }
                            
                            if (titleData[x / 2].InnerText.Contains("3"))
                            {
                               Logger.Debug("Check" + TitleParse(titleData[x / 2].InnerText, bookType, bookTitle));
                                KinokuniyaUSADataList.Add(new string[]{TitleParse(titleData[x / 2].InnerText, bookType, bookTitle), "$4.00", stockStatus, "KinokuniyaUSA"});
                                continue;
                            }

                            KinokuniyaUSADataList.Add(new string[]{TitleParse(titleData[x / 2].InnerText, bookType, bookTitle), priceData[x].InnerText.Trim(), stockStatus, "KinokuniyaUSA"});
                        }
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                    }
                    else
                    {
                        edgeDriver.Quit();
                        KinokuniyaUSADataList.Sort(new VolumeSort(bookTitle));
                        break;
                    }
                }
            }
            catch (Exception e) when (e is WebDriverTimeoutException || e is NoSuchElementException)
            {
                Logger.Error($"{bookTitle} Does Not Exist @ KinokuniyaUSA\n{e}");
            }

            //Print data to a txt file
            using (StreamWriter outputFile = new StreamWriter(@"Data\KinokuniyaUSAData.txt"))
            {
                if (KinokuniyaUSADataList.Count != 0)
                {
                    foreach (string[] data in KinokuniyaUSADataList)
                    {
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                        Logger.Debug("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else{
                    outputFile.WriteLine(bookTitle + " Does Not Exist at KinokuniyaUSA");
                }
            } 
            return KinokuniyaUSADataList;
        }
    }
}