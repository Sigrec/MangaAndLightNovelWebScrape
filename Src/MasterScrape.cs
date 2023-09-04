using MangaLightNovelWebScrape.Websites;
using System.Diagnostics;

namespace MangaLightNovelWebScrape
{
    public partial class MasterScrape
    { 
        private static List<List<EntryModel>> MasterList = new();
        private static List<Thread> WebThreads = new();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("MasterScrapeLogs");
        public static bool IsDebugEnabled;
        [GeneratedRegex("[^\\w+]")] public static partial Regex RemoveNonWordsRegex();
        [GeneratedRegex("\\d{1,3}")] public static partial Regex FindVolNumRegex();
        
        private Thread CreateRightStufAnimeThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            Logger.Debug("RightStufAnime Going");
            return new Thread(() => MasterList.Add(RightStufAnime.GetRightStufAnimeData(bookTitle, bookType, false, 1, edgeOptions)));
        }

        private Thread CreateRobertsAnimeCornerStoreThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            Logger.Debug("RobertsAnimeCornerSTore Going");
            return new Thread(() => MasterList.Add(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData(bookTitle, bookType, edgeOptions)));
        }
        
        private Thread CreateInStockTradesThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            Logger.Debug("InStockTrades Going");
            return new Thread(() => MasterList.Add(InStockTrades.GetInStockTradesData(bookTitle, 1, bookType, edgeOptions)));
        }

        private Thread CreateKinokuniyaUSAThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            Logger.Debug("KinokuniyaUSA Going");
            return new Thread(() => MasterList.Add(KinokuniyaUSA.GetKinokuniyaUSAData(bookTitle, bookType, true, 1, edgeOptions)));
        }

        private Thread CreateBarnesAndNobleThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            Logger.Debug("Barnes & Noble Going");
            return new Thread(() => MasterList.Add(BarnesAndNoble.GetBarnesAndNobleData(bookTitle, bookType, false, 1, edgeOptions)));
        }

        private Thread CreateBooksAMillionThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            Logger.Debug("Books-A-Million Going");
            return new Thread(() => MasterList.Add(BooksAMillion.GetBooksAMillionData(bookTitle, bookType, true, 1, edgeOptions)));
        }

        private Thread CreateAmazonUSAThread(EdgeOptions edgeOptions, string bookTitle, char bookType)
        {
            Logger.Debug("AmazonUSA Going");
            return new Thread(() => MasterList.Add(AmazonUSA.GetAmazonUSAData(bookTitle, bookType, 1, edgeOptions)));
        }

        public static List<EntryModel> GetFinalList()
        {
            return MasterList[0];
        }

        public static void DisableDebugMode()
        {
            IsDebugEnabled = false;
        }

        public static void EnableDebugMode()
        {
            IsDebugEnabled = true;
        }

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
        public void InitializeScrape(string bookTitle, char bookType, List<Website> webScrapeList, string browser = "Error")
        {
            EdgeOptions edgeOptions = new()
            {
                PageLoadStrategy = PageLoadStrategy.Normal
            };
            edgeOptions.AddArgument("--headless=new");
            edgeOptions.AddArgument("--enable-automation");
            edgeOptions.AddArgument("--no-sandbox");
            edgeOptions.AddArgument("--disable-infobars");
            edgeOptions.AddArgument("--disable-dev-shm-usage");
            edgeOptions.AddArgument("--disable-browser-side-navigation");
            edgeOptions.AddArgument("--disable-gpu");
            edgeOptions.AddArgument("--disable-extensions");
            edgeOptions.AddArgument("--inprivate");
            edgeOptions.AddArgument("--incognito");
            
            foreach (Website site in webScrapeList)
            {
                switch (site)
                {
                    case Website.RightStufAnime:
                        WebThreads.Add(CreateRightStufAnimeThread(edgeOptions, bookTitle, bookType));
                        break;
                    case Website.RobertsAnimeCornerStore:
                        WebThreads.Add(CreateRobertsAnimeCornerStoreThread(edgeOptions, bookTitle, bookType));
                        break;
                    case Website.InStockTrades:
                        WebThreads.Add(CreateInStockTradesThread(edgeOptions, bookTitle, bookType));
                        break;
                    case Website.KinokuniyaUSA:
                        WebThreads.Add(CreateKinokuniyaUSAThread(edgeOptions, bookTitle, bookType));
                        break;
                    case Website.BarnesAndNoble:
                        WebThreads.Add(CreateBarnesAndNobleThread(edgeOptions, bookTitle, bookType));
                        break;
                    case Website.BooksAMillion:
                        WebThreads.Add(CreateBooksAMillionThread(edgeOptions, bookTitle, bookType));
                        break;
                    case Website.AmazonUSA:
                        WebThreads.Add(CreateAmazonUSAThread(edgeOptions, bookTitle, bookType));
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
                            //Logger.Debug("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        outputFile.WriteLine("No MasterData Available");
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
            test.InitializeScrape("Jujutsu Kaisen", 'M', new List<Website>() { Website.RightStufAnime, Website.BarnesAndNoble, Website.RobertsAnimeCornerStore, Website.InStockTrades }, "Edge");
            watch.Stop();
            Logger.Info($"Time in Seconds: {watch.ElapsedMilliseconds / 1000}s");
        }
    }
}