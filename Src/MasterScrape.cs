using MangaLightNovelWebScrape.Websites.Canada;
using MangaLightNovelWebScrape.Websites.America;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Chrome;
using System.Collections.Concurrent;
using MangaLightNovelWebScrape.Websites.Japan;

namespace MangaLightNovelWebScrape
{
    /// <summary>
    /// Debug mode is disabled by default
    /// </summary>
    public partial class MasterScrape
    { 
        internal List<List<EntryModel>> MasterDataList = new List<List<EntryModel>>();
        private ConcurrentBag<List<EntryModel>> ResultsList = new ConcurrentBag<List<EntryModel>>();
        private List<Task> WebTasks = new List<Task>();
        private Dictionary<string, string> AmericaMasterUrls = new Dictionary<string, string>
        {
            { RightStufAnime.WEBSITE_TITLE, "" },
            { BarnesAndNoble.WEBSITE_TITLE , "" },
            { BooksAMillion.WEBSITE_TITLE , "" },
            { AmazonUSA.WEBSITE_TITLE , "" },
            { KinokuniyaUSA.WEBSITE_TITLE , "" },
            { InStockTrades.WEBSITE_TITLE , "" },
            { RobertsAnimeCornerStore.WEBSITE_TITLE , "" }
        };
        private Dictionary<string, string> CanadaMasterUrls = new Dictionary<string, string>
        {
            { Indigo.WEBSITE_TITLE, "" },
        };
        private Dictionary<string, string> JapanMasterUrls = new Dictionary<string, string>
        {
            { CDJapan.WEBSITE_TITLE, "" },
        };
        private AmazonUSA AmazonUSA = new AmazonUSA();
        private BarnesAndNoble BarnesAndNoble = new BarnesAndNoble();
        private BooksAMillion BooksAMillion = new BooksAMillion();
        private InStockTrades InStockTrades = new InStockTrades();
        private KinokuniyaUSA KinokuniyaUSA = new KinokuniyaUSA();
        private RightStufAnime RightStufAnime = new RightStufAnime();
        private RobertsAnimeCornerStore RobertsAnimeCornerStore = new RobertsAnimeCornerStore();
        private Indigo Indigo = new Indigo();
        private CDJapan CDJapan = new CDJapan();
        public Region Region { get; set; }
        public Browser Browser { get; set; }
        private static readonly Logger LOGGER = LogManager.GetLogger("MasterScrapeLogs");
        private static readonly string[] CHROME_BROWSER_ARGUMENTS = {"--headless=new", "--enable-automation", "--no-sandbox", "--disable-infobars", "--disable-dev-shm-usage", "--disable-extensions", "--inprivate", "--incognito", "--disable-logging", "--disable-notifications", "--disable-logging", "--silent"};
        private static readonly string[] FIREFOX_BROWSER_ARGUMENTS = {"-headless", "-new-instance", "-private", "-disable-logging", "-log-level=3"};
        /// <summary>
        /// Determines whether debug mode is enabled (Disabled by default)
        /// </summary>
        internal static bool IsDebugEnabled { get; set; } = false;

        [GeneratedRegex(@"[^\w+]")] public static partial Regex RemoveNonWordsRegex();
        [GeneratedRegex(@"\d{1,3}")] public static partial Regex FindVolNumRegex();
        [GeneratedRegex(@"--|—|\s{2,}")] public static partial Regex MultipleWhiteSpaceRegex();

        public MasterScrape() { } 
        public MasterScrape(Browser Browser = Browser.Chrome) => this.Browser = Browser;
        public MasterScrape(Region Region = Region.America) => this.Region = Region;
        public MasterScrape(Region Region = Region.America, Browser Browser = Browser.Chrome)
        {
            this.Region = Region;
            this.Browser = Browser;
        }

        public static Browser GetBrowserFromString(string browser)
        {
            return browser switch
            {
                "Edge" => Browser.Edge,
                "FireFox" => Browser.FireFox,
                _ => Browser.Chrome,
            };
        }

        public static Region GetRegionFromString(string region)
        {
            return region switch
            {
                "Britain" => Region.Britain,
                "Japan" => Region.Japan,
                "Canada" => Region.Canada,
                _ => Region.America,
            };
        }

        /// <summary>
        /// Disables debug mode prints the data to text files
        /// </summary>
        public MasterScrape DisableDebugMode()
        {
            IsDebugEnabled = false;
            return this;
        }

        /// <summary>
        /// Enables debug mode aka printing txt files to Data folder
        /// </summary>
        public MasterScrape EnableDebugMode()
        {
            IsDebugEnabled = true;
            if (!Directory.Exists(@"\Data"))
            {
                Directory.CreateDirectory(@"\Data");
            }
            return this;
        }

        /// <summary>
        /// Gets the results of a scrape
        /// </summary>
        /// <returns></returns>
        public List<EntryModel> GetResults()
        {
            return MasterDataList.Count != 0 ? MasterDataList[0] : new List<EntryModel>();
        }

        /// <summary>
        /// Gets the dictionary containing the links to the websites that are used in the final results
        /// </summary>
        /// <returns>Resulting Dictionary based on Region</returns>
        public Dictionary<string, string> GetResultUrls(Region curRegion)
        {
            return curRegion switch
            {
                Region.America => AmericaMasterUrls,
                Region.Canada => CanadaMasterUrls,
                Region.Britain => throw new NotImplementedException(),
                Region.Japan => throw new NotImplementedException(),
                _ => new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// Determines if the book title inputted by the user is contained within the current title scraped from the website
        /// </summary>
        /// <param name="bookTitle">The title inputed by the user to initialize the scrape</param>
        /// <param name="curTitle">The current title scraped from the website</param>
        internal static bool TitleContainsBookTitle(string bookTitle, string curTitle)
        {
            return RemoveNonWordsRegex().Replace(curTitle, "").Contains(RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Clears all entry and url data for every website
        /// </summary>
        private void ClearAllWebsiteData()
        {
            RightStufAnime.ClearData();
            RobertsAnimeCornerStore.ClearData();
            InStockTrades.ClearData();
            KinokuniyaUSA.ClearData();
            BarnesAndNoble.ClearData();
            BooksAMillion.ClearData();
            AmazonUSA.ClearData();
        }

        /// <summary>
        /// <br>Compares the prices of all the volumes that the two websites both have, and outputs the resulting list containing </br>
        /// <br>the lowest prices for each available volume between the websites. If one website does not have a volume that the other</br>
        /// <br>does then that volumes data set defaults to the "smallest" and is added to the list.</br>
        /// </summary>
        /// <param name="smallerList">The smaller list of data sets between the two websites</param>
        /// <param name="biggerList">The bigger list of data sets between the two websites</param>
        /// <param name="bookTitle">The initial title inputted by the user used to determine if the titles in the lists "match"</param>
        /// <returns>The final list of data containing all available lowest price volumes between the two websites</returns>
        private static List<EntryModel> PriceComparison(List<EntryModel> smallerList, List<EntryModel> biggerList, string bookTitle)
        {
            List<EntryModel> finalData = new List<EntryModel>(); // The final list of data containing all available volumes for the series from the website with the lowest price
            bool sameVolumeCheck; // Determines whether a match has been found where the 2 volumes are the same to compare prices for
            int nextVolPos = 0; // The position of the next volume and then proceeding volumes to check if there is a volume to compare
            double biggerListCurrentVolNum; // The current vol number from the website with the bigger list of volumes that is being checked
            // LOGGER.Debug($"Smaller -> {smallerList[0].Website} | Bigger -> {biggerList[0].Website}");

            foreach (EntryModel biggerListData in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(biggerList)){
                sameVolumeCheck = false; // Reset the check to determine if two volumes with the same number has been found to false
                biggerListCurrentVolNum = EntryModel.GetCurrentVolumeNum(biggerListData.Entry);

                if (nextVolPos != smallerList.Count) // Only need to check for a comparison if there are still volumes to compare in the "smallerList"
                {
                    for (int y = nextVolPos; y < smallerList.Count; y++) // Check every volume in the smaller list, skipping over volumes that have already been checked
                    { 
                        // Check to see if the titles are not the same and they are not similar enough, or it is not new then go to the next volume
                        if (smallerList[y].Entry.Contains("Imperfect") || (!smallerList[y].Entry.Equals(biggerListData.Entry, StringComparison.OrdinalIgnoreCase) && EntryModel.Similar(smallerList[y].Entry, biggerListData.Entry, smallerList[y].Entry.Length > biggerListData.Entry.Length ? biggerListData.Entry.Length / 6 : smallerList[y].Entry.Length / 6) != -1))
                        {
                            // LOGGER.Debug($"Not The Same {smallerList[y].Entry} | {biggerListData.Entry} | {!smallerList[y].Entry.Equals(biggerListData.Entry)} | {!EntryModel.Similar(smallerList[y].Entry, biggerListData.Entry)} | {smallerList[y].Entry.Contains("Imperfect")}");
                            continue;
                        }
                        // If the vol numbers are the same and the titles are similar or the same from the if check above, add the lowest price volume to the list
                        
                        // LOGGER.Debug($"MATCH? ({biggerListCurrentVolNum}, {(biggerListData.Entry.Contains("Box Set") ? EntryModel.GetCurrentVolumeNum(smallerList[y].Entry) : EntryModel.GetCurrentVolumeNum(smallerList[y].Entry))}) = {biggerListCurrentVolNum == (biggerListData.Entry.Contains("Box Set") ? EntryModel.GetCurrentVolumeNum(smallerList[y].Entry) : EntryModel.GetCurrentVolumeNum(smallerList[y].Entry))}");
                        if (biggerListCurrentVolNum == (biggerListData.Entry.Contains("Box Set") ? EntryModel.GetCurrentVolumeNum(smallerList[y].Entry) : EntryModel.GetCurrentVolumeNum(smallerList[y].Entry)))
                        {
                            // LOGGER.Debug($"Found Match for {biggerListData.Entry} {smallerList[y].Entry}");
                            // LOGGER.Debug($"PRICE COMPARISON ({float.Parse(biggerListData.Price[1..])}, {float.Parse(smallerList[y].Price[1..])}) -> {float.Parse(biggerListData.Price[1..]) > float.Parse(smallerList[y].Price[1..])}");
                            // Get the lowest price between the two then add the lowest dataset
                            if (float.Parse(biggerListData.Price[1..]) > float.Parse(smallerList[y].Price[1..]))
                            {
                                finalData.Add(smallerList[y]);
                                // LOGGER.Debug($"Add Match SmallerList {smallerList[y]}");
                            }
                            else
                            {
                                finalData.Add(biggerListData);
                                // LOGGER.Debug($"Add Match BiggerList {biggerListData}");
                            }
                            smallerList.RemoveAt(y);

                            nextVolPos = y; // Shift the position in which the next volumes to compare from the smaller list starts essentially "shrinking" the number of comparisons needed whenever a valid comparison is found by 1

                            sameVolumeCheck = true;
                            break;
                        }
                    }
                }

                if (!sameVolumeCheck) // If the current volume number in the bigger list has no match in the smaller list (same volume number and name) then add it
                {
                    // LOGGER.Debug($"Add No Match {biggerListData}");
                    finalData.Add(biggerListData);
                }
            }

            // LOGGER.Debug("SmallerList Size = " + smallerList.Count);
            // Smaller list has volumes that are not present in the bigger list and are volumes that have a volume # greater than the greatest volume # in the bigger lis
            for (int x = 0; x < smallerList.Count; x++)
            {
                // LOGGER.Debug($"Add SmallerList Leftovers {smallerList[x]}");
                finalData.Add(smallerList[x]);
            }
            // finalData.ForEach(data => LOGGER.Info($"Final -> {data}"));
            finalData.Sort(new VolumeSort());
            return finalData;
        }
    
        /// <summary>
        /// Setup the web driver based on inputted browser and whether the website requires a valid user-agent
        /// </summary>
        /// <param name="needsUserAgent">Whether the website needs a valid user-agent</param>
        /// <returns>Edge, Chrome, or FireFox WebDriver</returns>
        internal WebDriver SetupBrowserDriver(bool needsUserAgent)
        {
            switch (this.Browser)
            {
                case Browser.Edge:
                    EdgeOptions edgeOptions = new()
                    {
                        PageLoadStrategy = PageLoadStrategy.Eager,
                    };
                    EdgeDriverService edgeDriverService = EdgeDriverService.CreateDefaultService();
                    edgeDriverService.HideCommandPromptWindow = true;
                    edgeOptions.AddArguments(CHROME_BROWSER_ARGUMENTS);
                    edgeOptions.AddUserProfilePreference("profile.default_content_settings.geolocation", 2);
                    if (needsUserAgent)
                    {
                        WebDriver dummyDriver = new EdgeDriver(edgeOptions);
                        edgeOptions.AddArgument("user-agent=" + dummyDriver.ExecuteScript("return navigator.userAgent").ToString().Replace("Headless", ""));
                        dummyDriver.Close();
                        dummyDriver.Quit();
                    }
                    return new EdgeDriver(edgeDriverService, edgeOptions);
                case Browser.FireFox:
                    FirefoxOptions firefoxOptions = new()
                    {
                        PageLoadStrategy = PageLoadStrategy.Eager,
                        AcceptInsecureCertificates = true
                    };
                    FirefoxDriverService fireFoxDriverService = FirefoxDriverService.CreateDefaultService();
                    fireFoxDriverService.HideCommandPromptWindow = true;
                    firefoxOptions.AddArguments(FIREFOX_BROWSER_ARGUMENTS);
                    firefoxOptions.SetPreference("profile.default_content_settings.geolocation", 2); 
                    return new FirefoxDriver(fireFoxDriverService, firefoxOptions);
                case Browser.Chrome:
                default:
                    ChromeOptions chromeOptions = new()
                    {
                        PageLoadStrategy = PageLoadStrategy.Eager,
                    };
                    ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                    chromeDriverService.HideCommandPromptWindow = true;
                    chromeOptions.AddArguments(CHROME_BROWSER_ARGUMENTS);
                    chromeOptions.AddUserProfilePreference("profile.default_content_settings.geolocation", 2);
                    if (needsUserAgent)
                    {
                        WebDriver dummyDriver = new ChromeDriver(chromeOptions);
                        chromeOptions.AddArgument("user-agent=" + dummyDriver.ExecuteScript("return navigator.userAgent").ToString().Replace("Headless", ""));
                        dummyDriver.Close();
                        dummyDriver.Quit();
                    }
                    return new ChromeDriver(chromeDriverService, chromeOptions);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookTitle"></param>
        /// <param name="searchTitle"></param>
        /// <param name="curTitle"></param>
        /// <param name="removeText"></param>
        /// <returns>True if the curTitle should be removed</returns>
        internal static bool RemoveUnintendedVolumes(string bookTitle, string searchTitle, string curTitle, string removeText)
        {
            return bookTitle.Equals(searchTitle, StringComparison.OrdinalIgnoreCase) && curTitle.Contains(removeText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookTitle"></param>
        /// <returns></returns>
        internal static string FilterBookTitle(string bookTitle)
        {
            char[] trimedChars = {' ', '\'', '!', '-', ','};
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), "%" + Convert.ToByte(var).ToString("x2"));
            }
            return bookTitle;
        }

        public static List<Website> GenerateWebsiteList(IEnumerable<string> input)
        {
            List<Website> WebsiteList = new List<Website>();
            if (input != null && input.Any())
            {
                foreach (string website in input)
                {
                    switch (website)
                    {
                        case "RightStufAnime":
                            WebsiteList.Add(Website.RightStufAnime);
                            break;
                        case "Barnes & Noble":
                            WebsiteList.Add(Website.BarnesAndNoble);
                            break;
                        case "Books-A-Million":
                            WebsiteList.Add(Website.BooksAMillion);
                            break;
                        case "RobertsAnimeCornerStore":
                            WebsiteList.Add(Website.RobertsAnimeCornerStore);
                            break;
                        case "InStockTrades":
                            WebsiteList.Add(Website.InStockTrades);
                            break;
                        case "Kinokuniya USA":
                            WebsiteList.Add(Website.KinokuniyaUSA);
                            break;
                        case "AmazonUSA":
                            WebsiteList.Add(Website.AmazonUSA);
                            break;
                        case "AmazonJapan":
                            WebsiteList.Add(Website.AmazonJapan);
                            break;
                        case "CDJapan":
                            WebsiteList.Add(Website.CDJapan);
                            break;
                        case "Indigo":
                            WebsiteList.Add(Website.Indigo);
                            break;
                    }
                }
            }
            return WebsiteList;
        }
        
        private void GenerateMasterUrlDictionary(Dictionary<string, string> MasterUrls, List<EntryModel> CurMasterDataList)
        {
            foreach (EntryModel entry in CurMasterDataList)
            {
                if (string.IsNullOrWhiteSpace(MasterUrls[entry.Website]))
                {
                    switch (entry.Website)
                    {
                        case RightStufAnime.WEBSITE_TITLE:
                            MasterUrls[entry.Website] = RightStufAnime.GetUrl();
                            break;
                        case RobertsAnimeCornerStore.WEBSITE_TITLE:
                            MasterUrls[entry.Website] = RobertsAnimeCornerStore.GetUrl();
                            break;
                        case InStockTrades.WEBSITE_TITLE:
                            MasterUrls[entry.Website] = InStockTrades.GetUrl();
                            break;
                        case BarnesAndNoble.WEBSITE_TITLE:
                            MasterUrls[entry.Website] = BarnesAndNoble.GetUrl();
                            break;
                        case KinokuniyaUSA.WEBSITE_TITLE:
                            MasterUrls[entry.Website] = KinokuniyaUSA.GetUrl();
                            break;
                        case BooksAMillion.WEBSITE_TITLE:
                            MasterUrls[entry.Website] = BooksAMillion.GetUrl();
                            break;
                        case AmazonUSA.WEBSITE_TITLE:
                            MasterUrls[entry.Website] = AmazonUSA.GetUrl();
                            break;
                        case Indigo.WEBSITE_TITLE:
                            MasterUrls[entry.Website] = Indigo.GetUrl();
                            break;
                    }
                }
            }
        }

        private static void ClearMasterUrlDictionary(Dictionary<string, string> MasterUrls)
        {
            Parallel.ForEach(MasterUrls.Keys, (website, state) =>
            {
                MasterUrls[website] = string.Empty;
            });
        }

        private void GenerateTaskList(IEnumerable<Website> webScrapeList, string bookTitle, BookType book, bool isRightStufMember, bool isBarnesAndNobleMember, bool isBooksAMillionMember, bool isKinokuniyaUSAMember, bool isIndigoMember)
        {
            switch (this.Region)
            {
                case Region.America:
                    LOGGER.Debug("America Websites");
                    ClearMasterUrlDictionary(AmericaMasterUrls);
                    foreach (Website site in webScrapeList)
                    {
                        switch (site)
                        {
                            case Website.RightStufAnime:
                                WebTasks.Add(RightStufAnime.CreateRightStufAnimeTask(bookTitle, book, isRightStufMember, MasterDataList, SetupBrowserDriver(false)));
                                LOGGER.Debug("RightStufAnime Going");
                                break;
                            case Website.BarnesAndNoble:
                                WebTasks.Add(BarnesAndNoble.CreateBarnesAndNobleTask(bookTitle, book, isBarnesAndNobleMember, MasterDataList, SetupBrowserDriver(true)));
                                LOGGER.Debug("Barnes & Noble Going");
                                break;
                            case Website.RobertsAnimeCornerStore:
                                WebTasks.Add(RobertsAnimeCornerStore.CreateRobertsAnimeCornerStoreTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                LOGGER.Debug("RobertsAnimeCornerStore Going");
                                break;
                            case Website.InStockTrades:
                                WebTasks.Add(InStockTrades.CreateInStockTradesTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                LOGGER.Debug("InStockTrades Going");
                                break;
                            case Website.KinokuniyaUSA:
                                WebTasks.Add(KinokuniyaUSA.CreateKinokuniyaUSATask(bookTitle, book, isKinokuniyaUSAMember, MasterDataList, SetupBrowserDriver(true)));
                                LOGGER.Debug("Kinokuniya USA Going");
                                break;
                            case Website.BooksAMillion:
                                WebTasks.Add(BooksAMillion.CreateBooksAMillionTask(bookTitle, book, isBooksAMillionMember, MasterDataList, SetupBrowserDriver(true)));
                                LOGGER.Debug("Books-A-Million Going");
                                break;
                            case Website.AmazonUSA:
                                WebTasks.Add(AmazonUSA.CreateAmazonUSATask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                LOGGER.Debug("Amazon USA Going");
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case Region.Canada:
                    ClearMasterUrlDictionary(CanadaMasterUrls);
                    foreach (Website site in webScrapeList)
                    {
                        switch (site)
                        {
                            case Website.Indigo:
                                WebTasks.Add(Indigo.CreateIndigoTask(bookTitle, book, isIndigoMember, MasterDataList, SetupBrowserDriver(false)));
                                LOGGER.Debug("Indigo Going");
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case Region.Japan:
                    ClearMasterUrlDictionary(JapanMasterUrls);
                    foreach (Website site in webScrapeList)
                    {
                        switch (site)
                        {
                            case Website.CDJapan:
                                WebTasks.Add(CDJapan.CreateCDJapanTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                LOGGER.Debug("CDJapan Going");
                                break;
                            default:
                                break;
                        }
                    }
                    break;
            }
        }

        // TODO Create a Website Interface so websites can extend it
        // TODO Improve performance of Website Queries Starting w/ RightStufAnime
        // TODO Add ReadMe
        // TODO Figure out how to remove "know your location" from B&N & BAM

        /// <summary>
        /// Starts the web scrape
        /// </summary>
        /// <param name="bookTitle">The title of the series to search for</param>
        /// <param name="book">The book type of the series either Manga (M) or Novel (N)</param>
        /// <param name="webScrapeList">The list of websites you want to search at</param>
        /// <param name="browser">The browser either Edge, Chrome,l or FireFox the user wants to use</param>
        /// <param name="isRightStufMember">Whether the user is a RightStufAnime Member</param>
        /// <param name="isBarnesAndNobleMember">Whether the user is a Barnes & Noble Member</param>
        /// <param name="isBooksAMillionMember">Whether the user is a Books-A-Million Member</param>
        /// <param name="isKinokuniyaUSAMember">Whether the user is a Kinokuniya USA member</param>
        /// <returns></returns>
        public async Task InitializeScrapeAsync(string bookTitle, BookType book, StockStatus[] stockFilter, IEnumerable<Website> webScrapeList, bool isRightStufMember = false, bool isBarnesAndNobleMember = false, bool isBooksAMillionMember = false, bool isKinokuniyaUSAMember = false, bool isIndigoMember = false)
        {
            await Task.Run(async () =>
            {
                LOGGER.Debug($"Running on {this.Browser} Browser");
                MasterDataList.Clear(); // Clear the masterlist everytime there is a new run
                
                // Generate List of Tasks to 
                GenerateTaskList(webScrapeList, bookTitle, book, isRightStufMember, isBarnesAndNobleMember, isBooksAMillionMember, isKinokuniyaUSAMember, isIndigoMember);
                await Task.WhenAll(WebTasks);

                MasterDataList.RemoveAll(x => x.Count == 0); // Clear all lists from websites that didn't have any data
                WebTasks.Clear(); // Clear Previous Tasks
                if (MasterDataList != null && MasterDataList.Count != 0)
                {
                    // Apply Stock Status Filter
                    if (stockFilter.Length != 0)
                    {
                        LOGGER.Info("Applying Stock Filters");
                        foreach (List<EntryModel> WebsiteList in MasterDataList)
                        {
                            for (int x = 0; x < WebsiteList.Count; x++)
                            {
                                if (stockFilter.Contains(WebsiteList[x].StockStatus) || WebsiteList[x].StockStatus == StockStatus.NA)
                                {
                                    LOGGER.Info($"Removed {WebsiteList[x].Entry} for {WebsiteList[x].StockStatus} from {WebsiteList[x].Website}");
                                    WebsiteList.RemoveAt(x--);
                                }
                            }
                        }
                    }

                    // If user only is searched from 1 website then skip comparison
                    if (MasterDataList.Count == 1)
                    {
                        MasterDataList[0] = new List<EntryModel>(MasterDataList[0]);
                        goto Skip;
                    }

                    int pos = 0; // The position of the new lists of data after comparing
                    int taskCount;
                    int initialMasterDataListCount;
                    Task[] ComparisonTaskList = new Task[MasterDataList.Count / 2]; // Holds the comparison tasks for execution
                    LOGGER.Debug("Starting Price Comparison");
                    while (MasterDataList.Count > 1) // While there is still 2 or more lists of data to compare prices continue
                    {
                        initialMasterDataListCount = MasterDataList.Count;
                        taskCount = MasterDataList.Count / 2;
                        MasterDataList.Sort((dataSet1, dataSet2) => dataSet1.Count.CompareTo(dataSet2.Count));
                        for (int curTask = 0; curTask < MasterDataList.Count - 1; curTask += 2) // Create all of the tasks for compare processing
                        {
                            List<EntryModel> smallerList = MasterDataList[curTask];
                            List<EntryModel> biggerList = MasterDataList[curTask + 1];
                            ComparisonTaskList[pos] = Task.Run(() => 
                            {
                                ResultsList.Add(PriceComparison(smallerList, biggerList, bookTitle));
                            });
                            pos++;
                        }
                        await Task.WhenAll(ComparisonTaskList);
                        MasterDataList.AddRange(ResultsList); // Add all of the compared lists to the MasterDataList
                        ResultsList.Clear();

                        MasterDataList.RemoveRange(0, initialMasterDataListCount % 2 == 0 ? initialMasterDataListCount : initialMasterDataListCount - 1); // Shrink List

                        // MasterDataList[MasterDataList.Count - 1].ForEach(data => LOGGER.Info($"List 1 {data.ToString()}"));
                        // MasterDataList[0].ForEach(data => LOGGER.Debug($"List 0 {data}"));
                        // LOGGER.Debug("Current Pos = " + pos);
                        pos = 0;
                    }

                    // Add the links to the MasterUrl list and clear data lists
                    Skip:
                    switch (this.Region)
                    {
                        case Region.America:
                            GenerateMasterUrlDictionary(AmericaMasterUrls, MasterDataList[0]);
                            break;
                        case Region.Canada:
                            GenerateMasterUrlDictionary(CanadaMasterUrls, MasterDataList[0]);
                            break;
                    }
                }

                if (IsDebugEnabled)
                {
                    using (StreamWriter outputFile = new(@"Data\MasterData.txt"))
                    {
                        if (MasterDataList.Count > 0)
                        {
                            foreach (EntryModel data in MasterDataList[0])
                            {
                                LOGGER.Debug(data.ToString());
                                outputFile.WriteLine(data.ToString());
                            }

                            foreach (KeyValuePair<string, string> website in GetResultUrls(this.Region))
                            {
                                if (!string.IsNullOrWhiteSpace(website.Value))
                                {
                                    LOGGER.Debug(website);
                                    outputFile.WriteLine(website);
                                }
                            }
                        }
                        else
                        {
                            LOGGER.Info("No MasterData Available");
                        }
                    }
                }
                ClearAllWebsiteData();
            });
        }
        
        private static async Task Main(string[] args)
        {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            MasterScrape scrape = new MasterScrape(Region.America, Browser.Chrome).EnableDebugMode();
            await scrape.InitializeScrapeAsync("World Trigger", BookType.Manga, new StockStatus[] { StockStatus.OOS, StockStatus.PO }, GenerateWebsiteList(new List<string>() {"RobertsAnimeCornerStore", "InStockTrades", "RightStufAnime", "Barnes & Noble"}), false, false, false, false, false);
            watch.Stop();
            LOGGER.Info($"Time in Seconds: {(float)watch.ElapsedMilliseconds / 1000}s");
        }

        // public static void Main(string[] args)
        // {
            
        // }
    }
}