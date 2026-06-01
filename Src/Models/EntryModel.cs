namespace MangaAndLightNovelWebScrape
{
    /// <summary>
    /// One row in a scrape result — a single volume/box-set listing from a single retailer.
    /// Equality is by <see cref="Entry"/>, <see cref="Price"/>, and <see cref="Website"/>;
    /// dedup across sites uses just <see cref="Entry"/>.
    /// </summary>
    public partial struct EntryModel : IEquatable<EntryModel>
    {
        /// <summary>Cleaned title text, e.g. <c>"Jujutsu Kaisen Vol 12"</c>.</summary>
        public string Entry { get; set; }
        /// <summary>Display price with currency symbol, e.g. <c>"$9.99"</c> or <c>"£6.38"</c>.</summary>
        public string Price { get; set; }
        /// <summary>Availability state on the source site at scrape time.</summary>
        public StockStatus StockStatus { get; set; }
        /// <summary>Source retailer title (e.g. <c>"Crunchyroll"</c>) — the site's <c>TITLE</c> constant.</summary>
        public string  Website { get; set; }
        internal static VolumeSort VolumeSort = new();
        // [GeneratedRegex(@"[Vol|Box Set].*?(\d+).*")]  private static partial Regex VolumeNumRegex();
        [GeneratedRegex(@"(?:.*(?<int> \d{1,3})|.*(?<double> \d{1,3}\.\d{1,3}))(?:\s+Novel$|$)|(?:.*(?<int> \d{1,3})-\d{1,3})")] private static partial Regex ExtractDoubleRegex();

        /// <summary>
        /// Builds an <see cref="EntryModel"/> for a single scraped row. Per-site scrapers
        /// construct this once per qualifying listing they parse.
        /// </summary>
        /// <param name="Entry">Cleaned title including the volume / box-set marker.</param>
        /// <param name="Price">Display-form price string with currency symbol.</param>
        /// <param name="StockStatus">Availability state read off the listing.</param>
        /// <param name="Website">Source retailer title — usually the site's <c>TITLE</c> const.</param>
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

        /// <summary>
        /// Returns a compact display string in the form
        /// <c>[Entry, Price, StockStatus, Website]</c>. Used by
        /// <see cref="MasterScrapeExtensions.PrintResultsToConsole"/> and friends when
        /// <c>isAsciiTable</c> is <c>false</c>.
        /// </summary>
        public override string ToString()
        {
            return $"[{this.Entry}, {this.Price}, {this.StockStatus}, {this.Website}]";
        }

        /// <summary>
        /// Parses and returns the price as a decimal value. Uses span-based parsing so no
        /// intermediate substring is allocated — important because this is called per
        /// dedup pair and per merge probe.
        /// </summary>
        /// <remarks>
        /// Returns <c>0m</c> for empty / whitespace-only / unparseable prices. Sites should
        /// be filtering merchandise entries before they reach the data list, but the
        /// safety net here means one bad row can't crash the whole dedup pass.
        /// </remarks>
        public decimal ParsePrice()
        {
            ReadOnlySpan<char> price = this.Price.AsSpan().Trim();
            if (price.IsEmpty) return 0m;

            // Currency at front (USD, GBP, etc.): "$10.99" → slice off symbol.
            // Currency at end (JPY ¥, etc.): "1099¥" → slice off symbol.
            // If the only non-digit char is in the middle (malformed), TryParse below
            // catches it and returns 0 — better than throwing inside dedup.
            ReadOnlySpan<char> span = char.IsDigit(price[0])
                ? char.IsDigit(price[^1]) ? price : price[..^1]
                : price.Length > 1 ? price[1..] : default;

            return decimal.TryParse(span, System.Globalization.CultureInfo.InvariantCulture, out decimal value)
                ? value
                : 0m;
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

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            // only true if the boxed obj is an EntryModel
            if (obj is EntryModel other)
            {
                return Equals(other);
            }

            return false;
        }

        /// <summary>
        /// Two entries are equal when <see cref="Entry"/>, <see cref="Price"/>, and
        /// <see cref="Website"/> match. <see cref="StockStatus"/> is intentionally
        /// excluded so the same listing at the same price counts as equal even if its
        /// availability state changed between snapshots.
        /// </summary>
        public bool Equals(EntryModel other)
        {
            // compare all fields you care about
            return Entry       == other.Entry
                && Price       == other.Price
                && Website     == other.Website;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Entry, Price, StockStatus, Website);
        }

        /// <summary>Value-equality comparison; see <see cref="Equals(EntryModel)"/>.</summary>
        public static bool operator ==(EntryModel left, EntryModel right)
        {
            return EqualityComparer<EntryModel>.Default.Equals(left, right);
        }

        /// <summary>Inverse of <c>operator ==</c>.</summary>
        public static bool operator !=(EntryModel left, EntryModel right)
        {
            return !(left == right);
        }
    }
    /// <summary>
    /// Sorts <see cref="EntryModel"/> rows by series name then ascending volume number.
    /// Used by <c>List&lt;EntryModel&gt;.SortByVolume()</c> on per-site results and on the
    /// final merged list. Entries whose names don't match (or aren't close enough by
    /// Damerau-Levenshtein distance) fall back to ordinal title ordering.
    /// </summary>
    public partial class VolumeSort : IComparer<EntryModel>
    {
        [GeneratedRegex(@" (?:Vol|Box Set) \d{1,3}(?:\.\d{1,2})?$")] internal static partial Regex ExtractNameRegex();
        [GeneratedRegex(@"[^\p{L}\p{N}\s\.]")] internal static partial Regex FilterNameRegex();

        /// <summary>
        /// Returns a value &lt; 0 if <paramref name="entry1"/> sorts before
        /// <paramref name="entry2"/>, 0 if equal, &gt; 0 if after. Two entries from the
        /// same series sort by volume number; otherwise by ordinal title.
        /// </summary>
        /// <param name="entry1">The left-hand entry.</param>
        /// <param name="entry2">The right-hand entry.</param>
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