using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using CRSite = MangaAndLightNovelWebScrape.Websites.Crunchyroll;

namespace Tests.Websites.Crunchyroll;

[TestFixture, Description("Fixture-based validations for Crunchyroll")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class CrunchyrollTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "Crunchyroll", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "Crunchyroll");

    private const string _userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    // (title, bookType, slug, legacy-expected-suffix, skip)
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachMangaData", false },
        new object[] { "attack on titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "goodbye-eri-manga", "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "dimensional-seduction-manga", "DimensionalSeductionMangaData", false },
        new object[] { "overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "cote-novel", "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "blade-and-bastard-novel", "Blade&BastardNovelData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public void Crunchyroll_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: {title}");
            return;
        }

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        if (!Directory.Exists(fixtureDir))
        {
            Assert.Ignore(
                $"Fixture directory missing: {fixtureDir}. " +
                "Run the [Explicit] Regenerate test to download the HTML snapshot.");
            return;
        }

        // page.html holds whichever listing (search or collections) had products at
        // regeneration time. The Regenerate task picks the right one and saves a single file.
        string pagePath = Path.Combine(fixtureDir, "page.html");
        if (!File.Exists(pagePath))
        {
            Assert.Ignore($"No page.html in {fixtureDir}. Re-run Regenerate.");
            return;
        }

        HtmlDocument doc = new();
        doc.Load(pagePath);

        CRSite site = new();
        List<EntryModel> actual = site.ParseProducts(doc, title, bookType);

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"Crunchyroll{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            CRSite.REGION.HasFlag(Region.America)
            && !CRSite.REGION.HasFlag(Region.Australia)
            && !CRSite.REGION.HasFlag(Region.Britain)
            && !CRSite.REGION.HasFlag(Region.Canada)
            && !CRSite.REGION.HasFlag(Region.Europe)
            && !CRSite.REGION.HasFlag(Region.Japan));
    }

    // ─── Fixture regeneration ────────────────────────────────────────────────
    //
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~Crunchyroll"     (all slugs)
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~jujutsu-kaisen-manga"  (one slug)
    //
    // For each case: hit /search?...; if no products, fall back to /collections/<slug>/;
    // save whichever returned products as Fixtures/<slug>/page.html and write
    // Fixtures/<slug>/expected.txt by running ParseProducts on the saved HTML.

    private static IEnumerable<TestCaseData> RegenerateCases()
    {
        foreach (object[] row in ScrapeTestCases)
        {
            string title = (string)row[0];
            BookType bookType = (BookType)row[1];
            string slug = (string)row[2];
            string legacy = (string)row[3];
            bool skip = (bool)row[4];
            yield return new TestCaseData(title, bookType, slug, legacy, skip).SetName(slug);
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
        CRSite site = new();
        HtmlWeb html = HtmlFactory.CreateWeb();
        html.PreRequest += req =>
        {
            req.UserAgent = _userAgent;
            return true;
        };

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        Directory.CreateDirectory(fixtureDir);

        // Clear any stale snapshots before writing the fresh one.
        foreach (string old in Directory.EnumerateFiles(fixtureDir, "*.html"))
        {
            File.Delete(old);
        }

        // Step 1: try the /search?... URL.
        string searchUrl = site.GenerateWebsiteUrl(bookType, title);
        HtmlDocument doc = await html.LoadFromWebAsync(searchUrl);
        string source = "search";

        // Step 2: if that returns no products, fall back to /collections/<slug>/.
        if (CRSite.HasProducts(doc) == 0)
        {
            string collectionsUrl = site.GenerateWebsiteUrl(bookType, title, true);
            doc = await html.LoadFromWebAsync(collectionsUrl);
            source = "collections";
        }

        File.WriteAllText(
            Path.Combine(fixtureDir, "page.html"),
            doc.DocumentNode.OuterHtml);

        List<EntryModel> parsed = site.ParseProducts(doc, title, bookType);
        File.WriteAllLines(
            Path.Combine(fixtureDir, "expected.txt"),
            parsed.Select(e => e.ToString()));

        TestContext.Progress.WriteLine($"{slug}: source={source}, {parsed.Count} entries");
    }
}
