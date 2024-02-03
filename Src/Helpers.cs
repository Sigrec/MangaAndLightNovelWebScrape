using GraphQL.Client.Abstractions.Utilities;

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
                string curBrowser when curBrowser.Equals("Australia", StringComparison.OrdinalIgnoreCase) => Region.Australia,
                string curBrowser when curBrowser.Equals("Britain", StringComparison.OrdinalIgnoreCase) => Region.Britain,
                string curBrowser when curBrowser.Equals("Canada", StringComparison.OrdinalIgnoreCase) => Region.Canada,
                string curBrowser when curBrowser.Equals("Europe", StringComparison.OrdinalIgnoreCase) => Region.Europe,
                string curBrowser when curBrowser.Equals("Japan", StringComparison.OrdinalIgnoreCase) => Region.Japan,
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
            return stockStatus.ToLowerCase() switch
            {
                "is" or "instock" => StockStatus.IS,
                "po" or "Pre-Order" or "preorder" => StockStatus.PO,
                "oos" or "outofstock" => StockStatus.OOS,
                "bo" or "backorder" => StockStatus.BO,
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
                Region.America => [ /*AmazonUSA.WEBSITE_TITLE,*/ BarnesAndNoble.WEBSITE_TITLE, BooksAMillion.WEBSITE_TITLE, Crunchyroll.WEBSITE_TITLE, InStockTrades.WEBSITE_TITLE, KinokuniyaUSA.WEBSITE_TITLE, MerryManga.WEBSITE_TITLE, RobertsAnimeCornerStore.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
                Region.Australia => [ MangaMate.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
                Region.Britain => [ /*ForbiddenPlanet.WEBSITE_TITLE,*/ SciFier.WEBSITE_TITLE, SpeedyHen.WEBSITE_TITLE, Waterstones.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
                Region.Canada => [ Indigo.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
                Region.Europe => [ SciFier.WEBSITE_TITLE, Wordery.WEBSITE_TITLE ],
                Region.Japan => [ AmazonJapan.WEBSITE_TITLE, CDJapan.WEBSITE_TITLE ],
                _ => [ ]
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
                Region.America => [ /*Website.AmazonUSA,*/ Website.BarnesAndNoble, Website.BooksAMillion, Website.Crunchyroll, Website.InStockTrades, Website.KinokuniyaUSA, Website.MerryManga, Website.RobertsAnimeCornerStore, Website.SciFier, Website.Wordery ],
                Region.Australia => [ Website.MangaMate, Website.SciFier, Website.Wordery ],
                Region.Britain => [ /*Website.ForbiddenPlanet,*/ Website.SciFier, Website.SpeedyHen, Website.Waterstones, Website.Wordery ],
                Region.Canada => [ Website.Indigo, Website.SciFier, Website.Wordery ],
                Region.Europe => [ Website.SciFier, Website.Wordery ],
                Region.Japan => [ Website.AmazonJapan, Website.CDJapan ],
                _ => [ ],
            };
        }
    }
}