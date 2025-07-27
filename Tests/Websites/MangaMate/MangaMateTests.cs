namespace Tests.Websites.MangaMate;

[TestFixture, Description("Validations for MangaMate")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class MangaMateTests
{
    private MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = new HashSet<Website> { Website.MangaMate };

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.Australia);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Scrape = null;
    }

    private static readonly object[] ScrapeTestCases =
    [
        // Test case data structured as {title, book type, expected file path, skip flag}
        new object[] { "Akane-Banashi", BookType.Manga, "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", true },  // Skip test
        new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "DimensionalSeductionMangaData", true },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", true },
        new object[] { "overlord", BookType.Manga, "OverlordMangaData", true },
        new object[] { "07-ghost", BookType.Manga, "07GhostMangaData", true },
        new object[] { "Fullmetal Alchemist", BookType.Manga, "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", true },
        new object[] { "classroom of the elite", BookType.Manga, "COTENovelData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTEMangaData", false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task MangaMate_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: {title}");
            return;
        }

        // Scrape data and compare results with the expected data from the file
        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@$"C:\MangaAndLightNovelWebScrape\Tests\Websites\MangaMate\MangaMate{expectedFilePath}.txt")));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            !MangaAndLightNovelWebScrape.Websites.MangaMate.REGION.HasFlag(Region.America) && MangaAndLightNovelWebScrape.Websites.MangaMate.REGION.HasFlag(Region.Australia) && !MangaAndLightNovelWebScrape.Websites.MangaMate.REGION.HasFlag(Region.Britain) && !MangaAndLightNovelWebScrape.Websites.MangaMate.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.MangaMate.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.MangaMate.REGION.HasFlag(Region.Japan)
        );
    }
}