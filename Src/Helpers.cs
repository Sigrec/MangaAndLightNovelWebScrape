namespace MangaAndLightNovelWebScrape
{
    public class Helpers
    {
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
                string curBrowser when curBrowser.Equals("Australia", StringComparison.OrdinalIgnoreCase) => Region.Australia,
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
                "BO" or "Backorder" => StockStatus.BO,
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
                Region.America => [ AmazonUSA.WEBSITE_TITLE, BarnesAndNoble.WEBSITE_TITLE, BooksAMillion.WEBSITE_TITLE, InStockTrades.WEBSITE_TITLE, KinokuniyaUSA.WEBSITE_TITLE, Crunchyroll.WEBSITE_TITLE, RobertsAnimeCornerStore.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
                Region.Australia => [ SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE, MangaMate.WEBSITE_TITLE ],
                Region.Britain => [ ForbiddenPlanet.WEBSITE_TITLE, Waterstones.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
                Region.Canada => [ Indigo.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
                Region.Europe => [ SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
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
                Region.America => [ Website.AmazonUSA, Website.BarnesAndNoble, Website.BooksAMillion, Website.InStockTrades, Website.KinokuniyaUSA, Website.Crunchyroll, Website.RobertsAnimeCornerStore, Website.SciFier, Website.Wordery ],
                Region.Australia => [ Website.SciFier, Website.Wordery, Website.MangaMate ],
                Region.Britain => [ Website.ForbiddenPlanet, Website.Waterstones, Website.SciFier, Website.Wordery ],
                Region.Canada => [ Website.Indigo, Website.SciFier, Website.Wordery ],
                Region.Europe => [ Website.SciFier, Website.Wordery ],
                Region.Japan => [ Website.AmazonJapan, Website.CDJapan ],
                _ => [ ],
            };
        }
    }
}