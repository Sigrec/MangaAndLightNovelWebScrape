using System.Reflection;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Internal;

/// <summary>
/// Guard tests that every implemented <see cref="Website"/> is wired into every helper that
/// fans out per-site. The bugs this catches are mechanical: add a new Website enum value,
/// register it in three of the four registries, miss the fourth, and a live scrape silently
/// drops the site (or worse — throws <see cref="NotImplementedException"/> mid-run).
///
/// Source of truth is <see cref="_implementedSites"/>. When a new site ships, add it there
/// and every test in this fixture fans out to it automatically.
/// </summary>
[TestFixture, Description("Guard tests that every implemented Website is wired through every helper")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public sealed class WebsiteEnumSurfaceTests
{
    // Sites we ship with a working scraper today. AmazonJapan and CDJapan are declared in
    // the Website enum but have no scraper class — they're intentionally excluded so the
    // CanBeConstructed test doesn't trip on them. Add new entries when landing a site.
    private static readonly Website[] _implementedSites =
    [
        Website.AllStarComics, Website.AmazonUSA, Website.BooksAMillion,
        Website.Crunchyroll, Website.ForbiddenPlanet, Website.InStockTrades,
        Website.KingsComics, Website.KinokuniyaUSA, Website.MangaMart,
        Website.MangaMate, Website.MerryManga, Website.OKComics,
        Website.RobertsAnimeCornerStore, Website.SciFier, Website.TravellingMan,
    ];

    [TestCaseSource(nameof(_implementedSites))]
    public void Site_IsInWebsiteTitleMap(Website site)
        => Assert.That(
            Helpers.WebsiteTitleMap.Values, Contains.Item(site),
            $"{site} is missing from Helpers.WebsiteTitleMap — GetWebsiteFromString cannot resolve it.");

    [TestCaseSource(nameof(_implementedSites))]
    public void Site_IsInAtLeastOneRegion(Website site)
        => Assert.That(
            Helpers.WebsitesByRegion.Values.Any(arr => arr.Contains(site)), Is.True,
            $"{site} is not registered in any Helpers.WebsitesByRegion entry — IsWebsiteListValid will reject it.");

    [TestCaseSource(nameof(_implementedSites))]
    public void Site_HasNonEmptyUrl(Website site)
    {
        string url = Helpers.GetWebsiteLink(site, Region.America);
        Assert.That(url, Is.Not.Null.And.Not.Empty);
    }

    [TestCaseSource(nameof(_implementedSites))]
    public void Site_CanBeConstructed(Website site)
    {
        IWebsite scraper = InternalHelpers.CreateScraper(site, NullLoggerFactory.Instance);
        Assert.That(scraper, Is.Not.Null);
    }

    /// <summary>
    /// Each site exposes <c>public const Region REGION</c>. Verifies that the constant on
    /// the class matches the region(s) the site is listed under in
    /// <see cref="Helpers.WebsitesByRegion"/>. The bug to catch: I set
    /// <c>REGION = Region.Britain</c> on the class but only added the enum to the
    /// <see cref="Region.Australia"/> array (or forgot it entirely).
    /// </summary>
    [TestCaseSource(nameof(_implementedSites))]
    public void Site_RegionConstant_MatchesWebsitesByRegion(Website site)
    {
        Type siteType = ResolveSiteType(site);
        FieldInfo regionField = siteType.GetField(
            "REGION", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            ?? throw new InvalidOperationException($"{siteType.Name} is missing a public const REGION field.");
        Region declared = (Region)regionField.GetRawConstantValue()!;

        foreach (Region region in Enum.GetValues<Region>())
        {
            bool inMap = Helpers.WebsitesByRegion.TryGetValue(region, out Website[] sites)
                && sites.Contains(site);
            bool inDeclared = declared.HasFlag(region);

            Assert.That(inMap, Is.EqualTo(inDeclared),
                $"{site}: REGION const = {declared}. WebsitesByRegion[{region}] contains site = {inMap}, " +
                $"but the const {(inDeclared ? "claims" : "does not claim")} membership in {region}.");
        }
    }

    /// <summary>
    /// <see cref="InternalHelpers.NeedPlaywright"/> is the gate that decides whether a scrape
    /// pays the Playwright browser-launch cost. Drift between this set and the README's
    /// documented JS-rendered list means either a Playwright-needing site silently fails on
    /// HTML-only scrapes, or an HTML-only scrape pays for a browser it never uses.
    /// </summary>
    [Test]
    public void NeedPlaywright_True_ForKnownJsRenderedSites()
    {
        HashSet<Website> jsRendered =
        [
            Website.BooksAMillion, Website.KinokuniyaUSA, Website.ForbiddenPlanet,
            Website.MerryManga, Website.MangaMart, Website.MangaMate,
            Website.AmazonUSA, Website.AmazonJapan,
        ];
        Assert.That(InternalHelpers.NeedPlaywright(jsRendered), Is.True);
    }

    [Test]
    public void NeedPlaywright_False_ForKnownHtmlOnlySites()
    {
        HashSet<Website> htmlOnly =
        [
            Website.AllStarComics, Website.Crunchyroll, Website.InStockTrades,
            Website.KingsComics, Website.OKComics, Website.RobertsAnimeCornerStore,
            Website.SciFier, Website.TravellingMan,
        ];
        Assert.That(InternalHelpers.NeedPlaywright(htmlOnly), Is.False);
    }

    /// <summary>
    /// Convention: every <see cref="Website"/> enum name matches its scraper class name in
    /// the <c>MangaAndLightNovelWebScrape.Websites</c> namespace. Letting reflection resolve
    /// the type avoids a 14-case switch that would drift the next time a site is added.
    /// </summary>
    private static Type ResolveSiteType(Website site)
    {
        Assembly libAssembly = typeof(MasterScrape).Assembly;
        string typeName = $"MangaAndLightNovelWebScrape.Websites.{site}";
        return libAssembly.GetType(typeName)
            ?? throw new InvalidOperationException(
                $"No site class found at '{typeName}'. Either the convention broke or {site} is unimplemented.");
    }
}
