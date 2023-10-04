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
            InStockTrades,
            KinokuniyaUSA,
            RightStufAnime,
            RobertsAnimeCornerStore,
            Indigo
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

        public enum StockStatus
        {
            IS,
            OOS,
            PO,
            NA
        }
    }
}