namespace Src.Models
{
    public readonly struct StockStatusFilter
    {
        /// <summary>
        /// Default filter, excludes no stock status
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_NONE_FILTER = [];
        /// <summary>
        /// Excludes OOS (Out of Stock, PO (Pre Order) & BO (Backorder) entries
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_ALL_FILTER = [ StockStatus.PO, StockStatus.OOS, StockStatus.BO ];
        /// <summary>
        /// Exludes PO (Pre Order) entries only
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_PO_FILTER = [ StockStatus.PO ];
        /// <summary>
        /// Excludes OOS (Out of Stock) entries only
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_OOS_FILTER = [ StockStatus.OOS ];
        /// <summary>
        /// Excludes BO (Backorder) entries only
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_BO_FILTER = [ StockStatus.BO ];
        /// <summary>
        /// Excludes OOS (Out of Stock) & BO (Backorder) entries only
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_OOS_AND_BO_FILTER = [ StockStatus.OOS, StockStatus.BO ];
        /// <summary>
        /// Exludes PO (Pre Order) & BO (Backorder) entries only
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_PO_AND_BO_FILTER = [ StockStatus.PO, StockStatus.BO ];
        /// <summary>
        /// Excludes OOS (Out of Stock) & PO (Pre Order) entries only
        /// </summary>
        public static readonly StockStatus[] EXCLUDE_OOS_AND_PO_FILTER = [ StockStatus.OOS, StockStatus.PO ];
    }
}