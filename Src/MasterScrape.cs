using System.Runtime.InteropServices.ComTypes;
using MangaLightNovelWebScrape.Websites;
using System.Diagnostics;

namespace MangaLightNovelWebScrape
{
    public partial class MasterScrape
    { 
        private static List<List<EntryModel>> MasterList = new();
        private static List<Thread> WebThreads = new();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("MasterScrapeLogs");
        /// <summary>
        /// Determines whether debug mode is enabled (Diabled by default)
        /// </summary>
        public static bool IsDebugEnabled { get; set; }
        /// <summary>
        /// The browser arguments used for each scrape
        /// </summary>
        private static readonly string[] BROWSER_ARGUMENTS = { "--headless=new", "--enable-automation", "--no-sandbox", "--disable-infobars", "--disable-dev-shm-usage", "--disable-popup-blocking", "--disable-extensions", "--inprivate", "--incognito" };
        [GeneratedRegex("[^\\w+]")] public static partial Regex RemoveNonWordsRegex();
        [GeneratedRegex("\\d{1,3}")] public static partial Regex FindVolNumRegex();

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
            RobertsAnimeCornerStore
        }

        public MasterScrape(bool IsDebugEnabled = false)
        {
        }

        private Thread CreateRightStufAnimeThread(EdgeOptions edgeOptions, string bookTitle, char bookType, bool isMember)
        {
            return new Thread(() => MasterList.Add(RightStufAnime.GetRightStufAnimeData(bookTitle, bookType, isMember, 1, edgeOptions)));
        }

        private Thread CreateRobertsAnimeCornerStoreThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            return new Thread(() => MasterList.Add(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData(bookTitle, bookType, edgeOptions)));
        }
        
        private Thread CreateInStockTradesThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            return new Thread(() => MasterList.Add(InStockTrades.GetInStockTradesData(bookTitle, 1, bookType, edgeOptions)));
        }

        private Thread CreateKinokuniyaUSAThread(EdgeOptions edgeOptions, string bookTitle, char bookType, bool isMember)
        {
            return new Thread(() => MasterList.Add(KinokuniyaUSA.GetKinokuniyaUSAData(bookTitle, bookType, isMember, 1, edgeOptions)));
        }

        private Thread CreateBarnesAndNobleThread(EdgeOptions edgeOptions, string bookTitle, char bookType, bool isMember)
        {
            return new Thread(() => MasterList.Add(BarnesAndNoble.GetBarnesAndNobleData(bookTitle, bookType, isMember, 1, edgeOptions)));
        }

        private Thread CreateBooksAMillionThread(EdgeOptions edgeOptions, string bookTitle, char bookType, bool isMember)
        {
            return new Thread(() => MasterList.Add(BooksAMillion.GetBooksAMillionData(bookTitle, bookType, isMember, 1, edgeOptions)));
        }

        private Thread CreateAmazonUSAThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            return new Thread(() => MasterList.Add(AmazonUSA.GetAmazonUSAData(bookTitle, bookType, 1, edgeOptions)));
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
        }

        /// <summary>
        /// Gets the results of a scrape
        /// </summary>
        /// <returns></returns>
        public List<EntryModel> GetResults()
        {
            return MasterList[0];
        }

        /// <summary>
        /// Starts the web scrape
        /// </summary>
        /// <param name="bookTitle">The title of the series to search for</param>
        /// <param name="bookType">The book type of the series either Manga (M) or Novel (N)</param>
        /// <param name="webScrapeList">The list of websites you want to search at</param>
        /// <param name="browser">The browser either Edge, Chrome,l or FireFox the user wants to use</param>
        /// <param name="isRightStufMember">Whether the user is a RightStuf Member</param>
        /// <param name="isBarnesAndNobleMember">Whether the user is a Barnes&Noble Member</param>
        /// <param name="isBooksAMillionMember">Whether the user is a BooksAMillion Member</param>
        public void InitializeScrape(string bookTitle, char bookType, List<Website> webScrapeList, string browser = "Error", bool isRightStufMember = false, bool isBarnesAndNobleMember = false, bool isBooksAMillionMember = false, bool isKinokuniyaUSAMember = false)
        {
            MasterList.Clear(); // Clear the masterlist everytime there is a new run
            EdgeOptions edgeOptions = new()
            {
                PageLoadStrategy = PageLoadStrategy.Eager,
            };
            edgeOptions.AddArguments(BROWSER_ARGUMENTS);
            
            foreach (Website site in webScrapeList)
            {
                switch (site)
                {
                    case Website.RightStufAnime:
                        WebThreads.Add(CreateRightStufAnimeThread(edgeOptions, bookTitle, bookType, isRightStufMember));
                        Logger.Debug("RightStufAnime Going");
                        break;
                    case Website.RobertsAnimeCornerStore:
                        WebThreads.Add(CreateRobertsAnimeCornerStoreThread(edgeOptions, bookTitle, bookType));
                        Logger.Debug("RobertsAnimeCornerStore Going");
                        break;
                    case Website.InStockTrades:
                        WebThreads.Add(CreateInStockTradesThread(edgeOptions, bookTitle, bookType));
                        Logger.Debug("InStockTrades Going");
                        break;
                    case Website.BarnesAndNoble:
                        WebThreads.Add(CreateBarnesAndNobleThread(edgeOptions, bookTitle, bookType, isBarnesAndNobleMember));
                        Logger.Debug("BarnesAndNoble Going");
                        break;
                    case Website.KinokuniyaUSA:
                        WebThreads.Add(CreateKinokuniyaUSAThread(edgeOptions, bookTitle, bookType, isKinokuniyaUSAMember));
                        Logger.Debug("KinokuniyaUSA Going");
                        break;
                    case Website.BooksAMillion:
                        WebThreads.Add(CreateBooksAMillionThread(edgeOptions, bookTitle, bookType, isBooksAMillionMember));
                        Logger.Debug("BooksAMillion Going");
                        break;
                    case Website.AmazonUSA:
                        WebThreads.Add(CreateAmazonUSAThread(edgeOptions, bookTitle, bookType));
                        Logger.Debug("AmazonUSA Going");
                        break;
                }
            }

            WebThreads.ForEach(web => web.Start());
            WebThreads.ForEach(web => web.Join());
            MasterList.RemoveAll(x => x.Count == 0); // Clear all lists from websites that didn't have any data
            WebThreads.Clear();

            int pos = 0; // The position of the new lists of data after comparing
            int checkTask;
            int threadCount = MasterList.Count / 2; // Tracks the "status" of the data lists that need to be compared, essentially tracks needed thread count
            Thread[] threadList = new Thread[threadCount];; // Holds the comparison threads for execution
            while (MasterList.Count > 1) // While there is still 2 or more lists of data to compare prices continue
            {
                MasterList.Sort((dataSet1, dataSet2) => dataSet1.Count.CompareTo(dataSet2.Count));
                for (int curTask = 0; curTask < MasterList.Count - 1; curTask += 2) // Create all of the Threads for compare processing
                {
                    checkTask = curTask;
                    threadList[pos] = new Thread(() => MasterList[pos] = EntryModel.PriceComparison(MasterList[checkTask], MasterList[checkTask + 1], bookTitle)); // Compare (SmallerList, BiggerList)
                    threadList[pos].Start();
                    threadList[pos].Join();
                    // Logger.Debug("POSITION = " + pos);
                    pos++;
                }
                

                if (MasterList.Count % 2 != 0)
                {
                    Logger.Debug("Odd Thread Check");
                    MasterList[pos] = MasterList[^1];
                    pos++;
                }

                // MasterList[MasterList.Count - 1].ForEach(data => Logger.Info("List 1 [" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]"));
                // MasterList[0].ForEach(data => Logger.Info("List 0 [" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]"));
                // Logger.Debug("Current Pos = " + pos);
                MasterList.RemoveRange(pos, MasterList.Count - pos); // Shrink List
                // Check if the master data list MasterList[0] is the only list left -> comparison is done 
                if (MasterList.Count != 1 && threadCount != MasterList.Count / 2)
                {
                    threadCount = MasterList.Count / 2;
                    threadList = new Thread[threadCount];
                }
                pos = 0;
            }

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
                    }
                    else
                    {
                        Logger.Info("No MasterData Available");
                    }
                }
            }
        }

        private static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            MasterScrape test = new MasterScrape();
            EnableDebugMode();
            test.InitializeScrape("Dark Gathering", 'M', new List<Website>() { Website.KinokuniyaUSA }, "Edge", false, true, true, true);
            watch.Stop();
            Logger.Info($"Time in Seconds: {watch.ElapsedMilliseconds / 1000}s");
        }
    }
}