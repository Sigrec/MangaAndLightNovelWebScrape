namespace MangaAndLightNovelWebScrape.Enums;

/// <summary>
/// Per-site membership flags. Many retailers offer member-only pricing; tag the sites where
/// the consumer holds an account so the scraper picks the discounted price column.
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
    None = 0,
    BooksAMillion = 1 << 0,
    KinokuniyaUSA = 1 << 1,
}
