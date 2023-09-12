namespace MangaLightNovelWebScrape
{
    public partial class EntryModel
    {
        public string Entry { get; set; }
        public string Price { get; set; }
        public string StockStatus { get; set; }
        public string  Website { get; set; }
        [GeneratedRegex(".*?(\\d+).*")]  private static partial Regex VolumeNumRegex();

        /// <summary>
        /// Model for a series's book entry
        /// </summary>
        /// <param name="entry">The title and vol # of a series entry</param>
        /// <param name="price">The price of the entry</param>
        /// <param name="stockStatus">The stockstatus of an entry, either IS, PO, OOS, OOP</param>
        /// <param name="website">The website in which the entry is found at</param>
        public EntryModel (string entry, string price, string stockStatus, string website)
        {
            Entry = entry;
            Price = price;
            StockStatus = stockStatus;
            Website = website;
        }

        /// <summary>
        /// Applies discount to a entry's price
        /// </summary>
        /// <param name="initialPrice">The initial price of the entry</param>
        /// <param name="discount">THe discount to apply to the initial price</param>
        /// <returns>Returns the discounted price</returns>
        public static string ApplyDiscount(decimal initialPrice, decimal discount)
        {
            return decimal.Subtract(initialPrice, decimal.Multiply(initialPrice, discount)).ToString("0.00");
        }

        public override string ToString()
        {
            return $"[{Entry}, {Price}, {StockStatus}, {Website}]";
        }

        /// <summary>
        /// Last Modified On: 03 MArch, 2023 by: Prem/Sigrec (Sean. N)
        /// Gets the current volume num for a series unit entry givin its type (box set, omnibux, single, etc)
        /// </summary>
        /// <param name="title">The full title of the entry to get the volume number</param>
        /// <param name="type">The type of entry it is either box set, omnibus, or single</param>
        /// <returns></returns>
        public static int GetCurrentVolumeNum(string title, string type)
        {
            return int.Parse(VolumeNumRegex().Replace(title[(title.IndexOf(" " + type) + type.Length + 1)..], "$1", 1));
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
            // Logger.Debug((count <= (titleOne.Length > titleTwo.Length ? titleTwo.Length / 6 : titleOne.Length / 6)) ? $"{titleOne} is Similar to {titleTwo}" : $"{titleOne} is Not Similar to {titleTwo}");
            return count <= (titleOne.Length > titleTwo.Length ? titleTwo.Length / 6 : titleOne.Length / 6); // Determine if they are similar enough by a threshold of 1/6 the size of longest title
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
    public partial class VolumeSort : IComparer<EntryModel>
    {
        string bookTitle;
        [GeneratedRegex(".*( \\d+)$")] private static partial Regex ExtractIntRegex();
        [GeneratedRegex(" Vol \\d+$")] private static partial Regex ExtractIntVolRegex();

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
            if (string.Equals(ExtractIntVolRegex().Replace(entry1.Entry, ""), ExtractIntVolRegex().Replace(entry2.Entry, "")) || EntryModel.Similar(entry1.Entry, entry2.Entry))
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

        int ExtractInt(string s)
        {
            return int.Parse(ExtractIntRegex().Replace(s[bookTitle.Length..], "$1").TrimStart());
        }
    }
}