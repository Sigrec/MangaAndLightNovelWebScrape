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

        public enum Region
        {
            America,
            Canada,
            Britain,
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