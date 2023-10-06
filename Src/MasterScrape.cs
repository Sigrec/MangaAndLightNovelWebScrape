using MangaLightNovelWebScrape.Websites.Canada;
using MangaLightNovelWebScrape.Websites.America;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Chrome;
using System.Collections.Concurrent;
using MangaLightNovelWebScrape.Websites.Japan;
using MangaLightNovelWebScrape.Websites;
using MangaLightNovelWebScrape.Websites.Britain;

namespace MangaLightNovelWebScrape
{
    /// <summary>
    /// Debug mode is disabled by default
    /// </summary>
    public partial class MasterScrape
    { 
        internal List<List<EntryModel>> MasterDataList = new List<List<EntryModel>>();
        private ConcurrentBag<List<EntryModel>> ResultsList = new ConcurrentBag<List<EntryModel>>();
        private List<Task> WebTasks = new List<Task>(11);
        private Dictionary<string, string> MasterUrls = new Dictionary<string, string>
        {
            { RightStufAnime.WEBSITE_TITLE, "" },
            { BarnesAndNoble.WEBSITE_TITLE , "" },
            { BooksAMillion.WEBSITE_TITLE , "" },
            { AmazonUSA.WEBSITE_TITLE , "" },
            { KinokuniyaUSA.WEBSITE_TITLE , "" },
            { InStockTrades.WEBSITE_TITLE , "" },
            { RobertsAnimeCornerStore.WEBSITE_TITLE , "" },
            { SciFier.WEBSITE_TITLE, "" },
            { ForbiddenPlanet.WEBSITE_TITLE, "" },
            { Waterstones.WEBSITE_TITLE, "" },
            { AmazonJapan.WEBSITE_TITLE, "" },
            { CDJapan.WEBSITE_TITLE, "" },
            { Indigo.WEBSITE_TITLE, "" }
        };
        private AmazonUSA AmazonUSA;
        private BarnesAndNoble BarnesAndNoble;
        private BooksAMillion BooksAMillion;
        private InStockTrades InStockTrades;
        private KinokuniyaUSA KinokuniyaUSA;
        private RightStufAnime RightStufAnime;
        private RobertsAnimeCornerStore RobertsAnimeCornerStore;
        private Indigo Indigo;
        private AmazonJapan AmazonJapan;
        private CDJapan CDJapan;
        private SciFier SciFier;
        private Waterstones Waterstones;
        private ForbiddenPlanet ForbiddenPlanet;
        public Region Region { get; set; }
        public Browser Browser { get; set; }
        private static readonly Logger LOGGER = LogManager.GetLogger("MasterScrapeLogs");
        private static readonly string[] CHROME_BROWSER_ARGUMENTS = {"--enable-automation", "--no-sandbox", "--disable-infobars", "--disable-dev-shm-usage", "--disable-extensions", "--inprivate", "--incognito", "--disable-logging", "--disable-notifications", "--disable-logging", "--silent"};
        private static readonly string[] FIREFOX_BROWSER_ARGUMENTS = {"-headless", "-new-instance", "-private", "-disable-logging", "-log-level=3"};
        /// <summary>
        /// Determines whether debug mode is enabled (Disabled by default)
        /// </summary>
        internal static bool IsDebugEnabled { get; set; } = false;

        [GeneratedRegex(@"[^\w+]")] internal static partial Regex RemoveNonWordsRegex();
        [GeneratedRegex(@"\d{1,3}")] internal static partial Regex FindVolNumRegex();
        [GeneratedRegex(@"--|—|\s{2,}")] internal static partial Regex MultipleWhiteSpaceRegex();
        [GeneratedRegex(@";jsessionid=[^?]*")] internal static partial Regex RemoveJSessionIDRegex();
        [GeneratedRegex(@"Volume|Vol\\.|Volumr", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex("Official|Character Book|Guide|Art of |Illustration|Chapter Book|Anime Profiles", RegexOptions.IgnoreCase)] internal static partial Regex EntryRemovalRegex();

        public MasterScrape() { } 
        public MasterScrape(Browser Browser = Browser.Chrome) => this.Browser = Browser;
        public MasterScrape(Region Region = Region.America) => this.Region = Region;
        public MasterScrape(Region Region = Region.America, Browser Browser = Browser.Chrome)
        {
            this.Region = Region;
            this.Browser = Browser;
        }

        /// <summary>
        /// Gets Browser Enum from string
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static Browser GetBrowserFromString(string browser)
        {
            return browser switch
            {
                string curBrowser when curBrowser.Equals("Chrome", StringComparison.OrdinalIgnoreCase) => Browser.Chrome,
                string curBrowser when curBrowser.Equals("Edge", StringComparison.OrdinalIgnoreCase) => Browser.Edge,
                string curBrowser when curBrowser.Equals("FireFox", StringComparison.OrdinalIgnoreCase) => Browser.FireFox,
                _ => throw new Exception(),
            };
        }

        /// <summary>
        /// Gets Region Enum from string
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static Region GetRegionFromString(string region)
        {
            return region switch
            {
                string curBrowser when curBrowser.Equals("America", StringComparison.OrdinalIgnoreCase) => Region.America,
                string curBrowser when curBrowser.Equals("Britain", StringComparison.OrdinalIgnoreCase) => Region.Britain,
                string curBrowser when curBrowser.Equals("Japan", StringComparison.OrdinalIgnoreCase) => Region.Japan,
                string curBrowser when curBrowser.Equals("Canada", StringComparison.OrdinalIgnoreCase) => Region.Canada,
                string curBrowser when curBrowser.Equals("Europe", StringComparison.OrdinalIgnoreCase) => Region.Europe,
                _ => throw new Exception(),
            };
        }

        /// <summary>
        /// Gets StockStatus Enum from string
        /// </summary>
        /// <param name="stockStatus"></param>
        /// <returns></returns>
        public static StockStatus GetStockStatusFromString(string stockStatus)
        {
            return stockStatus switch
            {
                "IS" or "In Stock" => StockStatus.IS,
                "PO" or "Pre-Order" => StockStatus.PO,
                "OOS" or "Out of Stock" => StockStatus.OOS,
                _ => StockStatus.NA
            };
        }

        /// <summary>
        /// Gets the array of Websites available for a specific region as strings
        /// </summary>
        /// <param name="region">The region to get</param>
        /// <returns></returns>
        public static string[] GetRegionWebsiteListAsString(Region region)
        {
            return region switch
            {
                Region.America => new string[] { AmazonUSA.WEBSITE_TITLE, BarnesAndNoble.WEBSITE_TITLE, BooksAMillion.WEBSITE_TITLE, InStockTrades.WEBSITE_TITLE, KinokuniyaUSA.WEBSITE_TITLE, RightStufAnime.WEBSITE_TITLE, RobertsAnimeCornerStore.WEBSITE_TITLE, SciFier.WEBSITE_TITLE },
                Region.Britain => new string[] { ForbiddenPlanet.WEBSITE_TITLE, Waterstones.WEBSITE_TITLE, SciFier.WEBSITE_TITLE },
                Region.Canada => new string[] { Indigo.WEBSITE_TITLE, SciFier.WEBSITE_TITLE },
                Region.Europe => new string[] { SciFier.WEBSITE_TITLE },
                Region.Japan => new string[] { AmazonJapan.WEBSITE_TITLE, CDJapan.WEBSITE_TITLE },
                _ => Array.Empty<string>(),
            };
        }

        /// <summary>
        /// Gets the array of Websites available for a specific region as Website Enums
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static Website[] GetRegionWebsiteList(Region region)
        {
            return region switch
            {
                Region.America => new Website[] { Website.AmazonUSA, Website.BarnesAndNoble, Website.BooksAMillion, Website.InStockTrades, Website.KinokuniyaUSA, Website.RightStufAnime, Website.RobertsAnimeCornerStore, Website.SciFier },
                Region.Britain => new Website[] { Website.ForbiddenPlanet, Website.Waterstones, Website.SciFier },
                Region.Canada => new Website[] { Website.Indigo, Website.SciFier },
                Region.Europe => new Website[] { Website.SciFier },
                Region.Japan => new Website[] { Website.AmazonJapan, Website.CDJapan },
                _ => Array.Empty<Website>(),
            };
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
        public Dictionary<string, string> GetResultUrls()
        {
            return MasterUrls;
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

        private void ClearAmericaWebsiteData()
        {
            RightStufAnime?.ClearData();
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
        }

        private void ClearBritainWebsiteData()
        {
            ForbiddenPlanet?.ClearData();
            Waterstones?.ClearData();
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
        /// <param name="searchTitle"></param>
        /// <param name="curTitle"></param>
        /// <param name="removeText"></param>
        /// <returns>True if the curTitle should be removed</returns>
        internal static bool RemoveUnintendedVolumes(string bookTitle, string searchTitle, string curTitle, string removeText)
        {
            return bookTitle.Contains(searchTitle, StringComparison.OrdinalIgnoreCase) && curTitle.Contains(removeText, StringComparison.OrdinalIgnoreCase);
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

        /// <summary>
        /// Generates a list of Website Enums from a given string & region
        /// </summary>
        /// <param name="input">The input list of strings</param>
        /// <param name="curRegion">The region the user wants to create the list from</param>
        /// <returns></returns>
        public static List<Website> GenerateWebsiteList(IEnumerable<string> input, Region curRegion)
        {
            List<Website> WebsiteList = new List<Website>();
            if (input != null && input.Any())
            {
                foreach (string website in input)
                {
                    switch (curRegion)
                    {
                        case Region.America:
                            switch (website)
                            {
                                case RightStufAnime.WEBSITE_TITLE:
                                    WebsiteList.Add(Website.RightStufAnime);
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

        private void GenerateTaskList(IEnumerable<Website> webScrapeList, string bookTitle, BookType book, bool isRightStufMember, bool isBarnesAndNobleMember, bool isBooksAMillionMember, bool isKinokuniyaUSAMember, bool isIndigoMember)
        {
            switch (this.Region)
            {
                case Region.America:
                    Parallel.ForEach(webScrapeList, (site) =>
                    {
                        switch (site)
                        {
                            case Website.RightStufAnime:
                                RightStufAnime = new RightStufAnime();
                                LOGGER.Info($"{RightStufAnime.WEBSITE_TITLE} Going");
                                WebTasks.Add(RightStufAnime.CreateRightStufAnimeTask(bookTitle, book, isRightStufMember, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.BarnesAndNoble:
                                BarnesAndNoble = new BarnesAndNoble();
                                LOGGER.Info($"{BarnesAndNoble.WEBSITE_TITLE} Going");
                                WebTasks.Add(BarnesAndNoble.CreateBarnesAndNobleTask(bookTitle, book, isBarnesAndNobleMember, MasterDataList, SetupBrowserDriver(true)));
                                break;
                            case Website.RobertsAnimeCornerStore:
                                RobertsAnimeCornerStore = new RobertsAnimeCornerStore();
                                LOGGER.Info($"{RobertsAnimeCornerStore.WEBSITE_TITLE} Going");
                                WebTasks.Add(RobertsAnimeCornerStore.CreateRobertsAnimeCornerStoreTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.InStockTrades:
                                InStockTrades = new InStockTrades();
                                LOGGER.Info($"{InStockTrades.WEBSITE_TITLE} Going");
                                WebTasks.Add(InStockTrades.CreateInStockTradesTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.KinokuniyaUSA:
                                KinokuniyaUSA = new KinokuniyaUSA();
                                LOGGER.Info($"{KinokuniyaUSA.WEBSITE_TITLE} Going");
                                WebTasks.Add(KinokuniyaUSA.CreateKinokuniyaUSATask(bookTitle, book, isKinokuniyaUSAMember, MasterDataList, SetupBrowserDriver(true)));
                                break;
                            case Website.BooksAMillion:
                                BooksAMillion = new BooksAMillion();
                                LOGGER.Info($"{BooksAMillion.WEBSITE_TITLE} Going");
                                WebTasks.Add(BooksAMillion.CreateBooksAMillionTask(bookTitle, book, isBooksAMillionMember, MasterDataList, SetupBrowserDriver(true)));
                                break;
                            case Website.AmazonUSA:
                                AmazonUSA = new AmazonUSA();
                                LOGGER.Info($"{AmazonUSA.WEBSITE_TITLE} Going");
                                WebTasks.Add(AmazonUSA.CreateAmazonUSATask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.SciFier:
                                SciFier = new SciFier();
                                LOGGER.Info($"{SciFier.WEBSITE_TITLE} Going");
                                WebTasks.Add(SciFier.CreateSciFierTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
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
                                ForbiddenPlanet = new ForbiddenPlanet();
                                LOGGER.Info($"{ForbiddenPlanet.WEBSITE_TITLE} Going");
                                WebTasks.Add(ForbiddenPlanet.CreateForbiddenPlanetTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.Waterstones:
                                Waterstones = new Waterstones();
                                LOGGER.Info($"{Waterstones.WEBSITE_TITLE} Going");
                                WebTasks.Add(Waterstones.CreateWaterstonesTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.SciFier:
                                SciFier = new SciFier();
                                LOGGER.Info($"{SciFier.WEBSITE_TITLE} Going");
                                WebTasks.Add(SciFier.CreateSciFierTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
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
                            case Website.Indigo:
                                Indigo = new Indigo();
                                LOGGER.Info($"{Indigo.WEBSITE_TITLE} Going");
                                WebTasks.Add(Indigo.CreateIndigoTask(bookTitle, book, isIndigoMember, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.SciFier:
                                SciFier = new SciFier();
                                LOGGER.Info($"{SciFier.WEBSITE_TITLE} Going");
                                WebTasks.Add(SciFier.CreateSciFierTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
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
                                AmazonJapan = new AmazonJapan();
                                LOGGER.Info($"{AmazonJapan.WEBSITE_TITLE} Going");
                                WebTasks.Add(AmazonJapan.CreateAmazonJapanTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            case Website.CDJapan:
                                CDJapan = new CDJapan();
                                LOGGER.Info($"{CDJapan.WEBSITE_TITLE} Going");
                                WebTasks.Add(CDJapan.CreateCDJapanTask(bookTitle, book, MasterDataList, SetupBrowserDriver(false)));
                                break;
                            default:
                                break;
                        }
                    });
                    break;
            }
        }

        // TODO Improve performance of Website Queries Starting w/ RightStufAnime
        // TODO Add ReadMe
        // TODO Figure out how to remove "know your location" from B&N & BAM
        // TODO Brit store https://travellingman.com/

        /// <summary>
        /// Initalizes the scrape and outputs the compared data
        /// </summary>
        /// <param name="bookTitle">The title of the series to search for</param>
        /// <param name="book">The book type of the series either Manga or Light Novel</param>
        /// <param name="stockFilter"></param>
        /// <param name="webScrapeList">The list of websites you want to search at</param>
        /// <param name="browser">The browser either Edge, Chrome,l or FireFox the user wants to use</param>
        /// <param name="isRightStufMember">Whether the user is a RightStufAnime Member</param>
        /// <param name="isBarnesAndNobleMember">Whether the user is a Barnes & Noble Member</param>
        /// <param name="isBooksAMillionMember">Whether the user is a Books-A-Million Member</param>
        /// <param name="isKinokuniyaUSAMember">Whether the user is a Kinokuniya USA member</param>
        /// <param name="isIndigoMember">Whether the user is a Indigo member</param>
        /// <returns></returns>
        public async Task InitializeScrapeAsync(string bookTitle, BookType book, StockStatus[] stockFilter, IEnumerable<Website> webScrapeList, bool isRightStufMember = false, bool isBarnesAndNobleMember = false, bool isBooksAMillionMember = false, bool isKinokuniyaUSAMember = false, bool isIndigoMember = false)
        {
            await Task.Run(async () =>
            {
                LOGGER.Info($"Running on {this.Browser} Browser");

                // Clear the Data & Urls everytime there is a new run
                MasterDataList.Clear();
                Parallel.ForEach(MasterUrls.Keys, (website, state) =>
                {
                    MasterUrls[website] = string.Empty;
                });
                
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
                            LOGGER.Warn("No MasterData Available");
                        }
                    }
                }
            });
        }
        
        private static async Task Main(string[] args)
        {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            MasterScrape scrape = new MasterScrape(Region.Britain, Browser.Chrome).EnableDebugMode();
            await scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), GenerateWebsiteList(new List<string>() { ForbiddenPlanet.WEBSITE_TITLE }, Region.Britain), false, false, false, false, false);
            watch.Stop();
            LOGGER.Info($"Time in Seconds: {(float)watch.ElapsedMilliseconds / 1000}s");
        }

        // public static void Main(string[] args)
        // {
            
        // }
    }
}