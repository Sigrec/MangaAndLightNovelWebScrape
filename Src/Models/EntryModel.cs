namespace MangaAndLightNovelWebScrape
{
    public partial struct EntryModel : IEquatable<EntryModel>
    {
        public string Entry { get; set; }
        public string Price { get; set; }
        public StockStatus StockStatus { get; set; }
        public string  Website { get; set; }
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
        /// Parses and returns the price as a decimal value. Uses span-based parsing so no
        /// intermediate substring is allocated — important because this is called per
        /// dedup pair and per merge probe.
        /// </summary>
        public decimal ParsePrice()
        {
            // Currency at front (USD, GBP, etc.): "$10.99" → slice off symbol.
            // Currency at end (JPY ¥, etc.): "1099¥" → slice off symbol.
            ReadOnlySpan<char> span = char.IsDigit(this.Price[0])
                ? this.Price.AsSpan(0, this.Price.Length - 1)
                : this.Price.AsSpan(1);

            return decimal.Parse(span, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the current volume num for a series unit entry given its type (box set, omnibus,
        /// single, etc). Returns <c>-1</c> for Box Sets and unparseable titles.
        /// </summary>
        internal static double GetCurrentVolumeNum(string title)
        {
            if (title.Contains("Box Set"))
            {
                return -1;
            }

            Match match = ExtractDoubleRegex().Match(title);

            // Group.ValueSpan + double.Parse(span) avoids materializing the captured substring
            // and the boxing/conversion path through Convert.ToDouble(string).
            Group intGroup = match.Groups["int"];
            if (intGroup.Success)
            {
                return double.Parse(intGroup.ValueSpan, System.Globalization.CultureInfo.InvariantCulture);
            }

            Group doubleGroup = match.Groups["double"];
            if (doubleGroup.Success)
            {
                return double.Parse(doubleGroup.ValueSpan, System.Globalization.CultureInfo.InvariantCulture);
            }

            return -1;
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
        [GeneratedRegex(@" (?:Vol|Box Set) \d{1,3}(?:\.\d{1,2})?$")] internal static partial Regex ExtractNameRegex();
        [GeneratedRegex(@"[^\p{L}\p{N}\s\.]")] internal static partial Regex FilterNameRegex();

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
                    if (namesMatch || (InternalHelpers.Similar(entry1Name, entry2Name, Math.Min(entry1Name.Length, entry2Name.Length) / 6) != -1))
                    {
                        return val1.CompareTo(val2); // Simplified comparison of val1 and val2
                    }
                }
            }
            return string.Compare(entry1Text, entry2Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}