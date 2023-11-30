using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Chrome;
using System.Collections.Concurrent;
using MangaAndLightNovelWebScrape.Websites;

namespace MangaAndLightNovelWebScrape
{
    /// <summary>
    /// Debug mode is disabled by default
    /// </summary>
    public partial class MasterScrape
    { 
        internal List<List<EntryModel>> MasterDataList = new List<List<EntryModel>>();
        private ConcurrentBag<List<EntryModel>> ResultsList = new ConcurrentBag<List<EntryModel>>();
        private List<Task> WebTasks = new List<Task>(13);
        private Dictionary<string, string> MasterUrls = new Dictionary<string, string>();
        private AmazonUSA AmazonUSA = new AmazonUSA();
        private BarnesAndNoble BarnesAndNoble = new BarnesAndNoble();
        private BooksAMillion BooksAMillion = new BooksAMillion();
        private InStockTrades InStockTrades = new InStockTrades();
        private KinokuniyaUSA KinokuniyaUSA = new KinokuniyaUSA();
        private Crunchyroll Crunchyroll = new Crunchyroll();
        private RobertsAnimeCornerStore RobertsAnimeCornerStore = new RobertsAnimeCornerStore();
        private Indigo Indigo = new Indigo();
        private AmazonJapan AmazonJapan = new AmazonJapan();
        private CDJapan CDJapan = new CDJapan();
        private SciFier SciFier = new SciFier();
        private Waterstones Waterstones = new Waterstones();
        private ForbiddenPlanet ForbiddenPlanet = new ForbiddenPlanet();
        internal static VolumeSort VolumeSort = new VolumeSort();
        /// <summary>
        /// The current region of the Scrape
        /// </summary>
        public Region Region { get; set; }
        /// <summary>
        /// The current browser of the Scrape
        /// </summary>
        public Browser Browser { get; set; }
        private static readonly Logger LOGGER = LogManager.GetLogger("MasterScrapeLogs");
        internal static readonly string[] CHROME_BROWSER_ARGUMENTS = ["--headless=new", "--enable-automation", "--no-sandbox", "--disable-infobars", "--disable-dev-shm-usage", "--disable-extensions", "--inprivate", "--incognito", "--disable-logging", "--disable-notifications", "--disable-logging", "--silent"];
        private static readonly string[] FIREFOX_BROWSER_ARGUMENTS = ["-headless", "-new-instance", "-private", "-disable-logging", "-log-level=3"];
        /// <summary>
        /// Excludes nothing or include OOS (Out of Stock) & PO (Pre Order) Entries
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_NONE_FILTER = [];
        /// <summary>
        /// Excludes both OOS (Out of Stock) & PO (Pre Order) entries
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_BOTH_FILTER = [ StockStatus.PO, StockStatus.OOS ];
        /// <summary>
        /// Exludes PO (Pre Order) entries only
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_PO_FILTER = [ StockStatus.PO ];
        /// <summary>
        /// Excludes OOS (Out of Stock) entries only
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_OOS_FILTER = [ StockStatus.OOS ];
        /// <summary>
        /// Determines whether debug mode is enabled (Disabled by default)
        /// </summary>
        internal static bool IsDebugEnabled { get; set; } = false;
        
        [GeneratedRegex(@"\d{1,3}$")] internal static partial Regex FindVolNumRegex();
        [GeneratedRegex(@"--|—|\s{2,}")] internal static partial Regex MultipleWhiteSpaceRegex();
        [GeneratedRegex(@";jsessionid=[^?]*")] internal static partial Regex RemoveJSessionIDRegex();
        [GeneratedRegex(@"GN|Graphic Novel|:\s+Volumes|Volumes|:\s+Volume|Volume|Vol\.|:\s+Volumr|Volumr|Volume(\d{1,3})", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@"Encyclopedia|Anthology|Official|Character|Guide|Art of |[^\w]Art of |Illustration|Anime Profiles|Choose Your Path|Compendium|Artbook|Error|\(Osi\)|Advertising|Art Book|Adventure|Artbook|Coloring Book|the Anime|Calendar|Ani-manga|Anime", RegexOptions.IgnoreCase)] internal static partial Regex EntryRemovalRegex();
        [GeneratedRegex(@"Official|Guide|Adventure|Advertising|Anime|Calendar|Error|Encyclopedia|Anthology|Character", RegexOptions.IgnoreCase)] internal static partial Regex CheckEntryRemovalRegex();

        public MasterScrape(Region Region = Region.America, Browser Browser = Browser.Chrome)
        {
            this.Region = Region;
            this.Browser = Browser;
        }

        /// <summary>
        /// Disables debug mode
        /// </summary>
        public MasterScrape DisableDebugMode()
        {
            IsDebugEnabled = false;
            return this;
        }

        /// <summary>
        /// Enables debug mode allowing printing to txt files in "Data" directory
        /// </summary>
        public MasterScrape EnableDebugMode()
        {
            IsDebugEnabled = true;
            if (!Directory.Exists(@"\Data"))
            {
                Directory.CreateDirectory(@"\Data");
            }
            if (!Directory.Exists(@"\Logs"))
            {
                Directory.CreateDirectory(@"\Logs");
            }
            return this;
        }

        /// <summary>
        /// Gets the results of a scrape
        /// </summary>
        public List<EntryModel> GetResults()
        {
            return MasterDataList.Count != 0 ? MasterDataList[0] : new List<EntryModel>();
        }

        /// <summary>
        /// Gets the dictionary containing the links to the websites that were used in the final results
        /// </summary>
        public Dictionary<string, string> GetResultUrls()
        {
            return MasterUrls;
        }

        /// <summary>
        /// Prints the results of the scrape to the console
        /// </summary>
        public void PrintResultsToConsole()
        {
            if (GetResults().Count > 0)
            {
                GetResults().ForEach(Console.WriteLine);
                foreach (KeyValuePair<string, string> url in GetResultUrls())
                {
                    Console.WriteLine($"[{url.Key}, {url.Value}]");
                }
            }
            else
            {
                Console.WriteLine("No MasterData Available");
            }
        }

        /// <summary>
        /// Prints the results of the scrape to a file
        /// </summary>
        public void PrintResultsToFile(string fileName)
        {
            using (StreamWriter outputFile = new(fileName))
            {
                if (GetResults().Count > 0)
                {
                    foreach (EntryModel data in GetResults())
                    {
                        outputFile.WriteLine(data.ToString());
                    }

                    foreach (KeyValuePair<string, string> website in GetResultUrls())
                    {
                        if (!string.IsNullOrWhiteSpace(website.Value))
                        {
                            outputFile.WriteLine(website);
                        }
                    }
                }
                else
                {
                    outputFile.WriteLine("No MasterData Available");
                }
            }
        }

        /// <summary>
        /// Prints the results of the scrape to a specified logger given a log level
        /// </summary>
        /// <param name="UserLogger">The logger to print the reuslts to</param>
        /// <param name="logLevel">The log level you want the results to be printed to</param>
        public void PrintResultsToLogger(Logger UserLogger, NLog.LogLevel logLevel)
        {
            switch (logLevel.Ordinal)
            {
                case 0:
                    if (GetResults().Count > 0)
                    {
                        GetResults().ForEach(UserLogger.Trace);
                        foreach (KeyValuePair<string, string> url in GetResultUrls())
                        {
                            UserLogger.Trace($"[{url.Key}, {url.Value}]");
                        }
                    }
                    else
                    {
                        UserLogger.Trace("No MasterData Available");
                    }
                    break;
                case 1:
                    if (GetResults().Count > 0)
                    {
                        GetResults().ForEach(UserLogger.Debug);
                        foreach (KeyValuePair<string, string> url in GetResultUrls())
                        {
                            UserLogger.Debug($"[{url.Key}, {url.Value}]");
                        }
                    }
                    else
                    {
                        UserLogger.Debug("No MasterData Available");
                    }
                    break;
                default:
                case 2:
                    if (GetResults().Count > 0)
                    {
                        GetResults().ForEach(UserLogger.Info);
                        foreach (KeyValuePair<string, string> url in GetResultUrls())
                        {
                            UserLogger.Info($"[{url.Key}, {url.Value}]");
                        }
                    }
                    else
                    {
                        UserLogger.Info("No MasterData Available");
                    }
                    break;
                case 3:
                    if (GetResults().Count > 0)
                    {
                        GetResults().ForEach(UserLogger.Warn);
                        foreach (KeyValuePair<string, string> url in GetResultUrls())
                        {
                            UserLogger.Warn($"[{url.Key}, {url.Value}]");
                        }
                    }
                    else
                    {
                        UserLogger.Warn("No MasterData Available");
                    }
                    break;
                case 4:
                    if (GetResults().Count > 0)
                    {
                        GetResults().ForEach(UserLogger.Error);
                        foreach (KeyValuePair<string, string> url in GetResultUrls())
                        {
                            UserLogger.Error($"[{url.Key}, {url.Value}]");
                        }
                    }
                    else
                    {
                        UserLogger.Error("No MasterData Available");
                    }
                    break;
                case 5:
                    if (GetResults().Count > 0)
                    {
                        GetResults().ForEach(UserLogger.Fatal);
                        foreach (KeyValuePair<string, string> url in GetResultUrls())
                        {
                            UserLogger.Fatal($"[{url.Key}, {url.Value}]");
                        }
                    }
                    else
                    {
                        UserLogger.Fatal("No MasterData Available");
                    }
                    break;
            }
        }

        private void ClearAmericaWebsiteData()
        {
            Crunchyroll?.ClearData();
            RobertsAnimeCornerStore?.ClearData();
            InStockTrades?.ClearData();
            KinokuniyaUSA?.ClearData();
            BarnesAndNoble?.ClearData();
            BooksAMillion?.ClearData();
            AmazonUSA?.ClearData();
            SciFier?.ClearData();
        }

        private void ClearCanadaWebsiteData()
        {
            Indigo?.ClearData();
            SciFier?.ClearData();
        }

        private void ClearJapanWebsiteData()
        {
            CDJapan?.ClearData();
            AmazonJapan?.ClearData();
        }

        private void ClearBritainWebsiteData()
        {
            ForbiddenPlanet?.ClearData();
            Waterstones?.ClearData();
            SciFier?.ClearData();
        }

        private void ClearEuropeWebsiteData()
        {
            SciFier?.ClearData();
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
                        if (smallerList[y].Entry.Contains("Imperfect") || (!smallerList[y].Entry.Equals(biggerListData.Entry, StringComparison.OrdinalIgnoreCase) && EntryModel.Similar(smallerList[y].Entry, biggerListData.Entry, smallerList[y].Entry.Length > biggerListData.Entry.Length ? biggerListData.Entry.Length / 6 : smallerList[y].Entry.Length / 6) == -1))
                        {
                            LOGGER.Debug($"Not The Same ({smallerList[y].Entry} | {biggerListData.Entry} | {!smallerList[y].Entry.Equals(biggerListData.Entry)} | {(EntryModel.Similar(smallerList[y].Entry, biggerListData.Entry, smallerList[y].Entry.Length > biggerListData.Entry.Length ? biggerListData.Entry.Length / 6 : smallerList[y].Entry.Length / 6) == -1)} | {smallerList[y].Entry.Contains("Imperfect")})");
                            continue;
                        }
                        // If the vol numbers are the same and the titles are similar or the same from the if check above, add the lowest price volume to the list
                        
                        LOGGER.Debug($"MATCH? ({biggerListCurrentVolNum} = {(biggerListData.Entry.Contains("Box Set") ? EntryModel.GetCurrentVolumeNum(smallerList[y].Entry) : EntryModel.GetCurrentVolumeNum(smallerList[y].Entry))}) -> {biggerListCurrentVolNum == EntryModel.GetCurrentVolumeNum(smallerList[y].Entry)}");
                        if (biggerListCurrentVolNum == EntryModel.GetCurrentVolumeNum(smallerList[y].Entry))
                        {
                            LOGGER.Debug($"Found Match for {biggerListData.Entry} {smallerList[y].Entry}");
                            LOGGER.Debug($"PRICE COMPARISON ({float.Parse(biggerListData.Price[1..])} > {float.Parse(smallerList[y].Price[1..])}) -> {float.Parse(biggerListData.Price[1..]) > float.Parse(smallerList[y].Price[1..])}");
                            // Get the lowest price between the two then add the lowest dataset
                            if (float.Parse(biggerListData.Price[1..]) > float.Parse(smallerList[y].Price[1..]))
                            {
                                finalData.Add(smallerList[y]);
                                LOGGER.Debug($"Add Match SmallerList {smallerList[y]}");
                            }
                            else
                            {
                                finalData.Add(biggerListData);
                                LOGGER.Debug($"Add Match BiggerList {biggerListData}");
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
                    LOGGER.Debug($"Add No Match {biggerListData}");
                    finalData.Add(biggerListData);
                }
            }

            LOGGER.Debug("SmallerList Size = " + smallerList.Count);
            // Smaller list has volumes that are not present in the bigger list and are volumes that have a volume # greater than the greatest volume # in the bigger lis
            for (int x = 0; x < smallerList.Count; x++)
            {
                LOGGER.Debug($"Add SmallerList Leftovers {smallerList[x]}");
                finalData.Add(smallerList[x]);
            }
            // finalData.ForEach(data => LOGGER.Info($"Final -> {data}"));
            finalData.Sort(VolumeSort);
            return finalData;
        }
    
        /// <summary>
        /// Setup the web driver based on inputted browser and whether the website requires a valid user-agent
        /// </summary>
        /// <param name="needsUserAgent">Whether the website needs a valid user-agent</param>
        /// <returns>Edge, Chrome, or FireFox WebDriver</returns>
        protected internal WebDriver SetupBrowserDriver(bool needsUserAgent)
        {
            switch (this.Browser)
            {
                case Browser.Edge:
                    EdgeOptions edgeOptions = new()
                    {
                        PageLoadStrategy = PageLoadStrategy.Normal,
                    };
                    EdgeDriverService edgeDriverService = EdgeDriverService.CreateDefaultService();
                    edgeDriverService.HideCommandPromptWindow = true;
                    edgeOptions.AddArguments(CHROME_BROWSER_ARGUMENTS);
                    edgeOptions.AddExcludedArgument("disable-popup-blocking");
                    edgeOptions.AddUserProfilePreference("profile.default_content_settings.geolocation", 2);
                    edgeOptions.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
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
                        PageLoadStrategy = PageLoadStrategy.Normal,
                        AcceptInsecureCertificates = true
                    };
                    FirefoxDriverService fireFoxDriverService = FirefoxDriverService.CreateDefaultService();
                    fireFoxDriverService.HideCommandPromptWindow = true;
                    firefoxOptions.AddArguments(FIREFOX_BROWSER_ARGUMENTS);
                    firefoxOptions.SetPreference("profile.default_content_settings.geolocation", 2);
                    firefoxOptions.SetPreference("profile.default_content_setting_values.notifications", 2);
                    return new FirefoxDriver(fireFoxDriverService, firefoxOptions);
                case Browser.Chrome:
                default:
                    ChromeOptions chromeOptions = new()
                    {
                        PageLoadStrategy = PageLoadStrategy.Normal,
                    };
                    ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                    chromeDriverService.HideCommandPromptWindow = true;
                    chromeOptions.AddArguments(CHROME_BROWSER_ARGUMENTS);
                    chromeOptions.AddExcludedArgument("disable-popup-blocking");
                    chromeOptions.AddUserProfilePreference("profile.default_content_settings.geolocation", 2);
                    chromeOptions.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
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
        /// <returns></returns>
        protected internal static string FilterBookTitle(string bookTitle)
        {
            char[] trimedChars = [' ', '\'', '!', '-', ','];
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), "%" + Convert.ToByte(var).ToString("x2"));
            }
            return bookTitle;
        }

        /// <summary>
        /// Generates a list of Website Enums from a given string & region
        /// </summary>
        /// <param name="input">The input list of strings</param>
        /// <param name="curRegion">The region the user wants to create the list from</param>
        /// <returns></returns>
        public HashSet<Website> GenerateWebsiteList(IEnumerable<string> input)
        {
            HashSet<Website> WebsiteList = new HashSet<Website>();
            if (input != null && input.Any())
            {
                foreach (string website in input)
                {
                    switch (this.Region)
                    {
                        case Region.America:
                            switch (website)
                            {
                                case Crunchyroll.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.Crunchyroll);
                                    break;
                                case BarnesAndNoble.WEBSITE_TITLE:
                                case "BarnesAndNoble":
                                    WebsiteList.Add(Website.BarnesAndNoble);
                                    break;
                                case "BooksAMillion":
                                case BooksAMillion.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.BooksAMillion);
                                    break;
                                case RobertsAnimeCornerStore.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.RobertsAnimeCornerStore);
                                    break;
                                case InStockTrades.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.InStockTrades);
                                    break;
                                case KinokuniyaUSA.WEBSITE_TITLE:
                                case "KinokuniyaUSA":
                                    WebsiteList.Add(Website.KinokuniyaUSA);
                                    break;
                                case AmazonUSA.WEBSITE_TITLE:
                                case "AmazonUSA":
                                    WebsiteList.Add(Website.AmazonUSA);
                                    break;
                                case SciFier.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.SciFier);
                                    break;
                            }
                            break;
                        case Region.Britain:
                            switch (website)
                            {
                                case ForbiddenPlanet.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.ForbiddenPlanet);
                                    break;
                                case Waterstones.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.Waterstones);
                                    break;
                                case SciFier.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.SciFier);
                                    break;
                            }
                            break;
                        case Region.Canada:
                            switch (website)
                            {
                                case Indigo.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.Indigo);
                                    break;
                                case SciFier.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.SciFier);
                                    break;
                            }
                            break;
                        case Region.Europe:
                            switch (website)
                            {
                                case SciFier.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.SciFier);
                                    break;
                            }
                            break;
                        case Region.Japan:
                            switch (website)
                            {
                                case AmazonJapan.WEBSITE_TITLE:
                                case "AmazonJapan":
                                case "AmazonJP":
                                    WebsiteList.Add(Website.AmazonJapan);
                                    break;
                                case CDJapan.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.CDJapan);
                                    break;
                            }
                            break;
                    }
                }
            }
            return WebsiteList;
        }
        
        private void GenerateMasterUrlDictionary(List<EntryModel> CurMasterDataList)
        {
            foreach (EntryModel entry in CurMasterDataList)
            {
                switch (entry.Website)
                {
                    case Crunchyroll.WEBSITE_TITLE:
                        MasterUrls[entry.Website] = Crunchyroll.GetUrl();
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
                    case SciFier.WEBSITE_TITLE:
                        MasterUrls[entry.Website] = SciFier.GetUrl();
                        break;
                    case ForbiddenPlanet.WEBSITE_TITLE:
                        MasterUrls[entry.Website] = ForbiddenPlanet.GetUrl();
                        break;
                    case Waterstones.WEBSITE_TITLE:
                        MasterUrls[entry.Website] = Waterstones.GetUrl();
                        break;
                    case Indigo.WEBSITE_TITLE:
                        MasterUrls[entry.Website] = Indigo.GetUrl();
                        break;
                    case AmazonJapan.WEBSITE_TITLE:
                        MasterUrls[entry.Website] = AmazonJapan.GetUrl();
                        break;
                    case CDJapan.WEBSITE_TITLE:
                        MasterUrls[entry.Website] = CDJapan.GetUrl();
                        break;
                }
            }
        }

        private void GenerateTaskList(IEnumerable<Website> webScrapeList, string bookTitle, BookType book, bool isBarnesAndNobleMember, bool isBooksAMillionMember, bool isKinokuniyaUSAMember, bool isIndigoMember)
        {
            switch (this.Region)
            {
                case Region.America:
                    Parallel.ForEach(webScrapeList, (site) =>
                    {
                        switch (site)
                        {
                            case Website.Crunchyroll:
                                LOGGER.Info($"{Crunchyroll.WEBSITE_TITLE} Going");
                                WebTasks.Add(Crunchyroll.CreateCrunchyrollTask(bookTitle, book, MasterDataList));
                                break;
                            case Website.BarnesAndNoble:
                                LOGGER.Info($"{BarnesAndNoble.WEBSITE_TITLE} Going");
                                WebTasks.Add(BarnesAndNoble.CreateBarnesAndNobleTask(bookTitle, book, isBarnesAndNobleMember, MasterDataList));
                                break;
                            case Website.RobertsAnimeCornerStore:
                                LOGGER.Info($"{RobertsAnimeCornerStore.WEBSITE_TITLE} Going");
                                WebTasks.Add(RobertsAnimeCornerStore.CreateRobertsAnimeCornerStoreTask(bookTitle, book, MasterDataList));
                                break;
                            case Website.InStockTrades:
                                LOGGER.Info($"{InStockTrades.WEBSITE_TITLE} Going");
                                WebTasks.Add(InStockTrades.CreateInStockTradesTask(bookTitle, book, MasterDataList));
                                break;
                            case Website.KinokuniyaUSA:
                                LOGGER.Info($"{KinokuniyaUSA.WEBSITE_TITLE} Going");
                                WebTasks.Add(KinokuniyaUSA.CreateKinokuniyaUSATask(bookTitle, book, isKinokuniyaUSAMember, MasterDataList, SetupBrowserDriver(true)));
                                break;
                            case Website.BooksAMillion:
                                LOGGER.Info($"{BooksAMillion.WEBSITE_TITLE} Going");
                                WebTasks.Add(BooksAMillion.CreateBooksAMillionTask(bookTitle, book, isBooksAMillionMember, MasterDataList, SetupBrowserDriver(true)));
                                break;
                            case Website.AmazonUSA:
                                LOGGER.Info($"{AmazonUSA.WEBSITE_TITLE} Going");
                                WebTasks.Add(AmazonUSA.CreateAmazonUSATask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.SciFier:
                                LOGGER.Info($"{SciFier.WEBSITE_TITLE} Going");
                                WebTasks.Add(SciFier.CreateSciFierTask(bookTitle, book, MasterDataList, this.Region));
                                break;
                            default:
                                break;
                        }
                    });
                    break;
                case Region.Britain:
                    Parallel.ForEach(webScrapeList, (site) =>
                    {
                        switch (site)
                        {
                            case Website.ForbiddenPlanet:
                                LOGGER.Info($"{ForbiddenPlanet.WEBSITE_TITLE} Going");
                                WebTasks.Add(ForbiddenPlanet.CreateForbiddenPlanetTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.Waterstones:
                                LOGGER.Info($"{Waterstones.WEBSITE_TITLE} Going");
                                WebTasks.Add(Waterstones.CreateWaterstonesTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.SciFier:
                                LOGGER.Info($"{SciFier.WEBSITE_TITLE} Going");
                                WebTasks.Add(SciFier.CreateSciFierTask(bookTitle, book, MasterDataList, this.Region));
                                break;
                            default:
                                break;
                        }
                    });
                    break;
                case Region.Canada:
                    Parallel.ForEach(webScrapeList, (site) =>
                    {
                        switch (site)
                        {
                            case Website.Crunchyroll:
                                LOGGER.Info($"{Crunchyroll.WEBSITE_TITLE} Going");
                                WebTasks.Add(Crunchyroll.CreateCrunchyrollTask(bookTitle, book, MasterDataList));
                                break;
                            case Website.Indigo:
                                LOGGER.Info($"{Indigo.WEBSITE_TITLE} Going");
                                WebTasks.Add(Indigo.CreateIndigoTask(bookTitle, book, isIndigoMember, MasterDataList));
                                break;
                            case Website.SciFier:
                                LOGGER.Info($"{SciFier.WEBSITE_TITLE} Going");
                                WebTasks.Add(SciFier.CreateSciFierTask(bookTitle, book, MasterDataList, this.Region));
                                break;
                            default:
                                break;
                        }
                    });
                    break;
                case Region.Japan:
                    Parallel.ForEach(webScrapeList, (site) =>
                    {
                        switch (site)
                        {
                            case Website.AmazonJapan:
                                LOGGER.Info($"{AmazonJapan.WEBSITE_TITLE} Going");
                                WebTasks.Add(AmazonJapan.CreateAmazonJapanTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.CDJapan:
                                LOGGER.Info($"{CDJapan.WEBSITE_TITLE} Going");
                                WebTasks.Add(CDJapan.CreateCDJapanTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            default:
                                break;
                        }
                    });
                    break;
                case Region.Europe:
                    Parallel.ForEach(webScrapeList, (site) =>
                    {
                        switch (site)
                        {
                            case Website.SciFier:
                                LOGGER.Info($"{SciFier.WEBSITE_TITLE} Going");
                                WebTasks.Add(SciFier.CreateSciFierTask(bookTitle, book, MasterDataList, this.Region));
                                break;
                            default:
                                break;
                        }
                    });
                    break;
            }
        }
        
        // TODO Brit store https://travellingman.com/
        // TODO Remove "Location" Popup for BAM
        // TODO Create custom exceptions

        /// <summary>
        /// Initalizes the scrape and outputs the compared data
        /// </summary>
        /// <param name="bookTitle">The title of the series to search for</param>
        /// <param name="book">The book type of the series either Manga or Light Novel</param>
        /// <param name="stockFilter"></param>
        /// <param name="webScrapeList">The list of websites you want to search at</param>
        /// <param name="browser">The browser either Edge, Chrome, or FireFox the user wants to use</param>
        /// <param name="isBarnesAndNobleMember">Whether the user is a Barnes & Noble Member</param>
        /// <param name="isBooksAMillionMember">Whether the user is a Books-A-Million Member</param>
        /// <param name="isKinokuniyaUSAMember">Whether the user is a Kinokuniya USA member</param>
        /// <param name="isIndigoMember">Whether the user is a Indigo member</param>
        /// <returns></returns>
        public async Task InitializeScrapeAsync(string bookTitle, BookType bookType, StockStatus[] stockFilter, HashSet<Website> webScrapeList, bool isBarnesAndNobleMember = false, bool isBooksAMillionMember = false, bool isKinokuniyaUSAMember = false, bool isIndigoMember = false)
        {
            await Task.Run(async () =>
            {
                LOGGER.Info($"Region set to {this.Region}");
                LOGGER.Info($"Running on {this.Browser} Browser");

                // Clear the Data & Urls everytime there is a new run
                MasterDataList.Clear();
                Parallel.ForEach(MasterUrls.Keys, (website, state) =>
                {
                    MasterUrls[website] = string.Empty;
                });
                
                // Generate List of Tasks to 
                GenerateTaskList(webScrapeList, bookTitle, bookType, isBarnesAndNobleMember, isBooksAMillionMember, isKinokuniyaUSAMember, isIndigoMember);
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
                    GenerateMasterUrlDictionary(MasterDataList[0]);
                    switch (this.Region)
                    {
                        case Region.America:
                            ClearAmericaWebsiteData();
                            break;
                        case Region.Canada:
                            ClearCanadaWebsiteData();
                            break;
                        case Region.Japan:
                            ClearJapanWebsiteData();
                            break;
                        case Region.Britain:
                            ClearBritainWebsiteData();
                            break;
                        case Region.Europe:
                            ClearEuropeWebsiteData();
                            break;
                    }
                }

                if (IsDebugEnabled)
                {
                    using (StreamWriter outputFile = new(@"Data\MasterData.txt"))
                    {
                        if (MasterDataList.Count > 0)
                        {
                            foreach (EntryModel data in GetResults())
                            {
                                LOGGER.Info(data.ToString());
                                outputFile.WriteLine(data.ToString());
                            }

                            foreach (KeyValuePair<string, string> website in GetResultUrls())
                            {
                                if (!string.IsNullOrWhiteSpace(website.Value))
                                {
                                    LOGGER.Info(website);
                                    outputFile.WriteLine(website);
                                }
                            }
                        }
                        else
                        {
                            outputFile.WriteLine("No MasterData Available");
                            LOGGER.Warn("No MasterData Available");
                        }
                    }
                }
            });
        }
        
        // Need to Add AoT tests for all websites and make fixes
        //In the UK there's 
        // Wordery https://wordery.com/
        // Books Etc https://www.books-etc.com/
        // Speedyhen https://www.speedyhen.com/
        // Booksplease https://booksplea.se/index.php
        // Hive https://www.hive.co.uk/WhatsHiveallabout
        // Blackwells https://blackwells.co.uk/bookshop/home
        // Travelling Man https://travellingman.com/
        // Awesome Books https://www.awesomebooks.com/
        // Alibris https://m.alibris.co.uk/

        // Command to end all chrome.exe process -> taskkill /F /IM chrome.exe /T
        // Command to end all chromedriver.exe process -> taskkill /F /IM chromedriver.exe /T
        // TODO Add checks for 
        private static async Task Main(string[] args)
        {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            MasterScrape scrape = new MasterScrape(Region.America, Browser.Chrome).EnableDebugMode();
            await scrape.InitializeScrapeAsync("Attack On Titan", BookType.Manga, EXCLUDE_NONE_FILTER, [ Website.ForbiddenPlanet ], false, false, false, false);
            watch.Stop();
            LOGGER.Info($"Time in Seconds: {(float)watch.ElapsedMilliseconds / 1000}s");
        }
    }
}