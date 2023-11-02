namespace MangaLightNovelWebScrape
{
    public static partial class Helpers
    {
        [GeneratedRegex(@"[^\w+]")] internal static partial Regex RemoveNonWordsRegex();
        public static Browser GetBrowserFromString(string browser)
        {
            return browser switch
            {
                string curBrowser when curBrowser.Equals("Chrome", StringComparison.OrdinalIgnoreCase) => Browser.Chrome,
                string curBrowser when curBrowser.Equals("Edge", StringComparison.OrdinalIgnoreCase) => Browser.Edge,
                string curBrowser when curBrowser.Equals("FireFox", StringComparison.OrdinalIgnoreCase) => Browser.FireFox,
                _ => throw new Exception(),
            };
        }

        /// <summary>
        /// Gets Region Enum from string
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static Region GetRegionFromString(string region)
        {
            return region switch
            {
                string curBrowser when curBrowser.Equals("America", StringComparison.OrdinalIgnoreCase) => Region.America,
                string curBrowser when curBrowser.Equals("Britain", StringComparison.OrdinalIgnoreCase) => Region.Britain,
                string curBrowser when curBrowser.Equals("Japan", StringComparison.OrdinalIgnoreCase) => Region.Japan,
                string curBrowser when curBrowser.Equals("Canada", StringComparison.OrdinalIgnoreCase) => Region.Canada,
                string curBrowser when curBrowser.Equals("Europe", StringComparison.OrdinalIgnoreCase) => Region.Europe,
                _ => throw new Exception(),
            };
        }

        /// <summary>
        /// Gets StockStatus Enum from string
        /// </summary>
        /// <param name="stockStatus"></param>
        /// <returns></returns>
        public static StockStatus GetStockStatusFromString(string stockStatus)
        {
            return stockStatus switch
            {
                "IS" or "In Stock" => StockStatus.IS,
                "PO" or "Pre-Order" => StockStatus.PO,
                "OOS" or "Out of Stock" => StockStatus.OOS,
                _ => StockStatus.NA
            };
        }

        /// <summary>
        /// Gets the array of Websites available for a specific region as strings
        /// </summary>
        /// <param name="region">The region to get</param>
        /// <returns></returns>
        public static string[] GetRegionWebsiteListAsString(Region region)
        {
            return region switch
            {
                Region.America => [ AmazonUSA.WEBSITE_TITLE, BarnesAndNoble.WEBSITE_TITLE, BooksAMillion.WEBSITE_TITLE, InStockTrades.WEBSITE_TITLE, KinokuniyaUSA.WEBSITE_TITLE, Crunchyroll.WEBSITE_TITLE, RobertsAnimeCornerStore.WEBSITE_TITLE, SciFier.WEBSITE_TITLE ],
                Region.Britain => [ ForbiddenPlanet.WEBSITE_TITLE, Waterstones.WEBSITE_TITLE, SciFier.WEBSITE_TITLE ],
                Region.Canada => [ Indigo.WEBSITE_TITLE, SciFier.WEBSITE_TITLE ],
                Region.Europe => [ SciFier.WEBSITE_TITLE ],
                Region.Japan => [ AmazonJapan.WEBSITE_TITLE, CDJapan.WEBSITE_TITLE ],
                _ => []
            };
        }

        /// <summary>
        /// Gets the array of Websites available for a specific region as Website Enums
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static Website[] GetRegionWebsiteList(Region region)
        {
            return region switch
            {
                Region.America => [ Website.AmazonUSA, Website.BarnesAndNoble, Website.BooksAMillion, Website.InStockTrades, Website.KinokuniyaUSA, Website.Crunchyroll, Website.RobertsAnimeCornerStore, Website.SciFier ],
                Region.Britain => [ Website.ForbiddenPlanet, Website.Waterstones, Website.SciFier ],
                Region.Canada => [ Website.Indigo, Website.SciFier ],
                Region.Europe => [ Website.SciFier ],
                Region.Japan => [ Website.AmazonJapan, Website.CDJapan ],
                _ => [ ],
            };
        }

        /// <summary>
        /// Determines if the book title inputted by the user is contained within the current title scraped from the website
        /// </summary>
        /// <param name="bookTitle">The title inputed by the user to initialize the scrape</param>
        /// <param name="curTitle">The current title scraped from the website</param>
        internal static bool TitleContainsBookTitle(string bookTitle, string curTitle)
        {
            return RemoveNonWordsRegex().Replace(curTitle, "").Contains(RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TitleStartsWithCheck(string bookTitle, string curTitle)
        {
            return RemoveNonWordsRegex().Replace(curTitle, "").StartsWith(RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Trims the end of the StingBuilder Content. On Default only the white space char is truncated.
        /// </summary>
        /// <param name="pTrimChars">Array of additional chars to be truncated. A little bit more efficient than using char[]</param>
        /// <returns></returns>
        internal static StringBuilder TrimEnd(this StringBuilder pStringBuilder, HashSet<char> pTrimChars = null)
        {
            if (pStringBuilder == null || pStringBuilder.Length == 0)
                return pStringBuilder;

            int i = pStringBuilder.Length - 1;

            for (; i >= 0; i--)
            {
                var lChar = pStringBuilder[i];

                if (pTrimChars == null)
                {
                    if (char.IsWhiteSpace(lChar) == false)
                        break;
                }
                else if ((char.IsWhiteSpace(lChar) == false) && (pTrimChars.Contains(lChar) == false))
                    break;
            }

            if (i < pStringBuilder.Length - 1)
                pStringBuilder.Length = i + 1;

            return pStringBuilder;
        }

        internal static void ReplaceTextInEntryTitle (ref StringBuilder curTitle, string bookTitle, string containsText, string replaceText)
        {
            if (curTitle.ToString().Contains(containsText) && !bookTitle.Contains(containsText.Trim()))
            {
                curTitle.Replace(containsText, replaceText);
            }
        }

        internal static void RemoveCharacterFromTitle(ref StringBuilder title, string bookTitle, char charToRemove)
        {
            if (!bookTitle.Contains(charToRemove))
            {
                title.Replace(charToRemove.ToString(), "");
            }
        }
    }
}