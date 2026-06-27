using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using ISTSite = MangaAndLightNovelWebScrape.Websites.InStockTrades;

namespace Tests.Websites.InStockTrades;

[TestFixture, Description("Fixture-based validations for InStockTrades")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class InStockTradesTests
{
    // Tests load HTML snapshots from this directory instead of hitting the live site.
    // Each subdirectory matches `<slug>` from a row in ScrapeTestCases and holds one or
    // more `page-N.html` files plus an `expected.txt` regenerated alongside the HTML.
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "InStockTrades", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "InStockTrades");

    // (title, bookType, slug, legacy-expected-suffix, skip).
    // - slug: directory name under Fixtures/, used by RegenerateFixtures and the test.
    // - legacy: filename suffix of the pre-existing expected .txt (`InStockTrades<X>.txt`).
    //   Kept so the migration doesn't have to rename every fixture in one commit.
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", true },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false },
        new object[] { "Overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false },
        new object[] { "Fullmetal Alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false },
        new object[] { "Fullmetal Alchemist", BookType.LightNovel, "fmab-novel", "FMABNovelData", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false },
        new object[] { "classroom of elite", BookType.LightNovel, "cote-novel", "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", false },
        new object[] { "Spice & Wolf", BookType.LightNovel, "spice-and-wolf-novel", "Spice&WolfNovelData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public void InStockTrades_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        if (!Directory.Exists(fixtureDir))
        {
            Assert.Ignore(
                $"Fixture directory missing: {fixtureDir}. " +
                "Run the [Explicit] RegenerateFixtures test to download HTML snapshots.");
            return;
        }

        string[] pageFiles = Directory.GetFiles(fixtureDir, "page-*.html")
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (pageFiles.Length == 0)
        {
            Assert.Ignore($"No page-*.html files in {fixtureDir}. Re-run RegenerateFixtures.");
            return;
        }

        HtmlDocument[] docs = new HtmlDocument[pageFiles.Length];
        for (int i = 0; i < pageFiles.Length; i++)
        {
            HtmlDocument doc = new();
            doc.Load(pageFiles[i]);
            docs[i] = doc;
        }

        ISTSite site = new();
        List<EntryModel> actual = site.ParsePages(docs, title, bookType);

        // Prefer the regenerated expected.txt that lives alongside the HTML; fall back to
        // the pre-existing per-suite txt fixture during the migration window.
        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"InStockTrades{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            ISTSite.REGION.HasFlag(Region.America)
            && !ISTSite.REGION.HasFlag(Region.Australia)
            && !ISTSite.REGION.HasFlag(Region.Britain)
            && !ISTSite.REGION.HasFlag(Region.Canada)
            && !ISTSite.REGION.HasFlag(Region.Europe)
            && !ISTSite.REGION.HasFlag(Region.Japan));
    }

    // ─── Fixture regeneration ────────────────────────────────────────────────
    //
    // Run only when you want to refresh HTML snapshots and the matching expected output.
    // Marked [Explicit] so a plain `dotnet test` skips them; the [Category] filter is how
    // the NUnit adapter overrides Explicit at run time. Each row is its own test case so
    // you can pick exactly the scope you want:
    //
    //   # Refresh every slug across every site
    //   dotnet test --filter "TestCategory=RegenerateFixtures"
    //
    //   # Refresh every slug for InStockTrades (the substring matches the class name)
    //   dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~InStockTrades"
    //
    //   # Refresh one specific slug (the substring matches the SetName value)
    //   dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~jujutsu-kaisen-manga"
    //
    // What each case does:
    //   1. Downloads the live listing's page 1 via the site's own URL builder, discovers
    //      max-pages, downloads the rest in parallel, and writes them to
    //      Fixtures/<slug>/page-NNN.html.
    //   2. Runs ParsePages on the freshly downloaded HTML and writes the resulting entries
    //      to Fixtures/<slug>/expected.txt, locking the HTML and expected output together
    //      so a future run against the same HTML can never produce a different result.

    private static IEnumerable<TestCaseData> RegenerateCases()
    {
        foreach (object[] row in ScrapeTestCases)
        {
            string title = (string)row[0];
            BookType bookType = (BookType)row[1];
            string slug = (string)row[2];
            string legacy = (string)row[3];
            bool skip = (bool)row[4];
            // SetName makes the per-case test's display name be the slug, so the
            // `FullyQualifiedName~<slug>` filter selects exactly one case.
            yield return new TestCaseData(title, bookType, slug, legacy, skip)
                .SetName(slug);
        }
    }

    [TestCaseSource(nameof(RegenerateCases))]
    [Explicit("Hits the live site. Run on-demand to refresh a single slug's fixtures.")]
    [Category("RegenerateFixtures")]
    public async Task Regenerate(string title, BookType bookType, string slug, string legacy, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        Directory.CreateDirectory(FixturesRoot);
        ISTSite site = new();
        HtmlWeb html = HtmlFactory.CreateWeb();

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        Directory.CreateDirectory(fixtureDir);

        // Clear any stale page-*.html so the new snapshot is consistent.
        foreach (string old in Directory.EnumerateFiles(fixtureDir, "page-*.html"))
        {
            File.Delete(old);
        }

        string normalizedTitle = title.Contains('&') ? title.Replace("&", "and") : title;

        string firstUrl = site.GenerateWebsiteUrl(1, normalizedTitle);
        HtmlDocument firstPage = await html.LoadFromWebAsync(firstUrl);
        File.WriteAllText(
            Path.Combine(fixtureDir, "page-001.html"),
            firstPage.DocumentNode.OuterHtml);

        uint maxPages = ISTSite.GetMaxPages(firstPage);
        HtmlDocument[] pages = new HtmlDocument[maxPages];
        pages[0] = firstPage;
        if (maxPages > 1)
        {
            Task<HtmlDocument>[] fetches = new Task<HtmlDocument>[maxPages - 1];
            for (uint p = 2; p <= maxPages; p++)
            {
                fetches[p - 2] = html.LoadFromWebAsync(site.GenerateWebsiteUrl(p, normalizedTitle));
            }
            HtmlDocument[] others = await Task.WhenAll(fetches);
            for (int i = 0; i < others.Length; i++)
            {
                pages[i + 1] = others[i];
                File.WriteAllText(
                    Path.Combine(fixtureDir, $"page-{i + 2:D3}.html"),
                    others[i].DocumentNode.OuterHtml);
            }
        }

        // Re-run the parser on the saved HTML so the expected.txt always reflects what
        // ParsePages produces for that exact input.
        List<EntryModel> parsed = site.ParsePages(pages, title, bookType);
        File.WriteAllLines(
            Path.Combine(fixtureDir, "expected.txt"),
            parsed.Select(e => e.ToString()));

        TestContext.Progress.WriteLine($"{slug}: {pages.Length} page(s), {parsed.Count} entries");
    }
}
