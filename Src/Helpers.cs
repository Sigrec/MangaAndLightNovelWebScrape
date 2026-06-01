using MangaAndLightNovelWebScrape.Models;
using System.Collections.Frozen;

namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Stateless helpers exposed to library consumers — case-insensitive enum parsing,
/// region/site lookups, and URL resolution. Used to translate user-facing strings
/// (e.g. from a CLI) into the strongly-typed values <see cref="MasterScrape"/> expects.
/// </summary>
public static class Helpers
{
    // ─── Generic Enum Parser ─────────────────────────────────────────────────

    internal static T ParseEnum<T>(string value) where T : struct, Enum
        => Enum.TryParse(value, true, out T result)
           ? result
           : throw new ArgumentException($"Invalid {typeof(T).Name}: '{value}'", nameof(value));

    /// <summary>
    /// Parses a case-insensitive <see cref="Browser"/> name. Throws
    /// <see cref="ArgumentException"/> if the value doesn't match a defined enum member.
    /// </summary>
    public static Browser GetBrowserFromString(string browser)
        => ParseEnum<Browser>(browser);

    /// <summary>
    /// Parses a case-insensitive <see cref="Region"/> name. Throws
    /// <see cref="ArgumentException"/> if the value doesn't match a defined enum member.
    /// </summary>
    public static Region GetRegionFromString(string region)
        => ParseEnum<Region>(region);

    // ─── StockStatus & Filters ────────────────────────────────────────────────

    internal static readonly FrozenDictionary<string, StockStatus> StockStatusMap
        = new Dictionary<string, StockStatus>(StringComparer.OrdinalIgnoreCase)
        {
            ["is"] = StockStatus.IS,
            ["instock"] = StockStatus.IS,
            ["po"] = StockStatus.PO,
            ["pre-order"] = StockStatus.PO,
            ["preorder"] = StockStatus.PO,
            ["oos"] = StockStatus.OOS,
            ["outofstock"] = StockStatus.OOS,
            ["bo"] = StockStatus.BO,
            ["backorder"] = StockStatus.BO,
        }.ToFrozenDictionary();

    /// <summary>
    /// Maps a free-form stock status string (e.g. <c>"in stock"</c>, <c>"pre-order"</c>,
    /// <c>"OOS"</c>) to the corresponding <see cref="StockStatus"/>. Returns
    /// <see cref="StockStatus.NA"/> for unrecognized values.
    /// </summary>
    public static StockStatus GetStockStatusFromString(string status)
        => StockStatusMap.TryGetValue(status, out StockStatus st)
           ? st
           : StockStatus.NA;

    internal static readonly FrozenDictionary<string, StockStatus[]> StockFilterMap
        = new Dictionary<string, StockStatus[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["exclude all"] = StockStatusFilter.EXCLUDE_ALL_FILTER,
            ["all"] = StockStatusFilter.EXCLUDE_ALL_FILTER,
            ["exclude oos & po"] = StockStatusFilter.EXCLUDE_OOS_AND_PO_FILTER,
            ["oos & po"] = StockStatusFilter.EXCLUDE_OOS_AND_PO_FILTER,
            ["exclude oos & bo"] = StockStatusFilter.EXCLUDE_OOS_AND_BO_FILTER,
            ["oos & bo"] = StockStatusFilter.EXCLUDE_OOS_AND_BO_FILTER,
            ["exclude po & bo"] = StockStatusFilter.EXCLUDE_PO_AND_BO_FILTER,
            ["po & bo"] = StockStatusFilter.EXCLUDE_PO_AND_BO_FILTER,
            ["exclude oos"] = StockStatusFilter.EXCLUDE_OOS_FILTER,
            ["oos"] = StockStatusFilter.EXCLUDE_OOS_FILTER,
            ["exclude po"] = StockStatusFilter.EXCLUDE_PO_FILTER,
            ["po"] = StockStatusFilter.EXCLUDE_PO_FILTER,
            ["exclude bo"] = StockStatusFilter.EXCLUDE_BO_FILTER,
            ["bo"] = StockStatusFilter.EXCLUDE_BO_FILTER,
        }.ToFrozenDictionary();

    /// <summary>
    /// Maps a free-form filter name (e.g. <c>"exclude oos"</c>, <c>"all"</c>) to one of
    /// the pre-built filter arrays in <see cref="StockStatusFilter"/>. Returns
    /// <see cref="StockStatusFilter.EXCLUDE_NONE_FILTER"/> for unrecognized values.
    /// </summary>
    public static StockStatus[] GetStockStatusFilterFromString(string filter)
        => StockFilterMap.TryGetValue(filter, out StockStatus[]? arr)
           ? arr
           : StockStatusFilter.EXCLUDE_NONE_FILTER;

    // ─── Websites & Regions ───────────────────────────────────────────────────

    internal static readonly FrozenDictionary<Region, Website[]> WebsitesByRegion
        = new Dictionary<Region, Website[]>
        {
            [Region.America] =
            [
                Website.AmazonUSA, Website.BooksAMillion, Website.Crunchyroll,
                Website.InStockTrades, Website.KinokuniyaUSA, Website.MangaMart,
                Website.MerryManga, Website.RobertsAnimeCornerStore, Website.SciFier
            ],
            [Region.Australia] =
            [
                Website.MangaMate, Website.SciFier
            ],
            [Region.Britain] =
            [
                Website.ForbiddenPlanet, Website.SciFier,
                Website.TravellingMan, 
            ],
            [Region.Canada] =
            [
                Website.SciFier
            ],
            [Region.Europe] =
            [
                Website.SciFier
            ],
            [Region.Japan] = []
        }.ToFrozenDictionary();

    internal static readonly FrozenDictionary<Region, Website[]> MembershipSitesByRegion
        = new Dictionary<Region, Website[]>
        {
            [Region.America] = [Website.BooksAMillion, Website.KinokuniyaUSA]
        }.ToFrozenDictionary();

    internal static readonly FrozenDictionary<string, Website> WebsiteTitleMap
        = new Dictionary<string, Website>(StringComparer.OrdinalIgnoreCase)
        {
            [AmazonUSA.TITLE] = Website.AmazonUSA,
            [BooksAMillion.TITLE] = Website.BooksAMillion,
            [Crunchyroll.TITLE] = Website.Crunchyroll,
            [InStockTrades.TITLE] = Website.InStockTrades,
            [KinokuniyaUSA.TITLE] = Website.KinokuniyaUSA,
            [MangaMart.TITLE] = Website.MangaMart,
            [MangaMate.TITLE] = Website.MangaMate,
            [MerryManga.TITLE] = Website.MerryManga,
            [RobertsAnimeCornerStore.TITLE] = Website.RobertsAnimeCornerStore,
            [SciFier.TITLE] = Website.SciFier,
            [ForbiddenPlanet.TITLE] = Website.ForbiddenPlanet,
            [TravellingMan.TITLE] = Website.TravellingMan,
        }.ToFrozenDictionary();

    /// <summary>
    /// Maps a case-insensitive site title (e.g. <c>"Crunchyroll"</c>, <c>"SciFier"</c>) to
    /// its <see cref="Website"/> enum value. Throws <see cref="ArgumentException"/> when no
    /// site is registered under that title.
    /// </summary>
    public static Website GetWebsiteFromString(string title)
        => WebsiteTitleMap.TryGetValue(title, out Website w)
           ? w
           : throw new ArgumentException($"Unknown website: '{title}'", nameof(title));

    /// <summary>
    /// Returns the set of <see cref="Website"/>s supported in the given <see cref="Region"/>.
    /// Empty array for regions with no current implementation (e.g. <see cref="Region.Japan"/>).
    /// </summary>
    public static Website[] GetRegionWebsiteList(Region region)
        => WebsitesByRegion.TryGetValue(region, out Website[]? arr) ? arr : [];

    /// <summary>
    /// Same as <see cref="GetRegionWebsiteList(Region)"/> but returns the enum names as
    /// strings — convenient for UI dropdowns and CLI help text.
    /// </summary>
    public static string[] GetRegionWebsiteListAsString(Region region)
        => [.. GetRegionWebsiteList(region).Select(w => w.ToString())];

    /// <summary>
    /// Returns the subset of sites in <paramref name="region"/> that expose member-only
    /// pricing — i.e. the sites worth tagging in <see cref="MasterScrape.Memberships"/>.
    /// </summary>
    public static Website[] GetMembershipWebsitesForRegion(Region region)
        => MembershipSitesByRegion.TryGetValue(region, out Website[]? arr) ? arr : [];

    /// <summary>
    /// Same as <see cref="GetMembershipWebsitesForRegion(Region)"/> but returns enum names
    /// as strings.
    /// </summary>
    public static string[] GetMembershipWebsitesForRegionAsString(Region region)
        => [.. GetMembershipWebsitesForRegion(region).Select(w => w.ToString())];

    /// <summary>
    /// Returns <c>true</c> when every site title in <paramref name="input"/> is supported
    /// by <paramref name="region"/>. Unknown titles throw <see cref="ArgumentException"/>
    /// via <see cref="GetWebsiteFromString(string)"/>.
    /// </summary>
    public static bool IsWebsiteListValid(Region region, IEnumerable<string> input)
        => input.Select(GetWebsiteFromString).All(w => WebsitesByRegion[region].Contains(w));

    /// <summary>
    /// Returns <c>true</c> when every <see cref="Website"/> in <paramref name="input"/> is
    /// supported by <paramref name="region"/>.
    /// </summary>
    public static bool IsWebsiteListValid(Region region, IEnumerable<Website> input)
        => input.All(w => WebsitesByRegion[region].Contains(w));

    /// <summary>
    /// Returns the home/storefront URL for a site, including any region-keyed query
    /// parameters (e.g. SciFier's currency selector). Used by reachability checks and the
    /// per-site link entries in <see cref="MasterScrape.GetResultUrls"/>.
    /// </summary>
    /// <param name="site">Site to look up.</param>
    /// <param name="region">Active scrape region — only consulted for multi-region sites.</param>
    public static string GetWebsiteLink(Website site, Region region = Region.America)
    {
        if (site == Website.SciFier)
        {
            int id = region switch
            {
                Region.Britain => 1,
                Region.America => 2,
                Region.Australia => 3,
                Region.Europe => 5,
                Region.Canada => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(region))
            };
            return $"{SciFier.BASE_URL}/?setCurrencyId={id}";
        }

        return site switch
        {
            Website.AmazonUSA => AmazonUSA.BASE_URL,
            Website.BooksAMillion => BooksAMillion.BASE_URL,
            Website.CDJapan => CDJapan.WEBSITE_URL,
            Website.Crunchyroll => Crunchyroll.BASE_URL,
            Website.ForbiddenPlanet => ForbiddenPlanet.BASE_URL,
            Website.InStockTrades => InStockTrades.BASE_URL,
            Website.KinokuniyaUSA => KinokuniyaUSA.BASE_URL,
            Website.MangaMart => MangaMart.BASE_URL,
            Website.MangaMate => MangaMate.BASE_URL,
            Website.MerryManga => MerryManga.BASE_URL,
            Website.RobertsAnimeCornerStore => RobertsAnimeCornerStore.BASE_URL,
            Website.TravellingMan => TravellingMan.BASE_URL,
            _ => throw new NotImplementedException($"No URL for {site}")
        };
    }

    /// <summary>
    /// Overload of <see cref="GetWebsiteLink(Website, Region)"/> that accepts a case-insensitive
    /// site title string instead of the enum value.
    /// </summary>
    public static string GetWebsiteLink(string title, Region region = Region.America)
        => GetWebsiteLink(GetWebsiteFromString(title), region);
}