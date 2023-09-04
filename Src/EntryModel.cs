namespace MangaLightNovelWebScrape
{
    public class EntryModel
    {
        public string Entry { get; set; }
        public string Price { get; set; }
        public string StockStatus { get; set; }
        public string  Website { get; set; }

        public EntryModel (string entry, string price, string stockStatus, string website)
        {
            Entry = entry;
            Price = price;
            StockStatus = stockStatus;
            Website = website;
        }

        public override string ToString()
        {
            return $"[{Entry}, {Price}, {StockStatus}, {Website}]";
        }

        /********************************************************************************************************************************************
        * Last Modified On: 03 MArch, 2023
        *  by: Sigrec (Sean. N)
        * Description: Gets the current volume num for a series unit entry givin its type (box set, omnibux, single, etc)
        * Parameters:
        *      title | string | the full title of the entry to get the volume number
        *      type | string | the type of entry it is either box set, omnibus, or single
        ********************************************************************************************************************************************/
        public static int GetCurrentVolumeNum(String title, String type)
        {
            return Int32.Parse(new Regex(@".*?(\d+).*").Replace(title.Substring(title.IndexOf(" "+ type) + type.Length + 1), "$1", 1));
        }

        /// <summary>
        /// Last Modified: 06 March, 2023
        /// Author: Sigrec (Sean. N)
        /// Compares the titles of various entries for a series against each other to determine if they are similar enough to be cosnidered equal,
        /// allowing comparisons of titles who are technically the same but there websites have slightly different formats.
        /// It determine if they are similar enough by a threshold of 1/4 the size of longest title being the same, meaning based on the
        /// number of characters that do not match if it's is less than 1/4 the size of the longest title string.
        /// </summary>
        /// <param name="titleOne">The first title in the comparison and is used for determining when to stop traversing</param>
        /// <param name="titleTwo">The 2nd title in the comparison</param>
        /// <returns></returns>
        public static bool Similar(string titleOne, string titleTwo)
        {
            int count = 0; // The amount of times that the characters and there "alignment" don't match
            int titleOnePointer = 0, titleTwoPointer = 0; // Pointers for the characters in both strings
            titleOne = RemoveInPlaceCharArray(titleOne.ToLower());
            titleTwo = RemoveInPlaceCharArray(titleTwo.ToLower());

            while (titleOnePointer < titleOne.Length && titleTwoPointer < titleTwo.Length) // Keep traversing until u reach the end of titleOne's string
            {
                // Logger.Debug("O " + titleOnePointer + "(" + titleOne[titleOnePointer] + ")" + "\t" + titleTwoPointer + "(" + titleTwo[titleTwoPointer] + ")");
                if (titleOne[titleOnePointer] != titleTwo[titleTwoPointer]) // Checks to see if the characters match
                {
                    // Logger.Debug($"{titleOne[titleOnePointer]},{titleOnePointer} | {titleTwo[titleTwoPointer]},{titleTwoPointer}");
                    count++; // There is 1 additional character difference
                    for (int z = titleOnePointer; z < titleOne.Length; z++) // Start at the index of where the characters were not the same, then traverse the other string to see if it matches
                    {
                        // Logger.Debug("I " + z + "(" + titleOne[z] + ")" + "\t" + titleTwoPointer + "(" + titleTwo[titleTwoPointer] + ")");
                        if (titleOne[z] == titleTwo[titleTwoPointer]) // Checks to see if the character is present in the other string and is in a similar position
                        {
                            break;
                        }
                    }
                    // Logger.Debug("Current Cache Size After = " + cache);
                    // Logger.Debug("Count = " + count);
                } 
                else // Characters do match so just move to the next set of characters to compare in the strings
                {
                    titleOnePointer++;
                }
                titleTwoPointer++;
            }

            // Logger.Debug("Count = " + count);
            // Logger.Debug((count <= (titleOne.Length > titleTwo.Length ? titleTwo.Length / 5 : titleOne.Length / 5)) ? $"{titleOne} is Similar to {titleTwo}" : $"{titleOne} is Not Similar to {titleTwo}");
            return count <= (titleOne.Length > titleTwo.Length ? titleTwo.Length / 5 : titleOne.Length / 5); // Determine if they are similar enough by a threshold of 1/5 the size of longest title
        }

       /// <summary>
       /// Last Modified: 03 March, 2023
       /// Author: Sigrec (Sean. N)
       /// Compares the prices of all the volumes that the two websites both have, and outputs the resulting list containing 
       /// the lowest prices for each available volume between the websites. If one website does not have a volume that the other
       /// does then that volumes data set defaults to the "smallest" and is added to the list.
       /// </summary>
       /// <param name="smallerList">The smaller list of data sets between the two websites</param>
       /// <param name="biggerList">The bigger list of data sets between the two websites</param>
       /// <param name="bookTitle">The initial title inputted by the user used to determine if the titles in the lists "match"</param>
       /// <returns>The final list of data containing all available lowest price volumes between the two websites</returns>
       /// TODO Need to figure out what to do if the prices are the same, most likely at the end the site with the most entries is favored?
        public static List<EntryModel> PriceComparison(List<EntryModel> smallerList, List<EntryModel> biggerList, String bookTitle)
        {
            List<EntryModel> finalData = new List<EntryModel>();   // The final list of data containing all available volumes for the series from the website with the lowest price
            bool sameVolumeCheck;                                  // Determines whether a match has been found where the 2 volumes are the same to compare prices for
            int nextVolPos = 0;                                    // The position of the next volume and then proceeding volumes to check if there is a volume to compare
            int biggerListCurrentVolNum;                           // The current vol number from the website with the bigger list of volumes that is being checked

            foreach (EntryModel biggerListData in biggerList){
                sameVolumeCheck = false; // Reset the check to determine if two volumes with the same number has been found to false
                if (biggerListData.Entry.Contains("Imperfect")) // Skip comparing price data for a volumes that are not new
                {
                    continue;
                }
                else if (biggerListData.Entry.Contains("Box Set"))
                {
                    biggerListCurrentVolNum = GetCurrentVolumeNum(biggerListData.Entry, "Box Set");
                } 
                else
                {
                    biggerListCurrentVolNum = GetCurrentVolumeNum(biggerListData.Entry, "Vol");
                }

                if (nextVolPos != smallerList.Count) // Only need to check for a comparison if there are still volumes to compare in the "smallerList"
                {
                    for (int y = nextVolPos; y < smallerList.Count; y++) // Check every volume in the smaller list, skipping over volumes that have already been checked
                    { 
                        // Check to see if the titles are not the same and they are not similar enough, or it is not new then go to the next volume
                        if (smallerList[y].Entry.Contains("Imperfect") || (!smallerList[y].Entry.Equals(biggerListData.Entry) && !Similar(smallerList[y].Entry, biggerListData.Entry)))
                        {
                            // Logger.Debug($"Not The Same {smallerList[y].Entry} | {biggerListData.Entry} | {!smallerList[y].Entry.Equals(biggerListData.Entry)} | {!Similar(smallerList[y].Entry, biggerListData.Entry)} | {smallerList[y].Entry.Contains("Imperfect")}");
                            continue;
                        }
                        // If the vol numbers are the same and the titles are similar or the same from the if check above, add the lowest price volume to the list
                        
                        // Logger.Debug($"MATCH? ({biggerListCurrentVolNum}, {(biggerListData.Entry.Contains("Box Set") ? GetCurrentVolumeNum(smallerList[y].Entry, "Box Set") : GetCurrentVolumeNum(smallerList[y].Entry, "Vol"))}) = {biggerListCurrentVolNum == (biggerListData.Entry.Contains("Box Set") ? GetCurrentVolumeNum(smallerList[y].Entry, "Box Set") : GetCurrentVolumeNum(smallerList[y].Entry, "Vol"))}");
                        if (biggerListCurrentVolNum == (biggerListData.Entry.Contains("Box Set") ? GetCurrentVolumeNum(smallerList[y].Entry, "Box Set") : GetCurrentVolumeNum(smallerList[y].Entry, "Vol")))
                        {
                            // Logger.Debug($"Found Match for {biggerListData.Entry} {biggerListCurrentVolNum}");
                            // Logger.Debug($"PRICE COMPARISON ({float.Parse(biggerListData[1].Substring(1))}, {float.Parse(smallerList[y][1].Substring(1))}) -> {float.Parse(biggerListData[1].Substring(1)) > float.Parse(smallerList[y][1].Substring(1))}");
                            // Get the lowest price between the two then add the lowest dataset
                            if (float.Parse(biggerListData.Price.Substring(1)) > float.Parse(smallerList[y].Price.Substring(1)))
                            {
                                finalData.Add(smallerList[y]);
                                // Logger.Debug($"Add Match [{smallerList[y].Entry}, {smallerList[y].Price}, {smallerList[y].StockStatus}, {smallerList[y].Website}]");
                            }
                            else
                            {
                                finalData.Add(biggerListData);
                                // Logger.Debug($"Add Match [{biggerListData.Entry}, {biggerListData.Price}, {biggerListData.StockStatus}, {biggerListData.Website}]");
                            }
                            smallerList.RemoveAt(y);
                            // Logger.Debug($"Add [{biggerListData.Entry}, {biggerListData.Price}, {biggerListData.StockStatus}, {biggerListData.Website}]");

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
    }
    /// <summary>
    /// Compares EntryModel's by volume number
    /// </summary>
    public class VolumeSort : IComparer<EntryModel>
    {
        string bookTitle;

        public VolumeSort(string bookTitle)
        {
            this.bookTitle = bookTitle;
        }

        /// <summary>
        /// Last Modified: 03 March, 2023
        /// Author: Sigrec (Sean. N)
        /// Extracts the entry's volume number and checks to see if they are equal or similar enough
        /// then compares there volumes numbers to sort in ascending order.
        /// </summary>
        /// <param name="entry1">THe first EntryModel in the VolumeSort comparison</param>
        /// <param name="entry2">The second EntryModel in the VolumeSort comparison</param>
        /// <returns></returns>
        public int Compare(EntryModel entry1, EntryModel entry2)
        {
            int val1 = ExtractInt(entry1.Entry);
            int val2 = ExtractInt(entry2.Entry);
            if (string.Equals(Regex.Replace(entry1.Entry, @" Vol \d+$", ""), Regex.Replace(entry2.Entry, @" Vol \d+$", "")) || EntryModel.Similar(entry1.Entry, entry2.Entry))
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
            return entry1.Entry.CompareTo(entry2.Entry);
        }

        int ExtractInt(String s)
        {
            return Int32.Parse(Regex.Replace(s.Substring(bookTitle.Length), @".*( \d+)$", "$1").TrimStart());
        }
    }
}