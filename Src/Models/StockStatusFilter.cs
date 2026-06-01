namespace MangaAndLightNovelWebScrape.Models
{
    /// <summary>
    /// Pre-built <see cref="StockStatus"/> filter sets for <see cref="MasterScrape.Filter"/>.
    /// Pass one of these arrays to drop entries whose status is in the set; pass
    /// <see cref="EXCLUDE_NONE_FILTER"/> (the default) to keep everything.
    /// <para>
    /// <see cref="StockStatus.NA"/> is always implicitly excluded by the filter pipeline,
    /// regardless of which preset is selected.
    /// </para>
    /// </summary>
    public readonly struct StockStatusFilter
    {
        /// <summary>Empty filter — every entry is kept. Default for <see cref="MasterScrape.Filter"/>.</summary>
        public static readonly StockStatus[] EXCLUDE_NONE_FILTER = [];

        /// <summary>Excludes <see cref="StockStatus.PO"/>, <see cref="StockStatus.OOS"/>, and <see cref="StockStatus.BO"/>. Keeps only in-stock + coming-soon entries.</summary>
        public static readonly StockStatus[] EXCLUDE_ALL_FILTER = [ StockStatus.PO, StockStatus.OOS, StockStatus.BO ];

        /// <summary>Excludes <see cref="StockStatus.PO"/> entries.</summary>
        public static readonly StockStatus[] EXCLUDE_PO_FILTER = [ StockStatus.PO ];

        /// <summary>Excludes <see cref="StockStatus.OOS"/> entries.</summary>
        public static readonly StockStatus[] EXCLUDE_OOS_FILTER = [ StockStatus.OOS ];

        /// <summary>Excludes <see cref="StockStatus.BO"/> entries.</summary>
        public static readonly StockStatus[] EXCLUDE_BO_FILTER = [ StockStatus.BO ];

        /// <summary>Excludes <see cref="StockStatus.OOS"/> and <see cref="StockStatus.BO"/> entries.</summary>
        public static readonly StockStatus[] EXCLUDE_OOS_AND_BO_FILTER = [ StockStatus.OOS, StockStatus.BO ];

        /// <summary>Excludes <see cref="StockStatus.PO"/> and <see cref="StockStatus.BO"/> entries.</summary>
        public static readonly StockStatus[] EXCLUDE_PO_AND_BO_FILTER = [ StockStatus.PO, StockStatus.BO ];

        /// <summary>Excludes <see cref="StockStatus.OOS"/> and <see cref="StockStatus.PO"/> entries. Useful for "show me what I can buy right now".</summary>
        public static readonly StockStatus[] EXCLUDE_OOS_AND_PO_FILTER = [ StockStatus.OOS, StockStatus.PO ];
    }
}
