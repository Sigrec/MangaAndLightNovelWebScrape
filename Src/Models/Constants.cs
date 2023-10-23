namespace Src.Models
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
            ForbiddenPlanet,
            Indigo,
            InStockTrades,
            KinokuniyaUSA,
            Crunchyroll,
            RobertsAnimeCornerStore,
            Waterstones,
            SciFier
        }

        public enum BookType
        {
            Manga,
            LightNovel
        }

        [Flags]
        public enum Region
        {
            America,
            Britain,
            Canada,
            Europe,
            Japan
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
            NA
        }
    }
}