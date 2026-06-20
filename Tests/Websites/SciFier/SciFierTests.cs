using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using SciSite = MangaAndLightNovelWebScrape.Websites.SciFier;

namespace Tests.Websites.SciFier;

[TestFixture, Description("Fixture-based validations for SciFier")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class SciFierTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "SciFier", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "SciFier");

    // (title, bookType, region, slug, legacy-expected-suffix, skip)
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, Region.Australia, "akane-banashi-manga", "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, Region.Europe, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, Region.Canada, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", false },
        new object[] { "one piece", BookType.Manga, Region.America, "one-piece-manga", "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, Region.America, "naruto-manga", "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, Region.America, "naruto-novel", "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, Region.America, "bleach-manga", "BleachMangaData", false },
        new object[] { "attack on titan", BookType.Manga, Region.America, "attack-on-titan-manga", "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, Region.Britain, "goodbye-eri-manga", "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, Region.America, "dimensional-seduction-manga", "DimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, Region.America, "overlord-novel", "OverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, Region.America, "overlord-manga", "OverlordMangaData", false },
        new object[] { "07-ghost", BookType.Manga, Region.America, "07-ghost-manga", "07GhostMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, Region.America, "fmab-manga", "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, Region.America, "berserk-manga", "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, Region.America, "toilet-manga", "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, Region.America, "cote-novel", "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, Region.America, "boruto-manga", "BorutoMangaData", false },
        new object[] { "Noragami", BookType.Manga, Region.America, "noragami-manga", "NoragamiMangaData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task SciFier_Scrape_Test(string title, BookType bookType, Region region, string slug, string legacy, bool skip)
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

        // Load listing pages from page-*.html, ordered numerically.
        string[] pageFiles = Directory.GetFiles(fixtureDir, "page-*.html")
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (pageFiles.Length == 0)
        {
            Assert.Ignore($"No page-*.html files in {fixtureDir}. Re-run Regenerate.");
            return;
        }

        List<HtmlDocument> listingPages = [];
        foreach (string pageFile in pageFiles)
        {
            HtmlDocument doc = new();
            doc.Load(pageFile);
            listingPages.Add(doc);
        }

        // Load desc pages keyed by href. The DescIndex.txt manifest maps href → filename.
        Dictionary<string, HtmlDocument> descByHref = new(StringComparer.OrdinalIgnoreCase);
        string descManifest = Path.Combine(fixtureDir, "desc-index.txt");
        if (File.Exists(descManifest))
        {
            foreach (string line in File.ReadAllLines(descManifest))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                int sep = line.IndexOf('\t');
                if (sep < 0) continue;
                string href = line.Substring(0, sep);
                string filename = line.Substring(sep + 1);
                string descPath = Path.Combine(fixtureDir, filename);
                if (!File.Exists(descPath)) continue;
                HtmlDocument descDoc = new();
                descDoc.Load(descPath);
                descByHref[href] = descDoc;
            }
        }

        SciSite site = new();
        List<EntryModel> actual = await site.ParsePages(
            listingPages,
            title,
            bookType,
            SciSite.CreateOfflineDescResolver(descByHref));

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"SciFier{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            SciSite.REGION.HasFlag(Region.America)
            && SciSite.REGION.HasFlag(Region.Australia)
            && SciSite.REGION.HasFlag(Region.Britain)
            && SciSite.REGION.HasFlag(Region.Canada)
            && SciSite.REGION.HasFlag(Region.Europe)
            && !SciSite.REGION.HasFlag(Region.Japan));
    }

    // ─── Fixture regeneration ────────────────────────────────────────────────
    //
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~SciFier"     (all slugs)
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~jujutsu-kaisen-manga"  (one slug)
    //
    // Per case:
    //   1. Walk SciFier's pagination, saving each listing page as page-NNN.html.
    //   2. Whenever ParsePages requests a detail page (LightNovel pre-pass or box-set
    //      check), download it via HtmlWeb, store it as desc-NNN.html, and record an
    //      `<href>\t<filename>` entry in desc-index.txt.
    //   3. Re-run ParsePages against the saved fixtures to write expected.txt.

    private static IEnumerable<TestCaseData> RegenerateCases()
    {
        foreach (object[] row in ScrapeTestCases)
        {
            string title = (string)row[0];
            BookType bookType = (BookType)row[1];
            Region region = (Region)row[2];
            string slug = (string)row[3];
            string legacy = (string)row[4];
            bool skip = (bool)row[5];
            yield return new TestCaseData(title, bookType, region, slug, legacy, skip).SetName(slug);
        }
    }

    [TestCaseSource(nameof(RegenerateCases))]
    [Explicit("Hits the live site. Run on-demand to refresh a single slug's fixtures.")]
    [Category("RegenerateFixtures")]
    public async Task Regenerate(string title, BookType bookType, Region region, string slug, string legacy, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        Directory.CreateDirectory(FixturesRoot);
        SciSite site = new();
        HtmlWeb html = HtmlFactory.CreateWeb();

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        Directory.CreateDirectory(fixtureDir);

        // Clear any stale fixtures so the new snapshot is internally consistent.
        foreach (string old in Directory.EnumerateFiles(fixtureDir, "*.html"))
        {
            File.Delete(old);
        }
        string staleManifest = Path.Combine(fixtureDir, "desc-index.txt");
        if (File.Exists(staleManifest)) File.Delete(staleManifest);

        // ─── Step 1: walk pagination, capturing each listing page. ─────────────
        bool letterIsFrontHalf = char.IsDigit(title[0]) || (title[0] & 0b11111) <= 13;
        string url = site.GenerateWebsiteUrl(title, bookType, region, letterIsFrontHalf);

        List<HtmlDocument> listingPages = [];
        HtmlDocument doc = await html.LoadFromWebAsync(url);
        listingPages.Add(doc);
        File.WriteAllText(
            Path.Combine(fixtureDir, $"page-{listingPages.Count:D3}.html"),
            doc.DocumentNode.OuterHtml);

        const int MaxPages = 50;
        while (listingPages.Count < MaxPages)
        {
            if (site.ShouldStopPagination(doc, title, letterIsFrontHalf, out bool stop))
            {
                if (stop) break;
                // Irrelevant page — fall through to fetch the next.
            }

            HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//a[@aria-label='Next']");
            if (pageCheck == null) break;

            string nextUrl = $"https://scifier.com{System.Net.WebUtility.HtmlDecode(pageCheck.GetAttributeValue("href", "Url Error"))}";
            doc = await html.LoadFromWebAsync(nextUrl);
            listingPages.Add(doc);
            File.WriteAllText(
                Path.Combine(fixtureDir, $"page-{listingPages.Count:D3}.html"),
                doc.DocumentNode.OuterHtml);
        }

        // ─── Step 2: run ParsePages once with a recording resolver that saves each ──
        //         detail page it requests, then re-run with the offline resolver to
        //         confirm the saved fixtures produce identical output.
        Dictionary<string, HtmlDocument> recorded = new(StringComparer.OrdinalIgnoreCase);
        int descCounter = 0;
        List<string> manifest = [];

        Func<string, Task<HtmlDocument>> recordingResolver = async href =>
        {
            if (recorded.TryGetValue(href, out HtmlDocument cached)) return cached;
            HtmlDocument descDoc = await html.LoadFromWebAsync(href);
            descCounter++;
            string filename = $"desc-{descCounter:D3}.html";
            File.WriteAllText(
                Path.Combine(fixtureDir, filename),
                descDoc.DocumentNode.OuterHtml);
            manifest.Add($"{href}\t{filename}");
            recorded[href] = descDoc;
            return descDoc;
        };

        List<EntryModel> parsed = await site.ParsePages(
            listingPages,
            title,
            bookType,
            recordingResolver);

        File.WriteAllLines(Path.Combine(fixtureDir, "desc-index.txt"), manifest);
        File.WriteAllLines(
            Path.Combine(fixtureDir, "expected.txt"),
            parsed.Select(e => e.ToString()));

        TestContext.Progress.WriteLine(
            $"{slug}: {listingPages.Count} listing page(s), {recorded.Count} desc page(s), {parsed.Count} entries");
    }
}
