namespace MangaAndLightNovelWebScrape.Models
{
    public class Constants
    {
        public enum Browser
        {
            Chrome,
            Edge,
            FireFox
        }
        public enum Website
        {
            AmazonJapan,
            AmazonUSA,
            BarnesAndNoble,
            BooksAMillion,
            CDJapan,
            Crunchyroll,
            ForbiddenPlanet,
            Indigo,
            InStockTrades,
            KinokuniyaUSA,
            MangaMate,
            MerryManga,
            RobertsAnimeCornerStore,
            SciFier,
            SpeedyHen,
            Waterstones,
            Wordery
        }

        public enum BookType
        {
            Manga,
            LightNovel
        }

        [Flags]
        public enum Region
        {
            America = 0,
            Australia = 1,
            Britain = 2,
            Canada = 3,
            Europe = 4,
            Japan = 5,
        }

        /// <summary>
        /// The stock status of a entry, either In Stock (IS), Out of Stock (OOS), Pre Order (PO), or Not Available (NA)
        /// </summary>
        public enum StockStatus
        {
            /// <summary>
            /// In Stock
            /// </summary>
            IS,
            /// <summary>
            /// Out of Stock
            /// </summary>
            OOS,
            /// <summary>
            /// Pre Order
            /// </summary>
            PO,
            /// <summary>
            /// Not Available
            /// </summary>
            NA,
            /// <summary>
            /// Backorder
            /// </summary>
            BO,
        }
    }
}