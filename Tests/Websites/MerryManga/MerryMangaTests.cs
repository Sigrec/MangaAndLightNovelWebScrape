using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Playwright;
using MMSite = MangaAndLightNovelWebScrape.Websites.MerryManga;

namespace Tests.Websites.MerryManga;

[TestFixture, Description("Fixture-based validations for MerryManga")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class MerryMangaTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "MerryManga", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "MerryManga");

    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceMangaData", true },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachMangaData", true },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "goodbye-eri-manga", "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "dimensional-seduction-manga", "DimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false },
        new object[] { "07-ghost", BookType.Manga, "07-ghost-manga", "07GhostMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoMangaData", true },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "cote-novel", "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", true },
        new object[] { "Noragami", BookType.Manga, "noragami-manga", "NoragamiMangaData", false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public void MerryManga_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip)
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

        MMSite site = new();
        List<EntryModel> actual = site.ParsePages(listingPages, title, bookType);

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"MerryManga{legacy}.txt");
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

        // MerryManga is a WooCommerce site with a Load More facet; needs Playwright to
        // click through and settle the DOM before snapshotting.
        await using PlaywrightSession session = await PlaywrightFactory.SetupPlaywrightBrowserAsync();
        IPage page = await PlaywrightFactory.GetPageAsync(session.Browser);

        try
        {
            MMSite site = new();
            string titleLower = title.ToLower();
            int pageCount = 0;

            // Two passes: box-set then default category (or just default if box-set is empty).
            foreach (bool hasBoxSet in new[] { true, false })
            {
                if (bookType == BookType.LightNovel && hasBoxSet) continue;

                string url = site.GenerateWebsiteUrl(titleLower, bookType, hasBoxSet);
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await page.WaitForSelectorAsync("div.container.main-content");

                HtmlDocument doc = new();
                doc.LoadHtml(await page.ContentAsync());
                if (hasBoxSet && doc.Text.Contains("No products were found matching your selection."))
                {
                    continue;
                }

                // Dismiss "18+" popup if it appears.
                ILocator heading = page.Locator("h2.popup_heading");
                if (await heading.CountAsync() > 0)
                {
                    string text = (await heading.First.InnerTextAsync()).Trim();
                    if (text.Equals("This product is rated 18+", StringComparison.OrdinalIgnoreCase))
                    {
                        await page.Locator("button.btn_submit#submit").ClickAsync();
                    }
                }

                // Exhaust Load More.
                ILocator visibleBtn = page.Locator("button.facetwp-load-more:not(.facetwp-hidden)");
                if (await visibleBtn.CountAsync() > 0)
                {
                    ILocator hiddenBtn = page.Locator("button.facetwp-load-more.facetwp-hidden");
                    ILocator pager = page.Locator("div.facetwp-facet.facetwp-facet-load_more.facetwp-type-pager");
                    while (true)
                    {
                        if (await visibleBtn.CountAsync() == 0) break;
                        await visibleBtn.First.ClickAsync();
                        await pager.First.WaitForAsync(new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Attached,
                            Timeout = 5000
                        });
                        Task tHidden = hiddenBtn.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 4000 });
                        Task tGone = visibleBtn.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 4000 });
                        await Task.WhenAny(tHidden, tGone);
                        await page.WaitForTimeoutAsync(50);
                        if (await hiddenBtn.CountAsync() > 0 || await visibleBtn.CountAsync() == 0) break;
                    }
                }

                doc = new HtmlDocument();
                doc.LoadHtml(await page.ContentAsync());
                pageCount++;
                File.WriteAllText(
                    Path.Combine(fixtureDir, $"page-{pageCount:D3}.html"),
                    doc.DocumentNode.OuterHtml);
            }

            // Re-parse saved fixtures to write expected.txt.
            List<HtmlDocument> savedDocs = [];
            foreach (string p in Directory.GetFiles(fixtureDir, "page-*.html").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                HtmlDocument d = new();
                d.Load(p);
                savedDocs.Add(d);
            }
            List<EntryModel> parsed = site.ParsePages(savedDocs, title, bookType);
            File.WriteAllLines(
                Path.Combine(fixtureDir, "expected.txt"),
                parsed.Select(e => e.ToString()));

            TestContext.Progress.WriteLine($"{slug}: {pageCount} page(s), {parsed.Count} entries");
        }
        finally
        {
            await page.DisposeContextAsync();
        }
    }
}
