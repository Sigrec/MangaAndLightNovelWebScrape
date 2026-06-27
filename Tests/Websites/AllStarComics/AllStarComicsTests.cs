using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using ASCSite = MangaAndLightNovelWebScrape.Websites.AllStarComics;

namespace Tests.Websites.AllStarComics;

[TestFixture, Description("Fixture-based validations for All Star Comics")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class AllStarComicsTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "AllStarComics", "Fixtures");

    // (title, bookType, slug, skip)
    // All Star Comics is manga-only; no LightNovel cases.
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Jujutsu Kaisen", BookType.Manga, "jujutsu-kaisen-manga", false },
        new object[] { "Naruto", BookType.Manga, "naruto-manga", false },
        new object[] { "One Piece", BookType.Manga, "one-piece-manga", false },
        new object[] { "Bleach", BookType.Manga, "bleach-manga", false },
        new object[] { "Attack on Titan", BookType.Manga, "attack-on-titan-manga", false },
        new object[] { "Berserk", BookType.Manga, "berserk-manga", false },
        new object[] { "Akane-Banashi", BookType.Manga, "akane-banashi-manga", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "fmab-manga", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "toilet-manga", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public void AllStarComics_Scrape_Test(string title, BookType bookType, string slug, bool skip)
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

        ASCSite site = new();
        List<EntryModel> actual = site.ParsePages(listingPages, title, bookType);

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
            !ASCSite.REGION.HasFlag(Region.America)
            && ASCSite.REGION.HasFlag(Region.Australia)
            && !ASCSite.REGION.HasFlag(Region.Britain)
            && !ASCSite.REGION.HasFlag(Region.Canada)
            && !ASCSite.REGION.HasFlag(Region.Europe)
            && !ASCSite.REGION.HasFlag(Region.Japan));
    }

    [Test]
    public void LightNovel_Returns_Empty()
    {
        // All Star Comics is manga-only; ParsePages should short-circuit and return an
        // empty list rather than throwing or scanning the listing docs.
        ASCSite site = new();
        List<EntryModel> actual = site.ParsePages([], "Overlord", BookType.LightNovel);
        Assert.That(actual, Is.Empty);
    }

    // ─── Pure-parsing unit tests ─────────────────────────────────────────────
    //
    // Standalone tests for the title cleanup so we don't have to run the whole pipeline
    // to verify Diamond-catalog edge cases (leading-zero strip, trailing-edition strip,
    // SHOUTING → Title Case). These don't need fixtures — they assert the parser output
    // for a synthetic single-card HTML document.

    [TestCase("JUJUTSU KAISEN GN VOL 01", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 1")]
    [TestCase("JUJUTSU KAISEN GN VOL 08 NEW PTG (MR)", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 8")]
    [TestCase("JUJUTSU KAISEN GN VOL 08 (MR)", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 8")]
    [TestCase("JUJUTSU KAISEN GN VOL 10", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 10")]
    [TestCase("JUJUTSU KAISEN GN VOL 22 (C: 0-1-2)", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 22")]
    [TestCase("NARUTO BOX SET 02", "Naruto", BookType.Manga, "Naruto Box Set 2")]
    [TestCase("NARUTO 3-IN-1 EDITION TP VOL 02", "Naruto", BookType.Manga, "Naruto Omnibus Vol 2")]
    public void CleanTitle_Manga_Variants(string raw, string bookTitle, BookType bookType, string expected)
    {
        // Drive ParsePages with a single synthetic listing card. Anything that survives
        // the type filter goes through the same cleanup that the live path runs.
        HtmlDocument doc = BuildSyntheticListing(raw, "$21.95", inStock: true);
        ASCSite site = new();
        List<EntryModel> parsed = site.ParsePages([doc], bookTitle, bookType);

        Assert.That(parsed, Has.Count.EqualTo(1), $"expected exactly one entry, got {parsed.Count}");
        Assert.That(parsed[0].Entry, Is.EqualTo(expected));
    }

    /// <summary>
    /// Builds the minimal All Star Comics product-card markup that <see cref="ASCSite.ParsePages"/>
    /// needs to recognize an entry. Anything not listed here is irrelevant to the parser.
    /// </summary>
    private static HtmlDocument BuildSyntheticListing(string rawTitle, string price, bool inStock)
    {
        string buttonClass = inStock
            ? "product-item__action-button button button--small button--primary"
            : "product-item__action-button button button--small button--disabled";
        string buttonText = inStock ? "Add to cart" : "Sold out";

        string html = $@"<html><body>
<div class='product-item product-item--vertical'>
  <a href='/products/test' class='product-item__title text--strong link'>{rawTitle}</a>
  <span class='price'>{price}</span>
  <button class='{buttonClass}'>{buttonText}</button>
</div>
</body></html>";

        HtmlDocument doc = new();
        doc.LoadHtml(html);
        return doc;
    }

    // ─── Fixture regeneration ────────────────────────────────────────────────
    //
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~AllStarComics"
    //
    // Per case: walks pagination via HtmlWeb, saves page-NNN.html, re-runs ParsePages,
    // writes expected.txt. No detail-page fetches — All Star ships price + stock on
    // the listing card itself.

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
        ASCSite site = new();
        HtmlWeb web = HtmlFactory.CreateWeb();

        string fixtureDir = Path.Combine(FixturesRoot, slug);
        Directory.CreateDirectory(fixtureDir);

        foreach (string old in Directory.EnumerateFiles(fixtureDir, "*.html"))
        {
            File.Delete(old);
        }

        // Step 1: walk pagination.
        List<HtmlDocument> listingPages = [];
        const int MaxPages = 30;
        int curPage = 1;
        while (curPage <= MaxPages)
        {
            string url = site.GenerateWebsiteUrl(title, curPage);
            HtmlDocument doc = await web.LoadFromWebAsync(url);
            listingPages.Add(doc);
            File.WriteAllText(
                Path.Combine(fixtureDir, $"page-{listingPages.Count:D3}.html"),
                doc.DocumentNode.OuterHtml);

            HtmlNodeCollection cards = doc.DocumentNode.SelectNodes("//div[contains(@class,'product-item--vertical')]");
            if (cards == null || cards.Count == 0) break;
            if (doc.DocumentNode.SelectSingleNode("//a[contains(@class,'pagination__next')]") == null) break;
            curPage++;
        }

        // Step 2: parse what we just downloaded and write the expected output.
        List<EntryModel> parsed = site.ParsePages(listingPages, title, bookType);
        File.WriteAllLines(
            Path.Combine(fixtureDir, "expected.txt"),
            parsed.Select(e => e.ToString()));

        TestContext.Progress.WriteLine($"{slug}: {listingPages.Count} listing page(s), {parsed.Count} entries");
    }
}
