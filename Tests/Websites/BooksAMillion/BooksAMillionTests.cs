using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Playwright;
using BAMSite = MangaAndLightNovelWebScrape.Websites.BooksAMillion;

namespace Tests.Websites.BooksAMillion;

[TestFixture, Description("Fixture-based validations for BooksAMillion")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class BooksAMillionTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "BooksAMillion", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "BooksAMillion");

    // (title, bookType, slug, legacy, skip, isMember)
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false, false },
        new object[] { "jujutsu kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false, false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", false, false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceMangaData", false, false },
        new object[] { "Goodbye, Eri", BookType.Manga, "goodbye-eri-manga", "GoodbyeEriMangaData", false, true },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoMangaData", false, false },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false, false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachMangaData", false, false },
        new object[] { "Attack on Titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanMangaData", false, false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false, false },
        new object[] { "Overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false, false },
        new object[] { "Fullmetal Alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false, false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false, false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false, false },
        new object[] { "classroom of the elite", BookType.LightNovel, "cote-novel", "COTENovelData", false, false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", false, false },
        new object[] { "Noragami", BookType.Manga, "noragami-manga", "NoragamiMangaData", false, false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "blade-and-bastard-novel", "Blade&BastardNovelData", false, false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task BooksAMillion_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip, bool isMember)
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

        // Listing pages stored as `pass-1-page-NNN.html` (regular) and
        // `pass-2-page-NNN.html` (box-set), so we can recover the boxSet flags.
        string[] allPages = Directory.GetFiles(fixtureDir, "pass-*.html")
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (allPages.Length == 0)
        {
            Assert.Ignore($"No pass-*.html files in {fixtureDir}. Re-run Regenerate.");
            return;
        }

        List<HtmlDocument> listingPages = [];
        List<bool> boxSetFlags = [];
        foreach (string p in allPages)
        {
            HtmlDocument doc = new();
            doc.Load(p);
            listingPages.Add(doc);
            // pass-1 → regular (false), pass-2 → box-set (true)
            boxSetFlags.Add(Path.GetFileName(p).StartsWith("pass-2", StringComparison.Ordinal));
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

        BAMSite site = new();
        List<EntryModel> actual = await site.ParsePages(
            listingPages,
            boxSetFlags,
            title,
            bookType,
            isMember,
            BAMSite.CreateOfflineDescResolver(descByHref));

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"BooksAMillion{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            BAMSite.REGION.HasFlag(Region.America)
            && !BAMSite.REGION.HasFlag(Region.Australia)
            && !BAMSite.REGION.HasFlag(Region.Britain)
            && !BAMSite.REGION.HasFlag(Region.Canada)
            && !BAMSite.REGION.HasFlag(Region.Europe)
            && !BAMSite.REGION.HasFlag(Region.Japan));
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
            bool isMember = (bool)row[5];
            yield return new TestCaseData(title, bookType, slug, legacy, skip, isMember).SetName(slug);
        }
    }

    [TestCaseSource(nameof(RegenerateCases))]
    [Explicit("Hits the live site via Playwright. Run on-demand to refresh fixtures.")]
    [Category("RegenerateFixtures")]
    public async Task Regenerate(string title, BookType bookType, string slug, string legacy, bool skip, bool isMember)
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

        await using PlaywrightSession session = await PlaywrightFactory.SetupPlaywrightBrowserAsync();
        IPage page = await PlaywrightFactory.GetPageAsync(session.Browser, needsUserAgent: true);

        try
        {
            HtmlWeb descHtml = HtmlFactory.CreateWeb();
            descHtml.PreRequest += req =>
            {
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
                return true;
            };

            BAMSite site = new();
            List<HtmlDocument> listingPages = [];
            List<bool> boxSetFlags = [];

            // Two-pass: regular, then box-set.
            foreach (bool boxSetCheck in new[] { false, true })
            {
                int pageNum = 1;
                int pass = boxSetCheck ? 2 : 1;
                while (pageNum <= 30)
                {
                    string url = BAMSite.GenerateWebsiteUrl(title, boxSetCheck, bookType, pageNum);
                    await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

                    // Dismiss popup if present (only on first nav).
                    if (pageNum == 1 && !boxSetCheck)
                    {
                        var popup = await page.QuerySelectorAllAsync(".ltkpopup-container");
                        if (popup.Count > 0)
                        {
                            try { await page.ClickAsync(".ltkpopup-close"); } catch { }
                        }
                    }

                    try
                    {
                        await page.WaitForSelectorAsync(".search-item-title", new PageWaitForSelectorOptions { Timeout = 10000 });
                    }
                    catch (TimeoutException) { break; }

                    HtmlDocument doc = new();
                    doc.LoadHtml(await page.ContentAsync());
                    listingPages.Add(doc);
                    boxSetFlags.Add(boxSetCheck);
                    File.WriteAllText(
                        Path.Combine(fixtureDir, $"pass-{pass}-page-{pageNum:D3}.html"),
                        doc.DocumentNode.OuterHtml);

                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//ul[@class='search-page-list']//a[@title='Next']");
                    if (pageCheck == null) break;
                    pageNum++;
                }
            }

            // Recording resolver for detail-page desc fetches.
            Dictionary<string, HtmlDocument> recorded = new(StringComparer.OrdinalIgnoreCase);
            int descCounter = 0;
            List<string> manifest = [];
            Func<string, Task<HtmlDocument>> recordingResolver = async href =>
            {
                if (recorded.TryGetValue(href, out HtmlDocument cached)) return cached;
                string fullUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? href
                    : href.StartsWith('/') ? $"https://www.booksamillion.com{href}" : $"https://www.booksamillion.com/{href}";
                HtmlDocument descDoc = await descHtml.LoadFromWebAsync(fullUrl);
                descCounter++;
                string filename = $"desc-{descCounter:D3}.html";
                File.WriteAllText(
                    Path.Combine(fixtureDir, filename),
                    descDoc.DocumentNode.OuterHtml);
                manifest.Add($"{href}\t{filename}");
                recorded[href] = descDoc;
                return descDoc;
            };

            List<EntryModel> parsed = await site.ParsePages(listingPages, boxSetFlags, title, bookType, isMember, recordingResolver);

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
