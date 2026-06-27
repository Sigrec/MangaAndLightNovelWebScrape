using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using OKCSite = MangaAndLightNovelWebScrape.Websites.OKComics;

namespace Tests.Websites.OKComics;

[TestFixture, Description("Fixture-based validations for OK Comics")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class OKComicsTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "OKComics", "Fixtures");

    // (title, bookType, slug, skip)
    // OK Comics is manga-only; no LightNovel cases.
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Jujutsu Kaisen", BookType.Manga, "jujutsu-kaisen-manga", false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", false },
        new object[] { "Attack on Titan", BookType.Manga, "attack-on-titan-manga", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task OKComics_Scrape_Test(string title, BookType bookType, string slug, bool skip)
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

        string[] pageFiles = Directory.GetFiles(fixtureDir, "page-*.html")
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (pageFiles.Length == 0)
        {
            Assert.Ignore($"No page-*.html files in {fixtureDir}. Re-run Regenerate.");
            return;
        }

        List<HtmlDocument> listingPages = [];
        foreach (string p in pageFiles)
        {
            HtmlDocument doc = new();
            doc.Load(p);
            listingPages.Add(doc);
        }

        // Desc lookup keyed by href via the manifest written at regeneration time.
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

        OKCSite site = new();
        List<EntryModel> actual = await site.ParsePages(
            listingPages,
            title,
            bookType,
            OKCSite.CreateOfflineDescResolver(descByHref));

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            Assert.Ignore($"No expected.txt in {fixtureDir}. Re-run Regenerate.");
            return;
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            !OKCSite.REGION.HasFlag(Region.America)
            && !OKCSite.REGION.HasFlag(Region.Australia)
            && OKCSite.REGION.HasFlag(Region.Britain)
            && !OKCSite.REGION.HasFlag(Region.Canada)
            && !OKCSite.REGION.HasFlag(Region.Europe)
            && !OKCSite.REGION.HasFlag(Region.Japan));
    }

    [Test]
    public async Task LightNovel_Returns_Empty()
    {
        // OK Comics doesn't stock prose light novels; ParsePages should short-circuit and
        // return an empty list rather than throwing or attempting any HTTP work.
        OKCSite site = new();
        Dictionary<string, HtmlDocument> noDocs = new(StringComparer.OrdinalIgnoreCase);

        List<EntryModel> actual = await site.ParsePages(
            listingPages: [],
            bookTitle: "Overlord",
            bookType: BookType.LightNovel,
            resolveDescDoc: OKCSite.CreateOfflineDescResolver(noDocs));

        Assert.That(actual, Is.Empty);
    }

    // ─── Fixture regeneration ────────────────────────────────────────────────
    //
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~OKComics"     (all)
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~jujutsu-kaisen-manga"  (one)
    //
    // Per case:
    //   1. Walk pagination via HtmlWeb, saving each listing page as page-NNN.html.
    //   2. Recording resolver fetches each product detail page, saves as desc-NNN.html,
    //      and writes the href→filename mapping to desc-index.txt.
    //   3. Re-runs ParsePages against the saved fixtures to write expected.txt.

    private static IEnumerable<TestCaseData> RegenerateCases()
    {
        foreach (object[] row in ScrapeTestCases)
        {
            string title = (string)row[0];
            BookType bookType = (BookType)row[1];
            string slug = (string)row[2];
            bool skip = (bool)row[3];
            yield return new TestCaseData(title, bookType, slug, skip).SetName(slug);
        }
    }

    [TestCaseSource(nameof(RegenerateCases))]
    [Explicit("Hits the live site. Run on-demand to refresh a single slug's fixtures.")]
    [Category("RegenerateFixtures")]
    public async Task Regenerate(string title, BookType bookType, string slug, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        Directory.CreateDirectory(FixturesRoot);
        OKCSite site = new();
        HtmlWeb web = HtmlFactory.CreateWeb();

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        Directory.CreateDirectory(fixtureDir);

        foreach (string old in Directory.EnumerateFiles(fixtureDir, "*.html"))
        {
            File.Delete(old);
        }
        string staleManifest = Path.Combine(fixtureDir, "desc-index.txt");
        if (File.Exists(staleManifest)) File.Delete(staleManifest);

        // Step 1: walk pagination, capture each listing.
        List<HtmlDocument> listingPages = [];
        const int MaxPages = 20;
        int curPage = 1;
        while (curPage <= MaxPages)
        {
            string url = site.GenerateWebsiteUrl(title, bookType, curPage);
            HtmlDocument doc = await web.LoadFromWebAsync(url);
            listingPages.Add(doc);
            File.WriteAllText(
                Path.Combine(fixtureDir, $"page-{listingPages.Count:D3}.html"),
                doc.DocumentNode.OuterHtml);

            HtmlNodeCollection cards = doc.DocumentNode.SelectNodes("//a[@class='product-card']");
            if (cards == null || cards.Count == 0) break;
            curPage++;
        }

        // Step 2: recording resolver — fetch each detail page on demand, save and record.
        Dictionary<string, HtmlDocument> recorded = new(StringComparer.OrdinalIgnoreCase);
        int descCounter = 0;
        List<string> manifest = [];
        Func<string, Task<HtmlDocument>> recordingResolver = async href =>
        {
            if (recorded.TryGetValue(href, out HtmlDocument cached)) return cached;
            string fullUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : href.StartsWith('/')
                    ? $"https://www.okcomics.co.uk{href}"
                    : $"https://www.okcomics.co.uk/{href}";
            HtmlDocument descDoc = await web.LoadFromWebAsync(fullUrl);
            descCounter++;
            string filename = $"desc-{descCounter:D3}.html";
            File.WriteAllText(
                Path.Combine(fixtureDir, filename),
                descDoc.DocumentNode.OuterHtml);
            manifest.Add($"{href}\t{filename}");
            recorded[href] = descDoc;
            return descDoc;
        };

        List<EntryModel> parsed = await site.ParsePages(listingPages, title, bookType, recordingResolver);

        File.WriteAllLines(Path.Combine(fixtureDir, "desc-index.txt"), manifest);
        File.WriteAllLines(
            Path.Combine(fixtureDir, "expected.txt"),
            parsed.Select(e => e.ToString()));

        TestContext.Progress.WriteLine(
            $"{slug}: {listingPages.Count} listing page(s), {recorded.Count} desc page(s), {parsed.Count} entries");
    }
}
