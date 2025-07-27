using NLog.Common;

namespace MangaAndLightNovelWebScrape
{
    public partial struct EntryModel : IEquatable<EntryModel>
    {
        public string Entry { get; set; }
        public string Price { get; set; }
        public StockStatus StockStatus { get; set; }
        public string  Website { get; set; }
        private static readonly Logger LOGGER = LogManager.GetLogger("MasterScrape");
        internal static VolumeSort VolumeSort = new();
        // [GeneratedRegex(@"[Vol|Box Set].*?(\d+).*")]  private static partial Regex VolumeNumRegex();
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
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.IsNullOrEmpty(t) || t.Length <= maxDistance ? t.Length : -1;
            }

            if (string.IsNullOrWhiteSpace(t))
            {
                return s.Length <= maxDistance ? s.Length : -1;
            }

            ReadOnlySpan<char> sSpan = s;
            ReadOnlySpan<char> tSpan = t;

            // Always operate on the shorter string
            if (sSpan.Length > tSpan.Length)
            {
                ReadOnlySpan<char> tmp = sSpan;
                sSpan = tSpan;
                tSpan = tmp;
            }

            int sLen = sSpan.Length;
            int tLen = tSpan.Length;

            if (tLen - sLen > maxDistance)
            {
                return -1;
            }

            Span<int> previousRow = stackalloc int[tLen + 1];
            Span<int> currentRow = stackalloc int[tLen + 1];

            for (int j = 0; j <= tLen; j++)
            {
                previousRow[j] = j;
            }

            for (int i = 1; i <= sLen; i++)
            {
                currentRow[0] = i;
                int bestThisRow = currentRow[0];

                char sChar = char.ToLowerInvariant(sSpan[i - 1]);
                for (int j = 1; j <= tLen; j++)
                {
                    char tChar = char.ToLowerInvariant(tSpan[j - 1]);

                    int cost = sChar == tChar ? 0 : 1;
                    int insert = currentRow[j - 1] + 1;
                    int delete = previousRow[j] + 1;
                    int replace = previousRow[j - 1] + cost;

                    currentRow[j] = Math.Min(Math.Min(insert, delete), replace);

                    bestThisRow = Math.Min(bestThisRow, currentRow[j]);
                }

                if (bestThisRow > maxDistance)
                {
                    return -1;
                }

                Span<int> temp = previousRow;
                previousRow = currentRow;
                currentRow = temp;
            }

            int result = previousRow[tLen];
            return result <= maxDistance ? result : -1;
        }

        public override bool Equals(object? obj)
        {
            // only true if the boxed obj is an EntryModel
            if (obj is EntryModel other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(EntryModel other)
        {
            // compare all fields you care about
            return Entry       == other.Entry
                && Price       == other.Price
                && Website     == other.Website;
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