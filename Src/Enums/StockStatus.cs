namespace MangaAndLightNovelWebScrape.Enums
{
    /// <summary>
    /// Availability state for an <see cref="EntryModel"/>. Used both to label individual
    /// entries and as the input set for <see cref="MangaAndLightNovelWebScrape.Models.StockStatusFilter"/>.
    /// </summary>
    public enum StockStatus
    {
        /// <summary>In Stock — the entry is purchasable and shippable now.</summary>
        IS,
        /// <summary>Out of Stock — currently unavailable but listed.</summary>
        OOS,
        /// <summary>Pre-Order — not yet released; reserve before street date.</summary>
        PO,
        /// <summary>Not Available — entry exists but has no stock status the site exposes.</summary>
        NA,
        /// <summary>Backorder — the site will fulfill when restocked, no firm date.</summary>
        BO,
        /// <summary>Coming Soon — announced, not yet open for pre-order.</summary>
        CS,
    }
}
