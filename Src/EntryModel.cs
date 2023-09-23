using NLog.Common;

namespace MangaLightNovelWebScrape
{
    public partial class EntryModel : IEquatable<EntryModel>
    {
        public string Entry { get; set; }
        public string Price { get; set; }
        public string StockStatus { get; set; }
        public string  Website { get; set; }
        private static readonly Logger Logger = LogManager.GetLogger("MasterScrapeLogs");
        [GeneratedRegex("[Vol|Box Set].*?(\\d+).*")]  private static partial Regex VolumeNumRegex();
        [GeneratedRegex(".*(?<int> \\d+)$|.*(?<double> \\d+\\.\\d+)$")] private static partial Regex ExtractDoubleRegex();

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
        /// Gets the current volume num for a series unit entry givin its type (box set, omnibux, single, etc)
        /// </summary>
        /// <param name="title">The full title of the entry to get the volume number</param>
        /// <returns></returns>
        public static double GetCurrentVolumeNum(string title)
        {
            Match match = ExtractDoubleRegex().Match(title);
            if (match.Groups["int"].Success)
            {
                return Convert.ToDouble(match.Groups["int"].Value);
            }
            else if (match.Groups["double"].Success)
            {
                return Convert.ToDouble(match.Groups["double"].Value);
            }
            else if (title.Contains("Box Set"))
            {
                return -1;
            }
            Logger.Error($"Failed to Extract Entry # from {title}");
            return -1;
        }

        /// <summary>
        /// http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html
        /// </summary>
        /// <returns>distance, >= 0 representing the number of edits required
        /// to transform one string to the other, or -1 if the distance is greater than the specified maxDistance.</returns>
        public static int Similar(string s, string t, int maxDistance)
        {
            if (string.IsNullOrEmpty(s)) return ((t ?? "").Length <= maxDistance) ? (t ?? "").Length : -1;
            if (string.IsNullOrEmpty(t)) return (s.Length <= maxDistance) ? s.Length : -1;

            // if strings of different lengths, ensure shorter string is in s. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (s.Length > t.Length) {
                var temp = s; s = t; t = temp; // swap s and t
            }
            int sLen = s.Length; // this is also the minimun length of the two strings
            int tLen = t.Length;

            // suffix common to both strings can be ignored
            while ((sLen > 0) && (s[sLen - 1] == t[tLen - 1])) { sLen--; tLen--; }

            int start = 0;
            if ((s[0] == t[0]) || (sLen == 0)) { // if there's a shared prefix, or all s matches t's suffix
                // prefix common to both strings can be ignored
                while ((start < sLen) && (s[start] == t[start])) start++;
                sLen -= start; // length of the part excluding common prefix and suffix
                tLen -= start;

                // if all of shorter string matches prefix and/or suffix of longer string, then
                // edit distance is just the delete of additional characters present in longer string
                if (sLen == 0) return (tLen <= maxDistance) ? tLen : -1;

                t = t.Substring(start, tLen); // faster than t[start+j] in inner loop below
            }
            int lenDiff = tLen - sLen;
            if ((maxDistance < 0) || (maxDistance > tLen)) {
                maxDistance = tLen;
            } else if (lenDiff > maxDistance) return -1;

            var v0 = new int[tLen];
            var v2 = new int[tLen]; // stores one level further back (offset by +1 position)
            int j;
            for (j = 0; j < maxDistance; j++) v0[j] = j + 1;
            for (; j < tLen; j++) v0[j] = maxDistance + 1;

            int jStartOffset = maxDistance - (tLen - sLen);
            bool haveMax = maxDistance < tLen;
            int jStart = 0;
            int jEnd = maxDistance;
            char sChar = s[0];
            int current = 0;
            for (int i = 0; i < sLen; i++) {
                char prevsChar = sChar;
                sChar = s[start + i];
                char tChar = t[0];
                int left = i;
                current = left + 1;
                int nextTransCost = 0;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
                jStart += (i > jStartOffset) ? 1 : 0;
                jEnd += (jEnd < tLen) ? 1 : 0;
                for (j = jStart; j < jEnd; j++) {
                    int above = current;
                    int thisTransCost = nextTransCost;
                    nextTransCost = v2[j];
                    v2[j] = current = left; // cost of diagonal (substitution)
                    left = v0[j];    // left now equals current cost (which will be diagonal at next iteration)
                    char prevtChar = tChar;
                    tChar = t[j];
                    if (sChar != tChar) {
                        if (left < current) current = left;   // insertion
                        if (above < current) current = above; // deletion
                        current++;
                        if ((i != 0) && (j != 0)
                            && (sChar == prevtChar)
                            && (prevsChar == tChar)) {
                            thisTransCost++;
                            if (thisTransCost < current) current = thisTransCost; // transposition
                        }
                    }
                    v0[j] = current;
                }
                if (haveMax && (v0[i + lenDiff] > maxDistance)) return -1;
            }
            return (current <= maxDistance) ? current : -1;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EntryModel);
        }

        public bool Equals(EntryModel other)
        {
            return other is not null &&
                   Entry == other.Entry &&
                   Price == other.Price &&
                   StockStatus == other.StockStatus &&
                   Website == other.Website;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Entry, Price, StockStatus, Website);
        }

        public static bool operator ==(EntryModel left, EntryModel right)
        {
            return EqualityComparer<EntryModel>.Default.Equals(left, right);
        }

        public static bool operator !=(EntryModel left, EntryModel right)
        {
            return !(left == right);
        }
    }
    /// <summary>
    /// Compares EntryModel's by entry title
    /// </summary>
    public partial class VolumeSort : IComparer<EntryModel>
    {
        private static readonly Logger Logger = LogManager.GetLogger("MasterScrapeLogs");
        [GeneratedRegex(".*(?<int> \\d+)$|.*(?<double> \\d+\\.\\d+)$")] private static partial Regex ExtractIntRegex();
        [GeneratedRegex(" Vol \\d+$| Box Set\\d+$| Vol \\d+\\.\\d+$| Box Set \\d+\\.\\d+$")] private static partial Regex ExtractVolNameRegex();

        /// <summary>
        /// Extracts the entry's volume number and checks to see if they are equal or similar enough
        /// then compares there volumes numbers to sort in ascending order.
        /// </summary>
        /// <param name="entry1">THe first EntryModel in the VolumeSort comparison</param>
        /// <param name="entry2">The second EntryModel in the VolumeSort comparison</param>
        /// <returns></returns>
        public int Compare(EntryModel entry1, EntryModel entry2)
        {
            //Logger.Debug($"{entry1.Entry}|{entry2.Entry}");
            if ((entry1.Entry.Contains("Vol") || entry1.Entry.Contains("Box Set")) && (entry2.Entry.Contains("Vol") || entry2.Entry.Contains("Box Set")))
            {
                double val1 = EntryModel.GetCurrentVolumeNum(entry1.Entry);
                double val2 = EntryModel.GetCurrentVolumeNum(entry2.Entry);
                if (val1 != -1 && val2 != -1 && string.Equals(ExtractVolNameRegex().Replace(entry1.Entry, ""), ExtractVolNameRegex().Replace(entry2.Entry, ""), StringComparison.OrdinalIgnoreCase))
                {
                    if (val1 > val2)
                    {
                        return 1;
                    }
                    else if (val1 < val2)
                    {
                        return -1;
                    }
                    else if (val1 == val2)
                    {
                        return 0;
                    }
                }
            }
            return entry1.Entry.CompareTo(entry2.Entry);
        }
    }
}