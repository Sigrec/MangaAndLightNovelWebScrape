using GraphQL.Client.Abstractions.Utilities;
using MangaAndLightNovelWebScrape.Models;

namespace MangaAndLightNovelWebScrape
{
    public class Helpers
    {
        /// <summary>
        /// Gets the Browser Enum (Chrome, Edge, or FireFox) based on a string (ignores case)
        /// </summary>
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
        /// Gets the array of websites that have a membership for a given region as strings
        /// </summary>
        public static string[] GetMembershipWebsitesForRegionAsString(Region region)
        {
            return region switch
            {
                Region.America => [ BooksAMillion.WEBSITE_TITLE, KinokuniyaUSA.WEBSITE_TITLE ],
                Region.Canada => [ Indigo.WEBSITE_TITLE ],
                Region.Australia or Region.Britain or Region.Canada or Region.Japan or  _ => [ ]
            };
        }

        /// <summary>
        /// Gets the array of websites that have a membership for a given region
        /// </summary>
        public static Website[] GetMembershipWebsitesForRegion(Region region)
        {
            return region switch
            {
                Region.America => [ Website.BooksAMillion, Website.InStockTrades, Website.KinokuniyaUSA ],
                Region.Canada => [ Website.Indigo ],
                Region.Australia or Region.Britain or Region.Canada or Region.Japan or  _ => [ ]
            };
        }

        /// <summary>
        /// Gets Region Enum from a string (ignores case)
        /// </summary>
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
        /// Gets StockStatus Enum from a string (ignores case)
        /// </summary>
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
        /// Gets the StockStatusFilter from a given string
        /// </summary>
        /// <param name="stockStatusFilter">Stockstatus as a string</param>
        public static StockStatus[] GetStockStatusFilterFromString(string stockStatusFilter)
        {
            return stockStatusFilter switch
            {
                "Exclude All" or "all"=> StockStatusFilter.EXCLUDE_ALL_FILTER,
                "Exclude OOS & PO" or "OOS & PO" => StockStatusFilter.EXCLUDE_OOS_AND_PO_FILTER,
                "Exclude OOS & BO" or "OOS & BO" => StockStatusFilter.EXCLUDE_OOS_AND_BO_FILTER,
                "Exclude PO & BO" or "PO & BO" => StockStatusFilter.EXCLUDE_PO_AND_BO_FILTER,
                "Exclude OOS" or "OOS" or "oos"=> StockStatusFilter.EXCLUDE_OOS_FILTER,
                "Exclude PO" or "PO" or "po"=> StockStatusFilter.EXCLUDE_PO_FILTER,
                "Exclude BO" or "BO" or "bo"=> StockStatusFilter.EXCLUDE_BO_FILTER,
                _ => StockStatusFilter.EXCLUDE_NONE_FILTER
            };
        }

        /// <summary>
        /// Gets the array of Websites available for a specific region as strings where the strings are the const WEBSITE_TITLE of a Website class
        /// </summary>
        public static string[] GetRegionWebsiteListAsString(Region region)
        {
            return region switch
            {
                Region.America => [ AmazonUSA.WEBSITE_TITLE, BooksAMillion.WEBSITE_TITLE, Crunchyroll.WEBSITE_TITLE, InStockTrades.WEBSITE_TITLE, KinokuniyaUSA.WEBSITE_TITLE, MerryManga.WEBSITE_TITLE, RobertsAnimeCornerStore.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, ],
                Region.Australia => [ MangaMate.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, ],
                Region.Britain => [ ForbiddenPlanet.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, SpeedyHen.WEBSITE_TITLE, TravellingMan.WEBSITE_TITLE, Waterstones.WEBSITE_TITLE, ],
                Region.Canada => [ Indigo.WEBSITE_TITLE, SciFier.WEBSITE_TITLE, ],
                Region.Europe => [ SciFier.WEBSITE_TITLE, ],
                Region.Japan => [ /*AmazonJapan.WEBSITE_TITLE, CDJapan.WEBSITE_TITLE*/ ],
                _ => [ ]
            };
        }

        /// <summary>
        /// Gets the array of Websites available for a specific region as Website Enums
        /// </summary>
        public static HashSet<Website> GetRegionWebsiteList(Region region)
        {
            return region switch
            {
                Region.America => [ Website.AmazonUSA, Website.BooksAMillion, Website.Crunchyroll, Website.InStockTrades, Website.KinokuniyaUSA, Website.MerryManga, Website.RobertsAnimeCornerStore, Website.SciFier ],
                Region.Australia => [ Website.MangaMate, Website.SciFier ],
                Region.Britain => [ Website.ForbiddenPlanet, Website.SciFier, Website.SpeedyHen, Website.TravellingMan, Website.Waterstones ],
                Region.Canada => [ Website.Indigo, Website.SciFier ],
                Region.Europe => [ Website.SciFier ],
                Region.Japan => [ /*Website.AmazonJapan, Website.CDJapan*/ ],
                _ => [ ],
            };
        }

        /// <summary>
        /// Checks whether a given website list contains valid websites for a given region based on the websites WEBSITE_TITLE
        /// </summary>
        /// <param name="region">The region to check against</param>
        /// <param name="input">The list of websites </param>
        /// <returns>True if the given list is a valid list for the region, false otherwise</returns>
        public static bool IsWebsiteListValid(Region region, IEnumerable<string> input)
        {
            foreach (string website in input)
            {
                bool isValid = website.ToString() switch
                {
                    AmazonJapan.WEBSITE_TITLE => AmazonJapan.REGION.HasFlag(region),
                    AmazonUSA.WEBSITE_TITLE => AmazonUSA.REGION.HasFlag(region),
                    BooksAMillion.WEBSITE_TITLE => BooksAMillion.REGION.HasFlag(region),
                    CDJapan.WEBSITE_TITLE => CDJapan.REGION.HasFlag(region),
                    Crunchyroll.WEBSITE_TITLE => Crunchyroll.REGION.HasFlag(region),
                    ForbiddenPlanet.WEBSITE_TITLE => ForbiddenPlanet.REGION.HasFlag(region),
                    Indigo.WEBSITE_TITLE => Indigo.REGION.HasFlag(region),
                    InStockTrades.WEBSITE_TITLE => InStockTrades.REGION.HasFlag(region),
                    KinokuniyaUSA.WEBSITE_TITLE => KinokuniyaUSA.REGION.HasFlag(region),
                    MangaMate.WEBSITE_TITLE => MangaMate.REGION.HasFlag(region),
                    MerryManga.WEBSITE_TITLE => MerryManga.REGION.HasFlag(region),
                    RobertsAnimeCornerStore.WEBSITE_TITLE => RobertsAnimeCornerStore.REGION.HasFlag(region),
                    SciFier.WEBSITE_TITLE => SciFier.REGION.HasFlag(region),
                    SpeedyHen.WEBSITE_TITLE => SpeedyHen.REGION.HasFlag(region),
                    TravellingMan.WEBSITE_TITLE => TravellingMan.REGION.HasFlag(region),
                    Waterstones.WEBSITE_TITLE => Waterstones.REGION.HasFlag(region),
                    _ => throw new NotImplementedException(),
                };
                if (!isValid) { return false; }
            }
            return true;
        }

        /// <summary>
        /// Checks whether a given website list contains valid websites for a given region based on the website enum
        /// </summary>
        /// <param name="region">The region to check against</param>
        /// <param name="input">The list of websites </param>
        /// <returns>True if the given list is a valid list for the region, false otherwise</returns>
        public static bool IsWebsiteListValid(Region region, IEnumerable<Website> input)
        {
            foreach (Website website in input)
            {
                bool isValid = website switch
                {
                    Website.AmazonJapan => AmazonJapan.REGION.HasFlag(region),
                    Website.AmazonUSA => AmazonUSA.REGION.HasFlag(region),
                    Website.BooksAMillion => BooksAMillion.REGION.HasFlag(region),
                    Website.CDJapan => CDJapan.REGION.HasFlag(region),
                    Website.Crunchyroll => Crunchyroll.REGION.HasFlag(region),
                    Website.ForbiddenPlanet => ForbiddenPlanet.REGION.HasFlag(region),
                    Website.Indigo => Indigo.REGION.HasFlag(region),
                    Website.InStockTrades => InStockTrades.REGION.HasFlag(region),
                    Website.KinokuniyaUSA => KinokuniyaUSA.REGION.HasFlag(region),
                    Website.MangaMate => MangaMate.REGION.HasFlag(region),
                    Website.MerryManga => MerryManga.REGION.HasFlag(region),
                    Website.RobertsAnimeCornerStore => RobertsAnimeCornerStore.REGION.HasFlag(region),
                    Website.SciFier => SciFier.REGION.HasFlag(region),
                    Website.SpeedyHen => SpeedyHen.REGION.HasFlag(region),
                    Website.TravellingMan => TravellingMan.REGION.HasFlag(region),
                    Website.Waterstones => Waterstones.REGION.HasFlag(region),
                    _ => throw new NotImplementedException(),
                };
                if (!isValid) { return false; }
            }
            return true;
        }
    }
}