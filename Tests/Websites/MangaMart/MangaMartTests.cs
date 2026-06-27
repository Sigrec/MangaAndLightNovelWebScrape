using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Playwright;
using MMSite = MangaAndLightNovelWebScrape.Websites.MangaMart;

namespace Tests.Websites.MangaMart;

[TestFixture, Description("Fixture-based validations for MangaMart")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class MangaMartTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "MangaMart", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "MangaMart");

    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceMangaData", true },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachMangaData", false },
        new object[] { "attack on titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "goodbye-eri-manga", "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "dimensional-seduction-manga", "DimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "cote-novel", "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "blade-and-bastard-novel", "Blade&BastardNovelData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task MangaMart_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip)
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

        MMSite site = new();
        List<EntryModel> actual = await site.ParsePages(
            listingPages,
            title,
            bookType,
            MMSite.CreateOfflineDescResolver(descByHref));

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"MangaMart{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            MMSite.REGION.HasFlag(Region.America)
            && !MMSite.REGION.HasFlag(Region.Australia)
            && !MMSite.REGION.HasFlag(Region.Britain)
            && !MMSite.REGION.HasFlag(Region.Canada)
            && !MMSite.REGION.HasFlag(Region.Europe)
            && !MMSite.REGION.HasFlag(Region.Japan));
    }

    // ─── Fixture regeneration (Playwright) ───────────────────────────────────

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
    [Explicit("Hits the live site via Playwright. Run on-demand to refresh fixtures.")]
    [Category("RegenerateFixtures")]
    public async Task Regenerate(string title, BookType bookType, string slug, string legacy, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        Directory.CreateDirectory(FixturesRoot);
        string fixtureDir = Path.Combine(FixturesRoot, slug);
        Directory.CreateDirectory(fixtureDir);

        foreach (string old in Directory.EnumerateFiles(fixtureDir, "*.html"))
        {
            File.Delete(old);
        }
        string staleManifest = Path.Combine(fixtureDir, "desc-index.txt");
        if (File.Exists(staleManifest)) File.Delete(staleManifest);

        // Spin up Playwright. MangaMart is JS-rendered; HtmlWeb alone gets an empty shell.
        await using PlaywrightSession session = await PlaywrightFactory.SetupPlaywrightBrowserAsync();
        IPage page = await PlaywrightFactory.GetPageAsync(session.Browser);

        try
        {
            MMSite site = new();
            HtmlWeb html = HtmlFactory.CreateWeb();
            string encodedTitle = Uri.EscapeDataString(title);

            // Step 1: walk pagination via Playwright, saving each settled DOM.
            List<HtmlDocument> listingPages = [];
            uint curPage = 1;
            string url = site.GenerateWebsiteUrl(bookType, encodedTitle, curPage);
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            HtmlDocument doc = new();
            doc.LoadHtml(await page.ContentAsync());
            listingPages.Add(doc);
            File.WriteAllText(
                Path.Combine(fixtureDir, $"page-{listingPages.Count:D3}.html"),
                doc.DocumentNode.OuterHtml);

            HtmlNode maxNode = doc.DocumentNode.SelectSingleNode("(//a[@class='pagination__nav-item link'])[last()]");
            int maxPage = (maxNode != null && int.TryParse(maxNode.InnerText, out int mp)) ? mp : 1;

            const int MaxPages = 30;
            while (curPage < maxPage && listingPages.Count < MaxPages)
            {
                curPage++;
                url = site.GenerateWebsiteUrl(bookType, encodedTitle, curPage);
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                HtmlDocument next = new();
                next.LoadHtml(await page.ContentAsync());
                listingPages.Add(next);
                File.WriteAllText(
                    Path.Combine(fixtureDir, $"page-{listingPages.Count:D3}.html"),
                    next.DocumentNode.OuterHtml);
            }

            // Step 2: detail-page fetches go through HtmlWeb — they're not JS-gated for
            // MangaMart, so a plain HTTP request works. The recording resolver saves
            // each one and records the manifest.
            Dictionary<string, HtmlDocument> recorded = new(StringComparer.OrdinalIgnoreCase);
            int descCounter = 0;
            List<string> manifest = [];
            Func<string, Task<HtmlDocument>> recordingResolver = async href =>
            {
                if (recorded.TryGetValue(href, out HtmlDocument cached)) return cached;
                string fullUrl = href.StartsWith('/')
                    ? $"https://mangamart.com{href}"
                    : $"https://mangamart.com/{href}";
                HtmlDocument descDoc = await html.LoadFromWebAsync(fullUrl);
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
        finally
        {
            await page.DisposeContextAsync();
        }
    }
}
