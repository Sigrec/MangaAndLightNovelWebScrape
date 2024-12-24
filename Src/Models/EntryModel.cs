using NLog.Common;

namespace MangaAndLightNovelWebScrape
{
    public partial class EntryModel : IEquatable<EntryModel>
    {
        public string Entry { get; set; }
        public string Price { get; set; }
        public StockStatus StockStatus { get; set; }
        public string  Website { get; set; }
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        internal static VolumeSort VolumeSort = new VolumeSort();
        [GeneratedRegex(@"[Vol|Box Set].*?(\d+).*")]  private static partial Regex VolumeNumRegex();
        [GeneratedRegex(@"(?:.*(?<int> \d{1,3})|.*(?<double> \d{1,3}\.\d{1,3}))(?:\s+Novel$|$)|(?:.*(?<int> \d{1,3})-\d{1,3})")] private static partial Regex ExtractDoubleRegex();

        /// <summary>
        /// Model for a series's book entry
        /// </summary>
        /// <param name="entry">The title and vol # of a series entry</param>
        /// <param name="price">The price of the entry</param>
        /// <param name="stockStatus">The stockstatus of an entry, either IS, PO, OOS, OOP</param>
        /// <param name="website">The website in which the entry is found at</param>
        public EntryModel (string Entry, string Price, StockStatus StockStatus, string Website)
        {
            this.Entry = Entry;
            this.Price = Price;
            this.StockStatus = StockStatus;
            this.Website = Website;
        }

        /// <summary>
        /// Applies discount to a entry's price
        /// </summary>
        /// <param name="initialPrice">The initial price of the entry</param>
        /// <param name="discount">THe discount to apply to the initial price</param>
        /// <returns>Returns the discounted price</returns>
        internal static string ApplyDiscount(decimal initialPrice, decimal discount)
        {
            return decimal.Subtract(initialPrice, decimal.Multiply(initialPrice, discount)).ToString("0.00");
        }

        public override string ToString()
        {
            return $"[{this.Entry}, {this.Price}, {this.StockStatus}, {this.Website}]";
        }

        /// <summary>
        /// Parses and returns the price as a decimal value
        /// </summary>
        public decimal ParsePrice()
        {
            if (!char.IsDigit(this.Price[0])) // Currency places the symbol at the front like USD
            {
                return decimal.Parse(this.Price[1..]);
            }
            return decimal.Parse(this.Price[..^1]); // Currency places the symbol at the end like with Japanese Yen
        }

        /// <summary>
        /// Gets the current volume num for a series unit entry givin its type (box set, omnibux, single, etc)
        /// </summary>
        /// <param name="title">The full title of the entry to get the volume number</param>
        /// <returns></returns>
        internal static double GetCurrentVolumeNum(string title)
        {
            // Early return if "Box Set" is found
            if (title.Contains("Box Set"))
            {
                return -1;
            }

            Match match = ExtractDoubleRegex().Match(title);
            // Check for integer match first
            if (match.Groups["int"].Success)
            {
                return Convert.ToDouble(match.Groups["int"].Value);
            }

            // Check for double match
            if (match.Groups["double"].Success)
            {
                return Convert.ToDouble(match.Groups["double"].Value);
            }

            // Log failure if no match found
            LOGGER.Error($"Failed to Extract Entry # from \"{title}\"");
            return -1;
        }

        /// <summary>
        ///  The Damerau–Levenshtein distance between two words is the minimum number of operations (consisting of insertions, deletions or substitutions of a single character, or transposition of two adjacent characters) required to change one word into the other (http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html)
        /// </summary>
        /// <returns>The distance, >= 0 representing the number of edits required to transform one string to the other, or -1 if the distance is greater than the specified maxDistance.</returns>
        public static int Similar(string s, string t, int maxDistance)
        {
            if (string.IsNullOrWhiteSpace(s)) return ((t ?? string.Empty).Length <= maxDistance) ? (t ?? string.Empty).Length : -1;
            if (string.IsNullOrWhiteSpace(t)) return (s.Length <= maxDistance) ? s.Length : -1;
            s = s.ToLower();
            t = t.ToLower(); 
            // if strings of different lengths, ensure shorter string is in s. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (s.Length > t.Length) {
                (t, s) = (s, t);
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
                //    StockStatus == other.StockStatus &&
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
        [GeneratedRegex(@" (?:Vol|Box Set) \d{1,3}(?:\.\d{1,2})?$")] private static partial Regex ExtractNameRegex();
        [GeneratedRegex(@"[^\p{L}\p{N}\s\.]")] private static partial Regex FilterNameRegex();
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Extracts the entry's volume number and checks to see if they are equal or similar enough
        /// then compares there volumes numbers to sort in ascending order.
        /// </summary>
        /// <param name="entry1">THe first EntryModel in the VolumeSort comparison</param>
        /// <param name="entry2">The second EntryModel in the VolumeSort comparison</param>
        /// <returns></returns>
        public int Compare(EntryModel entry1, EntryModel entry2)
        {
            string entry1Text = FilterNameRegex().Replace(entry1.Entry, " ");
            string entry2Text = FilterNameRegex().Replace(entry2.Entry, " ");

            if ((entry1.Entry.Contains("Vol") && entry2.Entry.Contains("Vol")) || (entry1.Entry.Contains("Box Set") && entry2.Entry.Contains("Box Set")))
            {
                double val1 = EntryModel.GetCurrentVolumeNum(entry1.Entry);
                double val2 = EntryModel.GetCurrentVolumeNum(entry2.Entry);

                string entry1Name = ExtractNameRegex().Replace(entry1Text, string.Empty);
                string entry2Name = ExtractNameRegex().Replace(entry2Text, string.Empty);

                // Check for valid volume numbers and matching names
                if (val1 != -1 && val2 != -1)
                {
                    bool namesMatch = string.Equals(entry1Name, entry2Name, StringComparison.OrdinalIgnoreCase);
                    if (namesMatch || (EntryModel.Similar(entry1Name, entry2Name, Math.Min(entry1Name.Length, entry2Name.Length) / 6) != -1))
                    {
                        return val1.CompareTo(val2); // Simplified comparison of val1 and val2
                    }
                }
            }
            return string.Compare(entry1Text, entry2Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}