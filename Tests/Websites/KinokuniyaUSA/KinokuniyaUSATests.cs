using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Playwright;
using KSite = MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA;

namespace Tests.Websites.KinokuniyaUSA;

[TestFixture, Description("Fixture-based validations for KinokuniyaUSA")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class KinokuniyaUSATests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "KinokuniyaUSA", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "KinokuniyaUSA");

    // (title, bookType, slug, legacy, skip, isMember)
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false, true },
        new object[] { "jujutsu kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false, false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", false, false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceMangaData", false, false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoMangaData", false, false },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false, false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachMangaData", false, false },
        new object[] { "Attack on Titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanMangaData", false, false },
        new object[] { "Goodbye, Eri", BookType.Manga, "goodbye-eri-manga", "GoodbyeEriMangaData", false, false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false, false },
        new object[] { "Overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false, false },
        new object[] { "07-ghost", BookType.Manga, "07-ghost-manga", "07GhostMangaData", false, false },
        new object[] { "fullmetal alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false, false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false, false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false, false },
        new object[] { "classroom of the elite", BookType.LightNovel, "cote-novel", "COTENovelData", false, false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", false, false },
        new object[] { "Noragami", BookType.Manga, "noragami-manga", "NoragamiMangaData", false, false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public void KinokuniyaUSA_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip, bool isMember)
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

        KSite site = new();
        List<EntryModel> actual = site.ParsePages(listingPages, title, bookType, isMember);

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"KinokuniyaUSA{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            KSite.REGION.HasFlag(Region.America)
            && !KSite.REGION.HasFlag(Region.Australia)
            && !KSite.REGION.HasFlag(Region.Britain)
            && !KSite.REGION.HasFlag(Region.Canada)
            && !KSite.REGION.HasFlag(Region.Europe)
            && !KSite.REGION.HasFlag(Region.Japan));
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

        await using PlaywrightSession session = await PlaywrightFactory.SetupPlaywrightBrowserAsync();
        IPage page = await PlaywrightFactory.GetPageAsync(session.Browser, needsUserAgent: true);

        try
        {
            KSite site = new();
            string url = site.GenerateWebsiteUrl(title, bookType);
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Per-page selector to 100 (best-effort, via JS).
            try
            {
                await page.EvaluateAsync(
                    @"(value) => {
                        const s = document.querySelector(""select[name='per_page']"");
                        if (!s) return false;
                        if (![...s.options].some(o => o.value == value)) return false;
                        s.value = value;
                        s.dispatchEvent(new Event('change', { bubbles: true }));
                        return true;
                    }",
                    "100");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch (PlaywrightException) { }

            // Switch to list mode (best-effort).
            try
            {
                await page.Locator("li#detail-button a:has-text(\"List\")").ClickAsync(new LocatorClickOptions { Timeout = 5000, Force = true });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch (TimeoutException) { }
            catch (PlaywrightException) { }

            // Manga facet (best-effort).
            if (bookType == BookType.Manga)
            {
                try
                {
                    await page.GetByText("Manga", new PageGetByTextOptions { Exact = true }).ClickAsync(new LocatorClickOptions { Timeout = 5000, Force = true });
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
                catch (TimeoutException) { }
                catch (PlaywrightException) { }
            }

            // Walk pagination by clicking pagerArrowR until it disappears or page count caps.
            int pageNum = 0;
            const int MaxPages = 30;
            while (pageNum < MaxPages)
            {
                HtmlDocument doc = new();
                doc.LoadHtml(await page.ContentAsync());
                pageNum++;
                File.WriteAllText(
                    Path.Combine(fixtureDir, $"page-{pageNum:D3}.html"),
                    doc.DocumentNode.OuterHtml);

                ILocator next = page.Locator("p.pagerArrowR");
                if (await next.CountAsync() == 0) break;
                try
                {
                    await next.ClickAsync(new LocatorClickOptions { Timeout = 5000, Force = true });
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
                catch (TimeoutException) { break; }
                catch (PlaywrightException) { break; }
            }

            // Re-parse saved fixtures to write expected.txt.
            List<HtmlDocument> savedDocs = [];
            foreach (string p in Directory.GetFiles(fixtureDir, "page-*.html").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                HtmlDocument d = new();
                d.Load(p);
                savedDocs.Add(d);
            }
            List<EntryModel> parsed = site.ParsePages(savedDocs, title, bookType, isMember);
            File.WriteAllLines(
                Path.Combine(fixtureDir, "expected.txt"),
                parsed.Select(e => e.ToString()));

            TestContext.Progress.WriteLine($"{slug}: {pageNum} page(s), {parsed.Count} entries");
        }
        finally
        {
            await page.DisposeContextAsync();
        }
    }
}
