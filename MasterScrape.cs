using System;
using MangaWebScrape.Websites;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace MangaWebScrape
{
    class MasterScrape
    { 
        private static List<List<string[]>> MasterList = new List<List<string[]>>();
        private static List<Thread> WebThreads = new List<Thread>();
        private static string bookTitle;
        private static char bookType;

        private static bool Similar(string titleOne, string titleTwo)
        {
            int count = 0; // The amount of times that the characters and there "alignment" don't match
            int titleOnePointer = 0, titleTwoPointer = 0; // Pointers for the characters in both strings

            while (titleOnePointer < titleOne.Length && titleTwoPointer < titleTwo.Length) // Keep traversing until u reach the end of titleOne's string
            {
                //Console.WriteLine(titleOnePointer + "(" + titleOne[titleOnePointer] + ")" + "\t" + titleTwoPointer + "(" + titleTwo[titleTwoPointer] + ")");
                if (titleOne[titleOnePointer] != titleTwo[titleTwoPointer]) // Checks to see if the characters match
                {
                    int cache = titleOne.IndexOf(titleOne[titleOnePointer]) + 1;
                    for (int z = cache; z < titleOne.Length; z++) // Start at the index of where the characters were not the same, then traverse the other string to see if it matches
                    {
                        //Console.WriteLine(z + "(" + titleOne[z] + ")" + "\t" + titleTwoPointer + "(" + titleTwo[titleTwoPointer] + ")");
                        titleOnePointer++;
                        if (titleOne[z] == titleTwo[titleTwoPointer] && titleOne[z - 1] == titleTwo[titleTwoPointer - 1]) // Checks to see if the character is present in the other string and is in a similar position
                        {
                            break;
                        }
                    }
                    count++; // There is 1 additional character difference
                    titleOnePointer = cache;
                    //Console.WriteLine("Count = " + count);
                } 
                else // Characters do match so just move to the next set of characters to compare in the strings
                {
                    titleOnePointer++;
                }
                titleTwoPointer++;
            }

            // Console.WriteLine("Count = " + count);
            // Console.WriteLine(count <= (titleOne.Length > titleTwo.Length ? titleTwo.Length / 4 : titleOne.Length / 4));
            return count <= (titleOne.Length > titleTwo.Length ? titleTwo.Length / 4 : titleOne.Length / 4); // Determine if they are similar enough by a threshold of 1/4 the size of longest title
        }

        private static int GetCurrentVolumeNum(String title, String type){
            return Int32.Parse(new Regex(@".*?(\d+).*").Replace(title.Substring(title.IndexOf(" "+ type) + type.Length + 1), "$1", 1));
        }

        /**
        * Modified On: 02 December 2021
        *  by: Sean Njenga
        * Description: Compares the prices of all the volumes that the two websites both have, and outputs the resulting list containing
        *              the lowest prices for each available volume between the websites. If one website does not have a volume that the other
        *              does then that volumes data set defaults to the "smallest" and is added to the list.
        * Parameters:
        *      biggerList | string[] | The bigger list of data sets between the two websites
        *      smallerList | string[] | The smaller list of data sets between the two websites
        *      return | List<string[]> | The final list of data containing all available lowest price volumes between the two websites
        */
        private static List<string[]> PriceComparison(List<string[]> smallerList, List<string[]> biggerList, String bookTitle)
        {
            List<string[]> finalData = new List<string[]>();   // The final list of data containing all available volumes for the series from the website with the lowest price
            bool sameVolumeCheck;                           // Determines whether a match has been found where the 2 volumes are the same to compare prices for
            int pos = 0;                                       // The position of the next volume and then proceeding volumes to check if there is a volume to compare
            int getListOneVolNum;                              // The current vol number from the website with the bigger list of volumes that is being checked
            string[] smallerListData;                          // The current volume data set that is being compared against from the smaller data list

            foreach (string[] biggerListData in biggerList){
                sameVolumeCheck = false; // Reset the check to determine if two volumes with the same number has been found to false
                if (biggerListData[0].Contains("Imperfect")) // Skip comparing price data for a volumes that are not new
                {
                    continue;
                }
                else if (biggerListData[0].Contains("Box Set"))
                {
                    getListOneVolNum = GetCurrentVolumeNum(biggerListData[0], "Box Set");
                } 
                else
                {
                    getListOneVolNum = GetCurrentVolumeNum(biggerListData[0], "Vol");
                }

                if (pos != smallerList.Count) // Only need to check for a comparison if there are still volumes to compare in the "smallerList"
                { 
                    for (int y = pos; y < smallerList.Count; y++) // Check every volume in the smaller list, skipping over volumes that have already been checked
                    { 
                        smallerListData = smallerList[y];

                        // Check to see if the titles are the same and if not, if they are similar enough, if they aren't similar enough then go to the next volume and whether the volume is new
                        if ((!smallerListData[0].Equals(biggerListData[0]) && !Similar(smallerListData[0], biggerListData[0])) || smallerListData[0].Contains("Imperfect"))
                        {
                            continue;
                        }

                        // If the vol numbers are the same and the titles are similar or the same from the if check above, add the lowest price volume to the list
                        if (getListOneVolNum == (biggerListData[0].Contains("Box Set") ? GetCurrentVolumeNum(smallerListData[0], "Box Set") : GetCurrentVolumeNum(smallerListData[0], "Vol")))
                        {
                            //Console.WriteLine("Found Match for BoxSet/Vol " + getListOneVolNum);
                            // Get the lowest price between the two then add the lowest dataset
                            finalData.Add(float.Parse(biggerListData[1].Substring(1)) > float.Parse(smallerListData[1].Substring(1)) ? smallerListData : biggerListData);

                            pos = y + 1; // Increment the position in which the next volumes to compare from the smaller list starts essentially "shrinking" the number of comparisons needed whenever a valid comparison is found by 1

                            sameVolumeCheck = true;
                            break;
                        }
                    }
                }

                if (!sameVolumeCheck) // If the current volume number in the bigger list has no match in the smaller list (same volume number and name) then add it
                {
                    finalData.Add(biggerListData);
                }
            }

            if (pos != smallerList.Count) // Smaller list has volumes that are not present in the bigger list and are volumes that have a volume # greater than the greatest volume # in the bigger list
            {
                for (int x = pos; x < smallerList.Count; x++){
                    finalData.Add(smallerList[x]);
                }
            }
            return finalData;
        }

        private static Thread CreateRightStufAnimeThread(EdgeOptions edgeOptions)
        {
            return new Thread(() => MasterList.Add(RightStufAnime.GetRightStufAnimeData(bookTitle, bookType, true, 1, edgeOptions)));
        }

        private static Thread CreateRobertsAnimeCornerStoreThread(EdgeOptions edgeOptions)
        {
            return new Thread(() => MasterList.Add(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData(bookTitle, bookType, edgeOptions)));
        }
        
        private static Thread CreateInStockTradesThread(EdgeOptions edgeOptions)
        {
            return new Thread(() => MasterList.Add(InStockTrades.GetInStockTradesData(bookTitle, 1, edgeOptions)));
        }

        static void Main(string[] args)
        {
            System.Environment.SetEnvironmentVariable("webdriver.edge.driver", @"DriverExecutables/msedgedriver.exe");
            Console.Write("What is the Manga/Light Novel Title: ");
            bookTitle = Console.ReadLine();
            
            Console.Write("Are u searching for a Manga (M) or Light Novel (N): ");
            bookType = char.Parse(Console.ReadLine());

            Stopwatch watch = new Stopwatch();
            watch.Start();

            EdgeOptions edgeOptions = new EdgeOptions();
            edgeOptions.PageLoadStrategy = PageLoadStrategy.Eager;
            edgeOptions.AddArguments("headless");
		    edgeOptions.AddArguments("enable-automation");
		    edgeOptions.AddArguments("no-sandbox");
		    edgeOptions.AddArguments("disable-infobars");
		    edgeOptions.AddArguments("disable-dev-shm-usage");
		    edgeOptions.AddArguments("disable-browser-side-navigation");
		    edgeOptions.AddArguments("disable-gpu");
		    edgeOptions.AddArguments("disable-extensions");
		    edgeOptions.AddArguments("inprivate");

            WebThreads.Add(CreateRightStufAnimeThread(edgeOptions));
            WebThreads.Add(CreateRobertsAnimeCornerStoreThread(edgeOptions));
            //WebThreads.Add(CreateInStockTradesThread(edgeOptions));
            foreach(Thread t in WebThreads){
                t.Start();
            }
            foreach(Thread t in WebThreads)
            {
                t.Join();
            }
            WebThreads.Clear();
            MasterList.Sort((dataSet1, dataSet2) => dataSet1.Count.CompareTo(dataSet2.Count));

            int pos = 0; // The position of the new lists of data after comparing
            int threadCount = MasterList.Count % 2 == 0 ? MasterList.Count : MasterList.Count - 1; // Tracks the "status" of the data lists that need to be compared, essentially tracks needed thread count
            Thread[] threadList; // Holds the comparison threads for execution
            while (threadCount > 1) // While there is still 2 or more lists of data to compare prices
            {
                threadList = new Thread[threadCount / 2];
                for (int curTask = 0; curTask <= threadList.Length; curTask+=2) // Create all of the Threads for compare processing
                {
                    int curPos = pos;
                    int checkTask = curTask;
                    threadList[curPos] = new Thread(() => MasterList[curPos] = PriceComparison(MasterList[checkTask], MasterList[checkTask + 1], bookTitle)); // Compare (SmallerList, BiggerList) where the longerlist has more elements than the smaller list
                    threadList[curPos].Start();
                    pos++;
                }

                // Wait until all of the price comparisons are finished before doing another set of comparisons
                foreach(Thread compareThread in threadList)
                {
                    compareThread.Join();
                }

                // If there are an odd number of threads left then after calculations the dangling thread/website that didn't get compared moves forward for easier comparison later
                if (threadCount % 2 != 0)
                {
                    MasterList[pos] = MasterList[MasterList.Count - 1];
                }

                // Shrink List
                MasterList.RemoveRange(++pos, MasterList.Count - pos);
                // Check if list is odd
                threadCount = threadCount % 2 == 0 ? threadCount /= 2 : threadCount -= 1;
                pos = 0;
            }

            watch.Stop();
            Console.WriteLine($"Time in Seconds: {(long)watch.ElapsedMilliseconds / 1000}s");

            using (StreamWriter outputFile = new StreamWriter(@"Data\MasterData.txt"))
            {
                foreach (string[] data in MasterList[0]){
                    outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                }
            }
        }
    }

    public class VolumeSort : IComparer<string[]>
    {
        string bookTitle;

        public VolumeSort(string bookTitle){
            this.bookTitle = bookTitle;
        }

        public int Compare(string[] vol1, string[] vol2) {
            if (string.Equals(Regex.Replace(vol1[0], @" \d+$", ""), Regex.Replace(vol2[0], @" \d+$", ""), StringComparison.OrdinalIgnoreCase)) {
                return ExtractInt(vol1[0]) - ExtractInt(vol2[0]);
            }
            return vol1[0].CompareTo(vol2[0]);
        }

        int ExtractInt(String s) {
            return Int32.Parse(Regex.Replace(s.Substring(bookTitle.Length), @".*( \d+)$", "$1").TrimStart());
        }
    }
}