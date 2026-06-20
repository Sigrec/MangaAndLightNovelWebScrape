using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Playwright;
using FPSite = MangaAndLightNovelWebScrape.Websites.ForbiddenPlanet;

namespace Tests.Websites.ForbiddenPlanet;

[TestFixture, Description("Fixture-based validations for Forbidden Planet")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class ForbiddenPlanetTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "ForbiddenPlanet", "Fixtures");

    private static readonly string LegacyExpectedRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "ForbiddenPlanet");

    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", "AkaneBanashiMangaData", false },
        new object[] { "Jujutsu Kaisen", BookType.Manga, "jujutsu-kaisen-manga", "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "adventures-of-dai-manga", "AdventuresOfDaiMangaData", false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", "OnePieceManga", false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", "NarutoManga", false },
        new object[] { "Naruto", BookType.LightNovel, "naruto-novel", "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", "BleachManga", false },
        new object[] { "Attack On Titan", BookType.Manga, "attack-on-titan-manga", "AttackOnTitanManga", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "goodbye-eri-manga", "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "dimensional-seduction-manga", "DimensionalSeductionMangaData", false },
        new object[] { "overlord", BookType.Manga, "overlord-manga", "OverlordMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "overlord-novel", "OverlordNovelData", false },
        new object[] { "07-ghost", BookType.Manga, "07-ghost-manga", "07GhostMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "fmab-manga", "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "cote-novel", "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "boruto-manga", "BorutoMangaData", false },
        new object[] { "Noragami", BookType.Manga, "noragami-manga", "NoragamiMangaData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "blade-and-bastard-novel", "Blade&BastardNovelData", false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task ForbiddenPlanet_Scrape_Test(string title, BookType bookType, string slug, string legacy, bool skip)
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

        // category-*.html are listing pages (one per Manga vs Comics sweep).
        // desc-*.html + desc-index.txt are the detail pages for entries that needed
        // a Hardcover / novel-in-disguise check.
        string[] categoryFiles = Directory.GetFiles(fixtureDir, "category-*.html")
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (categoryFiles.Length == 0)
        {
            Assert.Ignore($"No category-*.html files in {fixtureDir}. Re-run Regenerate.");
            return;
        }

        List<HtmlDocument> listingPages = [];
        foreach (string p in categoryFiles)
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

        FPSite site = new();
        List<EntryModel> actual = await site.ParsePages(
            listingPages,
            title,
            bookType,
            FPSite.CreateOfflineDescResolver(descByHref));

        string expectedPath = Path.Combine(fixtureDir, "expected.txt");
        if (!File.Exists(expectedPath))
        {
            expectedPath = Path.Combine(LegacyExpectedRoot, $"ForbiddenPlanet{legacy}.txt");
        }
        List<EntryModel> expected = ImportDataToList(expectedPath);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            !FPSite.REGION.HasFlag(Region.America)
            && !FPSite.REGION.HasFlag(Region.Australia)
            && FPSite.REGION.HasFlag(Region.Britain)
            && !FPSite.REGION.HasFlag(Region.Canada)
            && !FPSite.REGION.HasFlag(Region.Europe)
            && !FPSite.REGION.HasFlag(Region.Japan));
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

        await using PlaywrightSession session = await PlaywrightFactory.SetupPlaywrightBrowserAsync();
        IPage page = await PlaywrightFactory.GetPageAsync(session.Browser, needsUserAgent: true);

        try
        {
            FPSite site = new();
            HtmlWeb html = HtmlFactory.CreateWeb();

            // Step 1: capture each of the two category listings (after Load More exhaust).
            List<HtmlDocument> listingPages = [];
            int categoryIdx = 0;
            foreach (bool isSecondCategory in new[] { false, true })
            {
                categoryIdx++;
                string url = site.GenerateWebsiteUrl(bookType, title, isSecondCategory, 1);
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                try
                {
                    await page.WaitForSelectorAsync("div.full", new PageWaitForSelectorOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = 15000
                    });
                }
                catch (TimeoutException) { continue; }

                // Exhaust Load More.
                ILocator productItems = page.Locator("//div[@class='full']/ul/li");
                ILocator loadMore = page.Locator("button.load-more.button--brand.brad--sm");
                const int MaxClicks = 50;
                for (int i = 0; i < MaxClicks; i++)
                {
                    if (!await loadMore.IsVisibleAsync()) break;
                    if (!await loadMore.IsEnabledAsync()) break;
                    int before = await productItems.CountAsync();
                    await loadMore.ClickAsync();
                    try
                    {
                        await page.WaitForFunctionAsync(
                            $"() => document.querySelectorAll('div.full > ul > li').length > {before}",
                            new PageWaitForFunctionOptions { Timeout = 15000 });
                    }
                    catch (TimeoutException) { break; }
                }

                HtmlDocument doc = new();
                doc.LoadHtml(await page.ContentAsync());
                listingPages.Add(doc);
                File.WriteAllText(
                    Path.Combine(fixtureDir, $"category-{categoryIdx:D2}.html"),
                    doc.DocumentNode.OuterHtml);
            }

            // Step 2: recording resolver for the per-entry detail pages. Detail pages are
            // not JS-gated on ForbiddenPlanet — plain HTTP works.
            Dictionary<string, HtmlDocument> recorded = new(StringComparer.OrdinalIgnoreCase);
            int descCounter = 0;
            List<string> manifest = [];
            Func<string, Task<HtmlDocument>> recordingResolver = async href =>
            {
                if (recorded.TryGetValue(href, out HtmlDocument cached)) return cached;
                HtmlDocument descDoc = await html.LoadFromWebAsync($"https://forbiddenplanet.com{href}");
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
                $"{slug}: {listingPages.Count} category page(s), {recorded.Count} desc page(s), {parsed.Count} entries");
        }
        finally
        {
            await page.DisposeContextAsync();
        }
    }
}
