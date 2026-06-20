using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using TMSite = MangaAndLightNovelWebScrape.Websites.TravellingMan;

namespace Tests.Websites.TravellingMan;

[TestFixture, Description("Fixture-based validations for TravellingMan")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class TravellingManTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "TravellingMan", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "TravellingMan");

    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false },
        new object[] { "Jujutsu Kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "goodbye-eri-manga", "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "dimensional-seduction-manga", "DimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false },
        new object[] { "07-ghost", BookType.Manga, "07-ghost-manga", "07GhostMangaData", false },
        new object[] { "Fullmetal Alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "cote-novel", "COTENovelData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "blade-and-bastard-novel", "Blade&BastardData", false },
        new object[] { "classroom of the elite", BookType.Manga, "cote-manga", "COTEMangaData", false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task TravellingMan_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip)
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

        TMSite site = new();
        List<EntryModel> actual = await site.ParsePages(
            listingPages,
            title,
            bookType,
            TMSite.CreateOfflineDescResolver(descByHref));

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"TravellingMan{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            !TMSite.REGION.HasFlag(Region.America)
            && !TMSite.REGION.HasFlag(Region.Australia)
            && TMSite.REGION.HasFlag(Region.Britain)
            && !TMSite.REGION.HasFlag(Region.Canada)
            && !TMSite.REGION.HasFlag(Region.Europe)
            && !TMSite.REGION.HasFlag(Region.Japan));
    }

    // ─── Fixture regeneration ────────────────────────────────────────────────

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
        TMSite site = new();
        HtmlWeb web = HtmlFactory.CreateWeb();

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        Directory.CreateDirectory(fixtureDir);

        foreach (string old in Directory.EnumerateFiles(fixtureDir, "*.html"))
        {
            File.Delete(old);
        }
        string staleManifest = Path.Combine(fixtureDir, "desc-index.txt");
        if (File.Exists(staleManifest)) File.Delete(staleManifest);

        // Step 1: walk pagination, capture each listing page.
        List<HtmlDocument> listingPages = [];
        int nextPage = 1;
        const int MaxPages = 30;
        while (listingPages.Count < MaxPages)
        {
            string url = site.GenerateWebsiteUrl(title, bookType, nextPage);
            HtmlDocument doc = await web.LoadFromWebAsync(url);
            listingPages.Add(doc);
            File.WriteAllText(
                Path.Combine(fixtureDir, $"page-{listingPages.Count:D3}.html"),
                doc.DocumentNode.OuterHtml);

            HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//li[@class='list-view-item']/div/div/div[2]/div/span");
            HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//li[@class='list-view-item']/div/div/div[3]/dl/div[2]/dd[2]/span[1]");
            HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//ul[@class='list--inline pagination']/li[3]/a");
            if (titleData == null || priceData == null) break;
            if (!(priceData.Count == titleData.Count && pageCheck != null)) break;
            nextPage++;
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
                    ? $"https://travellingman.com{href}"
                    : $"https://travellingman.com/{href}";
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
