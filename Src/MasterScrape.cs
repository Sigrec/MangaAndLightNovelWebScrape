using MangaLightNovelWebScrape.Src.Websites;
using System.Diagnostics;

namespace MangaLightNovelWebScrape
{
    public class MasterScrape
    { 
        private static List<List<string[]>> MasterList = new List<List<string[]>>();
        private static List<Thread> WebThreads = new List<Thread>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("MasterScrapeLogs");
        private static string bookTitle;
        private static char bookType;

        public static string RemoveInPlaceCharArray(string input)
		{
			var len = input.Length;
			var src = input.ToCharArray();
			int dstIdx = 0;
			for (int i = 0; i < len; i++)
			{
				var ch = src[i];
				switch (ch)
				{
					case '\u0020':
					case '\u00A0':
					case '\u1680':
					case '\u2000':
					case '\u2001':
					case '\u2002':
					case '\u2003':
					case '\u2004':
					case '\u2005':
					case '\u2006':
					case '\u2007':
					case '\u2008':
					case '\u2009':
					case '\u200A':
					case '\u202F':
					case '\u205F':
					case '\u3000':
					case '\u2028':
					case '\u2029':
					case '\u0009':
					case '\u000A':
					case '\u000B':
					case '\u000C':
					case '\u000D':
					case '\u0085':
						continue;
					default:
						src[dstIdx++] = ch;
						break;
				}
			}
			return new string(src, 0, dstIdx);
		}

        /********************************************************************************************************************************************
        * Last Modified On: 06 MArch, 2023
        *  by: Sigrec (Sean. N)
        * Description: Compares the titles of various entries for a series against each other to determine if they are similar enough to be cosnidered equal,
        *              allowing comparisons of titles who are technically the same but there websites have slightly different formats.
        *              It determine if they are similar enough by a threshold of 1/4 the size of longest title being the same, meaning based on the
        *              number of characters that do not match if it's is less than 1/4 the size of the longest title string.
        * Parameters:
        *      titleOne | string | 1 of the titles in the comparison and is used for determining when to stop traversing
        *      titleTwo | string | 1 of the titles in the comparison
        ********************************************************************************************************************************************/
        public static bool Similar(string titleOne, string titleTwo)
        {
            int count = 0; // The amount of times that the characters and there "alignment" don't match
            int titleOnePointer = 0, titleTwoPointer = 0; // Pointers for the characters in both strings
            int tracker = 0; // Tracks the current position and determiens whether to go back 1 char in titleOne to recompare with titleTwo's next char
            titleOne = RemoveInPlaceCharArray(titleOne.ToLower());
            titleTwo = RemoveInPlaceCharArray(titleTwo.ToLower());

            while (titleOnePointer < titleOne.Length && titleTwoPointer < titleTwo.Length) // Keep traversing until u reach the end of titleOne's string
            {
                // Logger.Debug("O " + titleOnePointer + "(" + titleOne[titleOnePointer] + ")" + "\t" + titleTwoPointer + "(" + titleTwo[titleTwoPointer] + ")");
                if (titleOne[titleOnePointer] != titleTwo[titleTwoPointer]) // Checks to see if the characters match
                {
                    int cache = titleOne.IndexOf(titleOne[titleOnePointer]) + 1;
                    // Logger.Debug("Current Cache Size = " + cache);
                    count++; // There is 1 additional character difference
                    for (int z = cache; z < titleOne.Length; z++) // Start at the index of where the characters were not the same, then traverse the other string to see if it matches
                    {
                        // Logger.Debug("I " + z + "(" + titleOne[z] + ")" + "\t" + titleTwoPointer + "(" + titleTwo[titleTwoPointer] + ")");
                        // Logger.Debug("S " + (z - 1) + "(" + titleOne[z - 1] + ")" + "\t" + (titleTwoPointer - 1) + "(" + titleTwo[(titleTwoPointer - 1 < 0 ? titleTwoPointer : titleTwoPointer - 1)] + ")");
                        tracker = z;
                        titleOnePointer++;
                        if (titleOne[z] == titleTwo[titleTwoPointer] && titleOne[z - 1] == titleTwo[(titleTwoPointer - 1 < 0 ? titleTwoPointer : titleTwoPointer - 1)]) // Checks to see if the character is present in the other string and is in a similar position
                        {
                            break;
                        }
                    }
                    titleOnePointer = cache;
                    //Logger.Debug("Count = " + count);
                } 
                else // Characters do match so just move to the next set of characters to compare in the strings
                {
                    titleOnePointer++;
                }

                if (tracker + 1 == titleOne.Length)
                {
                    titleOnePointer--;
                    tracker = 0;
                }
                titleTwoPointer++;
            }

            // Logger.Debug("Count = " + count);
            // Logger.Debug(count <= (titleOne.Length > titleTwo.Length ? titleTwo.Length / 4 : titleOne.Length / 4));
            return count <= (titleOne.Length > titleTwo.Length ? titleTwo.Length / 5 : titleOne.Length / 5); // Determine if they are similar enough by a threshold of 1/4 the size of longest title
        }

        /********************************************************************************************************************************************
        * Last Modified On: 03 MArch, 2023
        *  by: Sigrec (Sean. N)
        * Description: Gets the current volume num for a series unit entry givin its type (box set, omnibux, single, etc)
        * Parameters:
        *      title | string | the full title of the entry to get the volume number
        *      type | string | the type of entry it is either box set, omnibus, or single
        ********************************************************************************************************************************************/
        private static int GetCurrentVolumeNum(String title, String type){
            return Int32.Parse(new Regex(@".*?(\d+).*").Replace(title.Substring(title.IndexOf(" "+ type) + type.Length + 1), "$1", 1));
        }

        /********************************************************************************************************************************************
        * Modified On: 03 March, 2023
        *  by: Sigrec (Sean. N)
        * Description: Compares the prices of all the volumes that the two websites both have, and outputs the resulting list containing
        *              the lowest prices for each available volume between the websites. If one website does not have a volume that the other
        *              does then that volumes data set defaults to the "smallest" and is added to the list.
        * Parameters:
        *      biggerList | string[] | The bigger list of data sets between the two websites
        *      smallerList | string[] | The smaller list of data sets between the two websites
        *      return | List<string[]> | The final list of data containing all available lowest price volumes between the two websites
        ********************************************************************************************************************************************/
        private static List<string[]> PriceComparison(List<string[]> smallerList, List<string[]> biggerList, String bookTitle)
        {
            List<string[]> finalData = new List<string[]>();   // The final list of data containing all available volumes for the series from the website with the lowest price
            bool sameVolumeCheck;                           // Determines whether a match has been found where the 2 volumes are the same to compare prices for
            int nextVolPos = 0;                                       // The position of the next volume and then proceeding volumes to check if there is a volume to compare
            int biggerListCurrentVolNum;                              // The current vol number from the website with the bigger list of volumes that is being checked

            foreach (string[] biggerListData in biggerList){
                sameVolumeCheck = false; // Reset the check to determine if two volumes with the same number has been found to false
                if (biggerListData[0].Contains("Imperfect")) // Skip comparing price data for a volumes that are not new
                {
                    continue;
                }
                else if (biggerListData[0].Contains("Box Set"))
                {
                    biggerListCurrentVolNum = GetCurrentVolumeNum(biggerListData[0], "Box Set");
                } 
                else
                {
                    biggerListCurrentVolNum = GetCurrentVolumeNum(biggerListData[0], "Vol");
                }

                if (nextVolPos != smallerList.Count) // Only need to check for a comparison if there are still volumes to compare in the "smallerList"
                {
                    for (int y = nextVolPos; y < smallerList.Count; y++) // Check every volume in the smaller list, skipping over volumes that have already been checked
                    { 
                        // Check to see if the titles are not the same and they are not similar enough, or it is not new then go to the next volume
                        if (smallerList[y][0].Contains("Imperfect") || (!smallerList[y][0].Equals(biggerListData[0]) && !Similar(smallerList[y][0], biggerListData[0])))
                        {
                            // Logger.Debug($"Not The Same {smallerList[y][0]} | {biggerListData[0]} | {!smallerList[y][0].Equals(biggerListData[0])} | {!Similar(smallerList[y][0], biggerListData[0])} | {smallerList[y][0].Contains("Imperfect")}");
                            continue;
                        }
                        // If the vol numbers are the same and the titles are similar or the same from the if check above, add the lowest price volume to the list
                        
                        // Logger.Debug($"MATCH? ({biggerListCurrentVolNum}, {(biggerListData[0].Contains("Box Set") ? GetCurrentVolumeNum(smallerList[y][0], "Box Set") : GetCurrentVolumeNum(smallerList[y][0], "Vol"))}) = {biggerListCurrentVolNum == (biggerListData[0].Contains("Box Set") ? GetCurrentVolumeNum(smallerList[y][0], "Box Set") : GetCurrentVolumeNum(smallerList[y][0], "Vol"))}");
                        if (biggerListCurrentVolNum == (biggerListData[0].Contains("Box Set") ? GetCurrentVolumeNum(smallerList[y][0], "Box Set") : GetCurrentVolumeNum(smallerList[y][0], "Vol")))
                        {
                            // Logger.Debug($"Found Match for {biggerListData[0]} {biggerListCurrentVolNum}");
                            // Logger.Debug($"PRICE COMPARISON ({float.Parse(biggerListData[1].Substring(1))}, {float.Parse(smallerList[y][1].Substring(1))}) -> {float.Parse(biggerListData[1].Substring(1)) > float.Parse(smallerList[y][1].Substring(1))}");
                            // Get the lowest price between the two then add the lowest dataset
                            if (float.Parse(biggerListData[1].Substring(1)) > float.Parse(smallerList[y][1].Substring(1)))
                            {
                                finalData.Add(smallerList[y]);
                                // Logger.Debug($"Add Match [{smallerList[y][0]}, {smallerList[y][1]}, {smallerList[y][2]}, {smallerList[y][3]}]");
                            }
                            else
                            {
                                finalData.Add(biggerListData);
                                // Logger.Debug($"Add Match [{biggerListData[0]}, {biggerListData[1]}, {biggerListData[2]}, {biggerListData[3]}]");
                            }
                            smallerList.RemoveAt(y);
                            // Logger.Debug($"Add [{biggerListData[0]}, {biggerListData[1]}, {biggerListData[2]}, {biggerListData[3]}]");

                            nextVolPos = y; // Shift the position in which the next volumes to compare from the smaller list starts essentially "shrinking" the number of comparisons needed whenever a valid comparison is found by 1

                            sameVolumeCheck = true;
                            break;
                        }
                    }
                }

                if (!sameVolumeCheck) // If the current volume number in the bigger list has no match in the smaller list (same volume number and name) then add it
                {
                    //Logger.Debug($"Add No Match [{biggerListData[0]}, {biggerListData[1]}, {biggerListData[2]}, {biggerListData[3]}]");
                    finalData.Add(biggerListData);
                }
            }

            // Logger.Debug("SmallerList Size = " + smallerList.Count);
            // Smaller list has volumes that are not present in the bigger list and are volumes that have a volume # greater than the greatest volume # in the bigger lis
            for (int x = 0; x < smallerList.Count; x++){
                //Logger.Debug($"Add SmallerList Leftovers [{smallerList[x][0]}, {smallerList[x][1]}, {smallerList[x][2]}, {smallerList[x][3]}]");
                finalData.Add(smallerList[x]);
            }
            finalData.Sort(new VolumeSort(bookTitle));
            //finalData.ForEach(data => Logger.Info("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]"));
            return finalData;
        }
        
        private static Thread CreateRightStufAnimeThread(EdgeOptions edgeOptions)
        {
            Logger.Debug("RightStufAnime Going");
            return new Thread(() => MasterList.Add(RightStufAnime.GetRightStufAnimeData(bookTitle, bookType, false, 1, edgeOptions)));
        }

        private static Thread CreateRobertsAnimeCornerStoreThread(EdgeOptions edgeOptions)
        {
            Logger.Debug("RobertsAnimeCornerSTore Going");
            return new Thread(() => MasterList.Add(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData(bookTitle, bookType, edgeOptions)));
        }
        
        private static Thread CreateInStockTradesThread(EdgeOptions edgeOptions)
        {
            Logger.Debug("InStockTrades Going");
            return new Thread(() => MasterList.Add(InStockTrades.GetInStockTradesData(bookTitle, 1, bookType, edgeOptions)));
        }

        private static Thread CreateKinokuniyaUSAThread(EdgeOptions edgeOptions)
        {
            Logger.Debug("KinokuniyaUSA Going");
            return new Thread(() => MasterList.Add(KinokuniyaUSA.GetKinokuniyaUSAData(bookTitle, bookType, true, 1, edgeOptions)));
        }

        private static Thread CreateBarnesAndNobleThread(EdgeOptions edgeOptions)
        {
            Logger.Debug("Barnes & Noble Going");
            return new Thread(() => MasterList.Add(BarnesAndNoble.GetBarnesAndNobleData(bookTitle, bookType, false, 1, edgeOptions)));
        }

        private static Thread CreateBooksAMillionThread(EdgeOptions edgeOptions)
        {
            Logger.Debug("Books-A-Million Going");
            return new Thread(() => MasterList.Add(BooksAMillion.GetBooksAMillionData(bookTitle, bookType, true, 1, edgeOptions)));
        }

        private static Thread CreateAmazonUSAThread(EdgeOptions edgeOptions)
        {
            Logger.Debug("AmazonUSA Going");
            return new Thread(() => MasterList.Add(AmazonUSA.GetAmazonUSAData(bookTitle, bookType, 1, edgeOptions)));
        }

        static void Main(string[] args)
        {
            // Console.Write("What is the Manga/Light Novel Title: ");
            // bookTitle = Console.ReadLine();
            
            // Console.Write("Are u searching for a Manga (M) or Light Novel (N): ");
            // bookType = char.Parse(Console.ReadLine());

            // Logger.Debug(Similar("One Piece Omnibus Vol 3", "One Piece Vol 3") + " | " + "One Piece Omnibus Vol 3".CompareTo("One Piece Vol 3"));
        
            bookTitle = "one piece";
            bookType = 'M';

            Stopwatch watch = new Stopwatch();
            watch.Start();

            EdgeOptions edgeOptions = new EdgeOptions();
            edgeOptions.PageLoadStrategy = PageLoadStrategy.Eager;
            // edgeOptions.AddArgument("headless");
            edgeOptions.AddArgument("enable-automation");
            edgeOptions.AddArgument("no-sandbox");
            edgeOptions.AddArgument("disable-infobars");
            edgeOptions.AddArgument("disable-dev-shm-usage");
            edgeOptions.AddArgument("disable-browser-side-navigation");
            edgeOptions.AddArgument("disable-gpu");
            edgeOptions.AddArgument("disable-extensions");
            edgeOptions.AddArgument("inprivate");
            edgeOptions.AddArgument("incognito");

            // WebThreads.Add(CreateRightStufAnimeThread(edgeOptions));
            // WebThreads.Add(CreateRobertsAnimeCornerStoreThread(edgeOptions));
            // WebThreads.Add(CreateInStockTradesThread(edgeOptions));
            // WebThreads.Add(CreateKinokuniyaUSAThread(edgeOptions));
            // WebThreads.Add(CreateBarnesAndNobleThread(edgeOptions));
            // WebThreads.Add(CreateBooksAMillionThread(edgeOptions));
            WebThreads.Add(CreateAmazonUSAThread(edgeOptions));
            
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
                    threadList[pos] = new Thread(() => MasterList[pos] = PriceComparison(MasterList[checkTask], MasterList[checkTask + 1], bookTitle)); // Compare (SmallerList, BiggerList)
                    threadList[pos].Start();
                    threadList[pos].Join();
                    // Logger.Debug("POSITION = " + pos);
                    pos++;
                }
                

                if (MasterList.Count % 2 != 0)
                {
                    Logger.Debug("Odd Thread Check");
                    MasterList[pos] = MasterList[MasterList.Count - 1];
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

            watch.Stop();
            Logger.Info($"Time in Seconds: {(long)watch.ElapsedMilliseconds / 1000}s");


            using (StreamWriter outputFile = new StreamWriter(@"Data\MasterData.txt"))
            {
                if (MasterList.Count > 0)
                {
                    foreach (string[] data in MasterList[0])
                    {
                        //Logger.Debug("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else
                {
                    outputFile.WriteLine("No MasterData Available");
                }
            }
        }
    }

    public class VolumeSort : IComparer<string[]>
    {
        string bookTitle;

        public VolumeSort(string bookTitle)
        {
            this.bookTitle = bookTitle;
        }

        public int Compare(string[] vol1, string[] vol2)
        {
            int val1 = ExtractInt(vol1[0]);
            int val2 = ExtractInt(vol2[0]);
            if (string.Equals(Regex.Replace(vol1[0], @" Vol \d+$", ""), Regex.Replace(vol2[0], @" Vol \d+$", "")) || MasterScrape.Similar(vol1[0], vol2[0]))
            {
                if (val1 > val2)
                {
                    return 1;
                }
                else if (val1 < val2)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
            return vol1[0].CompareTo(vol2[0]);
        }

        int ExtractInt(String s)
        {
            return Int32.Parse(Regex.Replace(s.Substring(bookTitle.Length), @".*( \d+)$", "$1").TrimStart());
        }
    }
}