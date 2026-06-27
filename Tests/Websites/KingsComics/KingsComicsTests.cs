using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape.Services;
using MangaAndLightNovelWebScrape.Websites;
using KCSite = MangaAndLightNovelWebScrape.Websites.KingsComics;

namespace Tests.Websites.KingsComics;

[TestFixture, Description("Fixture-based validations for Kings Comics")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class KingsComicsTests
{
    private static readonly string FixturesRoot = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "Websites", "KingsComics", "Fixtures");

    // (title, bookType, slug, skip)
    // Kings Comics is manga-only; no LightNovel cases.
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
    public void KingsComics_Scrape_Test(string title, BookType bookType, string slug, bool skip)
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

        KCSite site = new();
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
            !KCSite.REGION.HasFlag(Region.America)
            && KCSite.REGION.HasFlag(Region.Australia)
            && !KCSite.REGION.HasFlag(Region.Britain)
            && !KCSite.REGION.HasFlag(Region.Canada)
            && !KCSite.REGION.HasFlag(Region.Europe)
            && !KCSite.REGION.HasFlag(Region.Japan));
    }

    [Test]
    public void LightNovel_Returns_Empty()
    {
        // Kings Comics is a comic / manga shop — no prose light novels. ParsePages
        // short-circuits and returns an empty list rather than throwing or scanning.
        KCSite site = new();
        List<EntryModel> actual = site.ParsePages([], "Overlord", BookType.LightNovel);
        Assert.That(actual, Is.Empty);
    }

    // ─── Pure-parsing unit tests ─────────────────────────────────────────────
    //
    // Synthetic single-card HTML drives the cleanup pipeline so we can assert
    // SHOUTING → Title Case, "3-IN-1 EDITION" → "Omnibus", leading-zero strip,
    // multi-digit vol preservation, and edition-marker strip — all without
    // fixtures.

    [TestCase("JUJUTSU KAISEN GN VOL 01", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 1")]
    [TestCase("JUJUTSU KAISEN GN VOL 08 NEW PTG (MR)", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 8")]
    [TestCase("JUJUTSU KAISEN GN VOL 10", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 10")]
    [TestCase("JUJUTSU KAISEN GN VOL 22 (C: 0-1-2)", "Jujutsu Kaisen", BookType.Manga, "Jujutsu Kaisen Vol 22")]
    [TestCase("NARUTO BOX SET 02", "Naruto", BookType.Manga, "Naruto Box Set 2")]
    [TestCase("NARUTO 3-IN-1 TP VOL 02", "Naruto", BookType.Manga, "Naruto Omnibus Vol 2")]
    public void CleanTitle_Manga_Variants(string raw, string bookTitle, BookType bookType, string expected)
    {
        HtmlDocument doc = BuildSyntheticListing(raw, "$21.95", availabilityClass: "product-card__availability--in");
        KCSite site = new();
        List<EntryModel> parsed = site.ParsePages([doc], bookTitle, bookType);

        Assert.That(parsed, Has.Count.EqualTo(1), $"expected exactly one entry, got {parsed.Count}");
        Assert.That(parsed[0].Entry, Is.EqualTo(expected));
    }

    [TestCase("product-card__availability--in", StockStatus.IS)]
    [TestCase("product-card__availability--last", StockStatus.IS)]
    [TestCase("product-card__availability--out", StockStatus.OOS)]
    [TestCase("product-card__availability--preorder", StockStatus.PO)]
    public void StockStatus_FromAvailabilityClass(string availabilityClass, StockStatus expected)
    {
        HtmlDocument doc = BuildSyntheticListing("JUJUTSU KAISEN GN VOL 01", "$21.95", availabilityClass);
        KCSite site = new();
        List<EntryModel> parsed = site.ParsePages([doc], "Jujutsu Kaisen", BookType.Manga);

        Assert.That(parsed, Has.Count.EqualTo(1));
        Assert.That(parsed[0].StockStatus, Is.EqualTo(expected));
    }

    /// <summary>
    /// Builds the minimal Kings Comics product-card markup that
    /// <see cref="KCSite.ParsePages"/> needs to recognize an entry. Anything not
    /// listed here is irrelevant to the parser. The availability class follows
    /// Shopify's BEM-style modifier convention used by the live theme.
    /// </summary>
    private static HtmlDocument BuildSyntheticListing(string rawTitle, string price, string availabilityClass)
    {
        string html = $@"<html><body>
<div class='product-card'>
  <a href='/products/test' class='product-card__link'>
    <p class='product-card__title'>{rawTitle}</p>
    <p class='product-card__price'>{price}</p>
    <p class='product-card__availability {availabilityClass}'>label</p>
  </a>
</div>
</body></html>";

        HtmlDocument doc = new();
        doc.LoadHtml(html);
        return doc;
    }

    // ─── Fixture regeneration ────────────────────────────────────────────────
    //
    // dotnet test --filter "TestCategory=RegenerateFixtures&FullyQualifiedName~KingsComics"
    //
    // Walks pagination via HtmlWeb, saves page-NNN.html, re-runs ParsePages,
    // writes expected.txt. No detail-page fetches — Kings Comics ships price +
    // stock on the listing card itself.

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
        KCSite site = new();
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

            HtmlNodeCollection cards = doc.DocumentNode.SelectNodes("//div[@class='product-card']");
            if (cards == null || cards.Count == 0) break;

            listingPages.Add(doc);
            File.WriteAllText(
                Path.Combine(fixtureDir, $"page-{listingPages.Count:D3}.html"),
                doc.DocumentNode.OuterHtml);
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
