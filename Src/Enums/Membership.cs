namespace MangaAndLightNovelWebScrape.Enums;

/// <summary>
/// Per-site membership flags. Many retailers offer member-only pricing; tag the sites
/// where the consumer holds an account so the scraper picks the discounted price column
/// instead of the public price. Combine with <c>|</c>.
/// </summary>
/// <example>
/// <code>
/// MasterScrape scrape = new(
///     StockStatusFilter.EXCLUDE_NONE_FILTER,
///     Memberships: Membership.BooksAMillion | Membership.KinokuniyaUSA);
/// </code>
/// </example>
[Flags]
public enum Membership
{
    /// <summary>No memberships — every site uses its public-facing price column.</summary>
    None = 0,
    /// <summary>Books-A-Million Millionaire's Club member pricing.</summary>
    BooksAMillion = 1 << 0,
    /// <summary>Kinokuniya USA Bookweb member pricing.</summary>
    KinokuniyaUSA = 1 << 1,
}
