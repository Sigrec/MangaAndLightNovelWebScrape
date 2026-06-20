using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using RACSSite = MangaAndLightNovelWebScrape.Websites.RobertsAnimeCornerStore;

namespace Tests.Websites.RobertsAnimeCornerStore;

[TestFixture, Description("Fixture-based validations for RobertsAnimeCornerStore")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class RobertsAnimeCornerStoreTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "RobertsAnimeCornerStore", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "RobertsAnimeCornerStore");

    // (title, bookType, slug, legacy-expected-suffix, skip)
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false },
        new object[] { "Overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false },
        new object[] { "Fullmetal Alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "cote-novel", "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", false },
        new object[] { "Persona 4", BookType.Manga, "persona-4-manga", "Persona4MangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "dimensional-seduction-manga", "DimensionalSeductionMangaData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "blade-and-bastard-novel", "Blade&BastardNovelData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public void RobertsAnimeCornerStore_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip)
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
                "Run the [Explicit] Regenerate task to download HTML snapshots.");
            return;
        }

        // landing.html is the alphabetic index. series-*.html are the per-series pages
        // whose URLs were extracted from the landing.
        string landingPath = Path.Combine(fixtureDir, "landing.html");
        if (!File.Exists(landingPath))
        {
            Assert.Ignore($"No landing.html in {fixtureDir}. Re-run Regenerate.");
            return;
        }

        string[] seriesFiles = Directory.GetFiles(fixtureDir, "series-*.html")
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        List<HtmlDocument> seriesPages = [];
        foreach (string p in seriesFiles)
        {
            HtmlDocument d = new();
            d.Load(p);
            seriesPages.Add(d);
        }

        RACSSite site = new();
        List<EntryModel> actual = site.ParseSeriesPages(seriesPages, title, bookType);

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"RobertsAnimeCornerStore{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            RACSSite.REGION.HasFlag(Region.America)
            && !RACSSite.REGION.HasFlag(Region.Australia)
            && !RACSSite.REGION.HasFlag(Region.Britain)
            && !RACSSite.REGION.HasFlag(Region.Canada)
            && !RACSSite.REGION.HasFlag(Region.Europe)
            && !RACSSite.REGION.HasFlag(Region.Japan));
    }

    // ─── Fixture regeneration ────────────────────────────────────────────────
    //
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~RobertsAnimeCornerStore"   (all)
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~jujutsu-kaisen-manga&FullyQualifiedName~RobertsAnime"   (one)
    //
    // For each case:
    //   1. Download the alphabetic landing page that matches the book title's first letter
    //      and save it as Fixtures/<slug>/landing.html.
    //   2. Run SelectSeriesLinks on the saved landing to discover matching series URLs.
    //   3. Download each series page and save as Fixtures/<slug>/series-NNN.html, recording
    //      the URL→filename mapping in series-index.txt.
    //   4. Re-run ParseSeriesPages against the saved series fixtures and write expected.txt.

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
        RACSSite site = new();
        HtmlWeb html = HtmlFactory.CreateWeb();

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        Directory.CreateDirectory(fixtureDir);

        // Clear any stale fixtures so the snapshot is internally consistent.
        foreach (string old in Directory.EnumerateFiles(fixtureDir, "*.html"))
        {
            File.Delete(old);
        }
        string indexPath = Path.Combine(fixtureDir, "series-index.txt");
        if (File.Exists(indexPath)) File.Delete(indexPath);

        // Step 1: landing.
        string landingUrl = site.GenerateWebsiteUrl(title);
        HtmlDocument landing = await html.LoadFromWebAsync(landingUrl);
        File.WriteAllText(
            Path.Combine(fixtureDir, "landing.html"),
            landing.DocumentNode.OuterHtml);

        // Step 2: discover matching series URLs from the saved landing.
        List<string> seriesUrls = RACSSite.SelectSeriesLinks(landing, title, bookType);

        // Step 3: fetch each series page and save it.
        List<HtmlDocument> seriesPages = [];
        List<string> manifest = [];
        for (int i = 0; i < seriesUrls.Count; i++)
        {
            HtmlDocument seriesDoc = await html.LoadFromWebAsync(seriesUrls[i]);
            string filename = $"series-{i + 1:D3}.html";
            File.WriteAllText(
                Path.Combine(fixtureDir, filename),
                seriesDoc.DocumentNode.OuterHtml);
            manifest.Add($"{seriesUrls[i]}\t{filename}");
            seriesPages.Add(seriesDoc);
        }
        File.WriteAllLines(indexPath, manifest);

        // Step 4: parse and write expected.
        List<EntryModel> parsed = site.ParseSeriesPages(seriesPages, title, bookType);
        File.WriteAllLines(
            Path.Combine(fixtureDir, "expected.txt"),
            parsed.Select(e => e.ToString()));

        TestContext.Progress.WriteLine(
            $"{slug}: landing + {seriesUrls.Count} series page(s), {parsed.Count} entries");
    }
}
