using MangaLightNovelWebScrape.Websites;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium.Firefox;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MangaLightNovelWebScrape
{
    public partial class MasterScrape
    { 
        private List<List<EntryModel>> MasterList = new();
        private static string browser { get; set; }
        private ConcurrentBag<List<EntryModel>> ResultsList = new();
        private List<Task> WebTasks = new();
        private Task[] ComparisonTaskList; // Holds the comparison tasks for execution
        private Dictionary<string, string> MasterUrls = new Dictionary<string, string>
        {
            { RightStufAnime.WEBSITE_TITLE, "" },
            { BarnesAndNoble.WEBSITE_TITLE , "" },
            { BooksAMillion.WEBSITE_TITLE , "" },
            { AmazonUSA.WEBSITE_TITLE , "" },
            { KinokuniyaUSA.WEBSITE_TITLE , "" },
            { InStockTrades.WEBSITE_TITLE , "" },
            { RobertsAnimeCornerStore.WEBSITE_TITLE , "" },
            { Indigo.WEBSITE_TITLE, "" }
        };

        private static readonly Logger Logger = LogManager.GetLogger("MasterScrapeLogs");
        /// <summary>
        /// Determines whether debug mode is enabled (Disabled by default)
        /// </summary>
        public static bool IsDebugEnabled { get; set; }
        /// <summary>
        /// The browser arguments used for Chrome & Edge
        /// </summary>
        private static string[] ChromeBrowserArguments = { "--headless=new", "--enable-automation", "--no-sandbox", "--disable-infobars", "--disable-dev-shm-usage", "--disable-extensions", "--inprivate", "--incognito", "--disable-geolocation", "--disable-logging"  };
        /// <summary>
        /// The browser arguments used for FireFox
        /// </summary>
        private static string[] FireFoxBrowserArguments = { "-headless", "-new-instance", "-private" };
        [GeneratedRegex("[^\\w+]")] public static partial Regex RemoveNonWordsRegex();
        [GeneratedRegex("\\d{1,3}")] public static partial Regex FindVolNumRegex();

        // In the UK there's 
        // Wordery https://wordery.com/
        // Books Etc https://www.booksetc.co.uk/
        // Speedyhen https://www.speedyhen.com/
        // Booksplease https://booksplea.se/index.php
        // Hive https://www.hive.co.uk/WhatsHiveallabout
        // Blackwells https://blackwells.co.uk/bookshop/home
        // Travelling Man https://travellingman.com/
        // Awesome Books https://www.awesomebooks.com/
        // World of Books https://www.wob.com/en-gb
        // Alibris https://m.alibris.co.uk/
        // And, of course, SciFier https://scifier.com/

        public enum Website
        {
            AmazonJapan,
            AmazonUSA,
            BarnesAndNoble,
            BooksAMillion,
            CDJapan,
            InStockTrades,
            KinokuniyaUSA,
            RightStufAnime,
            RobertsAnimeCornerStore,
            Indigo
        }

        public MasterScrape(bool IsDebugEnabled = false) {}

        private async Task CreateRightStufAnimeTask(string bookTitle, Book book, bool isMember)
        {
            await Task.Run(() =>
            {
                MasterList.Add(RightStufAnime.GetRightStufAnimeData(bookTitle, book, isMember, 1));
            });
        }

        private async Task CreateRobertsAnimeCornerStoreTask(string bookTitle, Book book)
        {
            await Task.Run(() =>
            {
                MasterList.Add(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData(bookTitle, book));
            });
        }
        
        private async Task CreateInStockTradesTask(string bookTitle, Book book)
        {
            await Task.Run(() => 
            {
                MasterList.Add(InStockTrades.GetInStockTradesData(bookTitle, book, 1));
            });
        }

        private async Task CreateKinokuniyaUSATask(string bookTitle, Book book, bool isMember)
        {
            await Task.Run(() => 
            {
                MasterList.Add(KinokuniyaUSA.GetKinokuniyaUSAData(bookTitle, book, isMember, 1));
            });
        }

        private async Task CreateBarnesAndNobleTask(string bookTitle, Book book, bool isMember)
        {
            await Task.Run(() => 
            {
                MasterList.Add(BarnesAndNoble.GetBarnesAndNobleData(bookTitle, book, isMember, 1));
            });
        }

        private async Task CreateBooksAMillionTask(string bookTitle, Book book, bool isMember)
        {
            await Task.Run(() => 
            {
                MasterList.Add(BooksAMillion.GetBooksAMillionData(bookTitle, book, isMember, 1));
            });
        }

        private async Task CreateAmazonUSATask(string bookTitle, Book book)
        {
            await Task.Run(() => 
            {
                MasterList.Add(AmazonUSA.GetAmazonUSAData(bookTitle, book, 1));
            });
        }

        private async Task CreateIndigoTask(string bookTitle, Book book, bool isMember)
        {
            await Task.Run(() => 
            {
                MasterList.Add(Indigo.GetIndigoData(bookTitle, book, isMember));
            });
        }

        /// <summary>
        /// Disables debug mode
        /// </summary>
        public static void DisableDebugMode()
        {
            IsDebugEnabled = false;
        }

        /// <summary>
        /// Enables debug mode aka printing txt files to Data folder
        /// </summary>
        public static void EnableDebugMode()
        {
            IsDebugEnabled = true;
            if (!Directory.Exists(@"\Data"))
            {
                Directory.CreateDirectory(@"\Data");
            }
        }

        /// <summary>
        /// Gets the results of a scrape
        /// </summary>
        /// <returns></returns>
        public List<EntryModel> GetResults()
        {
            return MasterList.ElementAt(0);
        }

        /// <summary>
        /// Determines if the book title inputted by the user is contained within the current title scraped from the website
        /// </summary>
        /// <param name="bookTitle">The title inputed by the user to initialize the scrape</param>
        /// <param name="curTitle">The current title scraped from the website</param>
        public static bool TitleContainsBookTitle(string bookTitle, string curTitle)
        {
            return RemoveNonWordsRegex().Replace(curTitle, "").Contains(RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the dictionary containing the links to the websites that are used in the final results
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetResultUrls()
        {
            return MasterUrls;
        }

        /// <summary>
        /// Clears all entry and url data for every website
        /// </summary>
        public static void ClearAllWebsiteData()
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
       /// TODO Need to figure out what to do if the prices are the same, most likely at the end the site with the most entries is favored? Or can list multiple
        private static List<EntryModel> PriceComparison(List<EntryModel> smallerList, List<EntryModel> biggerList, string bookTitle)
        {
            List<EntryModel> finalData = new List<EntryModel>(); // The final list of data containing all available volumes for the series from the website with the lowest price
            bool sameVolumeCheck; // Determines whether a match has been found where the 2 volumes are the same to compare prices for
            int nextVolPos = 0; // The position of the next volume and then proceeding volumes to check if there is a volume to compare
            double biggerListCurrentVolNum; // The current vol number from the website with the bigger list of volumes that is being checked
            // Logger.Debug($"Smaller -> {smallerList[0].Website} | Bigger -> {biggerList[0].Website}");

            foreach (EntryModel biggerListData in biggerList){
                sameVolumeCheck = false; // Reset the check to determine if two volumes with the same number has been found to false
                biggerListCurrentVolNum = EntryModel.GetCurrentVolumeNum(biggerListData.Entry);

                if (nextVolPos != smallerList.Count) // Only need to check for a comparison if there are still volumes to compare in the "smallerList"
                {
                    for (int y = nextVolPos; y < smallerList.Count; y++) // Check every volume in the smaller list, skipping over volumes that have already been checked
                    { 
                        // Check to see if the titles are not the same and they are not similar enough, or it is not new then go to the next volume
                        if (smallerList[y].Entry.Contains("Imperfect") || (!smallerList[y].Entry.Equals(biggerListData.Entry, StringComparison.OrdinalIgnoreCase) && !EntryModel.Similar(smallerList[y].Entry, biggerListData.Entry)))
                        {
                            Logger.Debug($"Not The Same {smallerList[y].Entry} | {biggerListData.Entry} | {!smallerList[y].Entry.Equals(biggerListData.Entry)} | {!EntryModel.Similar(smallerList[y].Entry, biggerListData.Entry)} | {smallerList[y].Entry.Contains("Imperfect")}");
                            continue;
                        }
                        // If the vol numbers are the same and the titles are similar or the same from the if check above, add the lowest price volume to the list
                        
                        Logger.Debug($"MATCH? ({biggerListCurrentVolNum}, {(biggerListData.Entry.Contains("Box Set") ? EntryModel.GetCurrentVolumeNum(smallerList[y].Entry) : EntryModel.GetCurrentVolumeNum(smallerList[y].Entry))}) = {biggerListCurrentVolNum == (biggerListData.Entry.Contains("Box Set") ? EntryModel.GetCurrentVolumeNum(smallerList[y].Entry) : EntryModel.GetCurrentVolumeNum(smallerList[y].Entry))}");
                        if (biggerListCurrentVolNum == (biggerListData.Entry.Contains("Box Set") ? EntryModel.GetCurrentVolumeNum(smallerList[y].Entry) : EntryModel.GetCurrentVolumeNum(smallerList[y].Entry)))
                        {
                            Logger.Debug($"Found Match for {biggerListData.Entry} {smallerList[y].Entry}");
                            Logger.Debug($"PRICE COMPARISON ({float.Parse(biggerListData.Price[1..])}, {float.Parse(smallerList[y].Price[1..])}) -> {float.Parse(biggerListData.Price[1..]) > float.Parse(smallerList[y].Price[1..])}");
                            // Get the lowest price between the two then add the lowest dataset
                            if (float.Parse(biggerListData.Price[1..]) > float.Parse(smallerList[y].Price[1..]))
                            {
                                finalData.Add(smallerList[y]);
                                Logger.Debug($"Add Match SmallerList {smallerList[y]}");
                            }
                            else
                            {
                                finalData.Add(biggerListData);
                                Logger.Debug($"Add Match BiggerList {biggerListData}");
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
                    Logger.Debug($"Add No Match {biggerListData}");
                    finalData.Add(biggerListData);
                }
            }

            // Logger.Debug("SmallerList Size = " + smallerList.Count);
            // Smaller list has volumes that are not present in the bigger list and are volumes that have a volume # greater than the greatest volume # in the bigger lis
            for (int x = 0; x < smallerList.Count; x++)
            {
                Logger.Debug($"Add SmallerList Leftovers {smallerList[x]}");
                finalData.Add(smallerList[x]);
            }
            // finalData.ForEach(data => Logger.Info($"Final -> {data}"));
            finalData.Sort(new VolumeSort());
            return finalData;
        }
    
        // TODO Need to find a way to find/check for driver.exe since it only checks current drive
        public static WebDriver SetupBrowserDriver(bool needsUserAgent)
        {
            switch (browser)
            {
                case "Edge":
                    EdgeOptions edgeOptions = new()
                    {
                        PageLoadStrategy = PageLoadStrategy.Eager,
                    };
                    edgeOptions.AddArguments(ChromeBrowserArguments);
                    edgeOptions.AddUserProfilePreference("profile.default_content_settings.geolocation", 2);
                    if (needsUserAgent)
                    {
                        WebDriver dummyDriver = new EdgeDriver(edgeOptions);
                        edgeOptions.AddArgument("user-agent=" + dummyDriver.ExecuteScript("return navigator.userAgent").ToString().Replace("Headless", ""));
                        dummyDriver.Quit();
                    }
                    return new EdgeDriver(edgeOptions);
                case "FireFox":
                    FirefoxOptions firefoxOptions = new()
                    {
                        PageLoadStrategy = PageLoadStrategy.Eager,
                        AcceptInsecureCertificates = true
                    };
                    firefoxOptions.AddArguments(FireFoxBrowserArguments);
                    firefoxOptions.SetPreference("profile.default_content_settings.geolocation", 2); 
                    return new FirefoxDriver(firefoxOptions);
                case "Chrome":
                default:
                    ChromeOptions chromeOptions = new()
                    {
                        PageLoadStrategy = PageLoadStrategy.Eager,
                    };
                    chromeOptions.AddArguments(ChromeBrowserArguments);
                    chromeOptions.AddUserProfilePreference("profile.default_content_settings.geolocation", 2);
                    if (needsUserAgent)
                    {
                        WebDriver dummyDriver = new ChromeDriver(chromeOptions);
                        chromeOptions.AddArgument("user-agent=" + dummyDriver.ExecuteScript("return navigator.userAgent").ToString().Replace("Headless", ""));
                        dummyDriver.Quit();
                    }
                    return new ChromeDriver(chromeOptions);
            }
        }

        public static bool RemoveUnintendedVolumes(string bookTitle, string searchTitle, string curTitle, string otherTitle)
        {
            return bookTitle.Equals(searchTitle, StringComparison.OrdinalIgnoreCase) && curTitle.Contains(otherTitle, StringComparison.OrdinalIgnoreCase);
        }

        // TODO Logic for when the prices are the same
        // TODO Figure out how to clear data within this method
        // TODO Create a Website Interface so websites can extend it
        // TODO Improve performance Starting w/ RightStufAnime
        // TODO Add ReadMe

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
        public async Task InitializeScrapeAsync(string bookTitle, Book book, string[] stockFilter, List<Website> webScrapeList, string curBrowser = "Error", bool isRightStufMember = false, bool isBarnesAndNobleMember = false, bool isBooksAMillionMember = false, bool isKinokuniyaUSAMember = false, bool isIndigoMember = false)
        {
            browser = curBrowser;
            Logger.Debug($"Running on {browser} Browser");
            await Task.Run(async () =>
            {
                MasterList.Clear(); // Clear the masterlist everytime there is a new run
                foreach (string website in MasterUrls.Keys) // Clear urls
                {
                    MasterUrls[website] = string.Empty;
                }
                
                // Generate List of Tasks to 
                foreach (Website site in webScrapeList)
                {
                    switch (site)
                    {
                        case Website.RightStufAnime:
                            WebTasks.Add(CreateRightStufAnimeTask(bookTitle, book, isRightStufMember));
                            Logger.Debug("RightStufAnime Going");
                            break;
                        case Website.BarnesAndNoble:
                            WebTasks.Add(CreateBarnesAndNobleTask(bookTitle, book, isBarnesAndNobleMember));
                            Logger.Debug("Barnes & Noble Going");
                            break;
                        case Website.RobertsAnimeCornerStore:
                            WebTasks.Add(CreateRobertsAnimeCornerStoreTask(bookTitle, book));
                            Logger.Debug("RobertsAnimeCornerStore Going");
                            break;
                        case Website.InStockTrades:
                            WebTasks.Add(CreateInStockTradesTask(bookTitle, book));
                            Logger.Debug("InStockTrades Going");
                            break;
                        case Website.KinokuniyaUSA:
                            WebTasks.Add(CreateKinokuniyaUSATask(bookTitle, book, isKinokuniyaUSAMember));
                            Logger.Debug("Kinokuniya USA Going");
                            break;
                        case Website.BooksAMillion:
                            WebTasks.Add(CreateBooksAMillionTask(bookTitle, book, isBooksAMillionMember));
                            Logger.Debug("Books-A-Million Going");
                            break;
                        case Website.AmazonUSA:
                            WebTasks.Add(CreateAmazonUSATask(bookTitle, book));
                            Logger.Debug("Amazon USA Going");
                            break;
                        case Website.Indigo:
                            WebTasks.Add(CreateIndigoTask(bookTitle, book, isIndigoMember));
                            break;
                    }
                }

                await Task.WhenAll(WebTasks);
                MasterList.RemoveAll(x => x.Count == 0); // Clear all lists from websites that didn't have any data
                WebTasks.Clear();
                if (MasterList.IsNullOrEmpty())
                {
                    goto Skip;
                }

                // Apply Stock Status Filter
                Logger.Debug("Applying Stock Filters");
                if (stockFilter.Length != 0)
                {
                    foreach (List<EntryModel> website in MasterList)
                    {
                        for (int x = 0; x < website.Count; x++)
                        {
                            if (stockFilter.Contains(website[x].StockStatus))
                            {
                                Logger.Debug($"Removed {website[x].Entry} for {website[x].StockStatus} from {website[x].Website}");
                                website.RemoveAt(x--);
                            }
                        }
                    }
                }

                int pos = 0; // The position of the new lists of data after comparing
                int taskCount;
                int initialMasterListCount;
                Array.Resize(ref ComparisonTaskList, MasterList.Count / 2); // Holds the comparison tasks for execution
                Logger.Debug("Starting Price Comparison");
                while (MasterList.Count > 1) // While there is still 2 or more lists of data to compare prices continue
                {
                    initialMasterListCount = MasterList.Count;
                    taskCount = MasterList.Count / 2;
                    MasterList.Sort((dataSet1, dataSet2) => dataSet1.Count.CompareTo(dataSet2.Count));
                    for (int curTask = 0; curTask < MasterList.Count - 1; curTask += 2) // Create all of the tasks for compare processing
                    {
                        List<EntryModel> smallerList = MasterList[curTask];
                        List<EntryModel> biggerList = MasterList[curTask + 1];
                        ComparisonTaskList[pos] = Task.Run(() => 
                        {
                            ResultsList.Add(PriceComparison(smallerList, biggerList, bookTitle));
                        });
                        pos++;
                    }
                    await Task.WhenAll(ComparisonTaskList);
                    MasterList.AddRange(ResultsList); // Add all of the compared lists to the MasterList
                    ResultsList.Clear();

                    MasterList.RemoveRange(0, initialMasterListCount % 2 == 0 ? initialMasterListCount : initialMasterListCount - 1); // Shrink List

                    // MasterList[MasterList.Count - 1].ForEach(data => Logger.Info($"List 1 {data.ToString()}"));
                    // MasterList[0].ForEach(data => Logger.Debug($"List 0 {data}"));
                    // Logger.Debug("Current Pos = " + pos);
                    pos = 0;
                }

                // Add the links to the MasterUrl list and clear data lists
                foreach (EntryModel entry in MasterList[0])
                {
                    if (string.IsNullOrWhiteSpace(MasterUrls[entry.Website]))
                    {
                        switch (entry.Website)
                        {
                            case RightStufAnime.WEBSITE_TITLE:
                                MasterUrls[entry.Website] = RightStufAnime.GetUrlLinks()[0];
                                break;
                            case RobertsAnimeCornerStore.WEBSITE_TITLE:
                                MasterUrls[entry.Website] = RobertsAnimeCornerStore.GetUrls().Last();
                                break;
                            case InStockTrades.WEBSITE_TITLE:
                                MasterUrls[entry.Website] = InStockTrades.InStockTradesLinks[0];
                                break;
                            case BarnesAndNoble.WEBSITE_TITLE:
                                MasterUrls[entry.Website] = BarnesAndNoble.BarnesAndNobleLinks[0];
                                break;
                            case KinokuniyaUSA.WEBSITE_TITLE:
                                MasterUrls[entry.Website] = KinokuniyaUSA.KinokuniyaUSALinks[0];
                                break;
                            case BooksAMillion.WEBSITE_TITLE:
                                MasterUrls[entry.Website] = BooksAMillion.BooksAMillionLinks[0];
                                break;
                            case AmazonUSA.WEBSITE_TITLE:
                                MasterUrls[entry.Website] = AmazonUSA.AmazonUSALinks[0];
                                break;
                            case Indigo.WEBSITE_TITLE:
                                MasterUrls[entry.Website] = Indigo.IndigoLinks[0];
                                break;
                        }
                    }
                }
                Skip:
                ClearAllWebsiteData();
                if (IsDebugEnabled)
                {
                    using (StreamWriter outputFile = new(@"Data\MasterData.txt"))
                    {
                        if (MasterList.Count > 0)
                        {
                            foreach (EntryModel data in MasterList[0])
                            {
                                Logger.Debug(data.ToString());
                                outputFile.WriteLine(data.ToString());
                            }

                            foreach (string website in MasterUrls.Keys)
                            {
                                if (!string.IsNullOrWhiteSpace(MasterUrls[website]))
                                {
                                    Logger.Debug($"{website} -> {MasterUrls[website]}");
                                    outputFile.WriteLine($"{website} -> {MasterUrls[website]}");
                                }
                            }
                        }
                        else
                        {
                            Logger.Info("No MasterData Available");
                        }
                    }
                }
            });
        }
        
        private static async Task Main(string[] args)
        {
            Stopwatch watch = new();
            watch.Start();
            MasterScrape test = new();
            EnableDebugMode();
            // { Website.RightStufAnime, Website.BarnesAndNoble, Website.InStockTrades, Website.RobertsAnimeCornerStore, Website.KinokuniyaUSA, Website.BooksAMillion }
            await test.InitializeScrapeAsync("Toilet-bound Hanako-kun", Book.Manga, new string[] { }, new List<Website>() { Website.BarnesAndNoble }, "Chrome", false, false, false, false, false);
            watch.Stop();
            Logger.Info($"Time in Seconds: {(float)watch.ElapsedMilliseconds / 1000}s");
        }

        // public static void Main(string[] args)
        // {
            
        // }
    }
}