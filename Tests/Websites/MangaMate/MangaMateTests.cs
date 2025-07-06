namespace Tests.Websites;

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
        new object[] { "Akane-Banashi", BookType.Manga, @"MangaMateAkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, @"MangaMateJujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"MangaMateAdventuresOfDaiMangaData", true },  // Skip test
        new object[] { "One Piece", BookType.Manga, @"MangaMateOnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, @"MangaMateNarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, @"MangaMateNarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, @"MangaMateBleachMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, @"MangaMateAttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, @"MangaMateGoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, @"MangaMateDimensionalSeductionMangaData", true },
        new object[] { "Overlord", BookType.LightNovel, @"MangaMateOverlordNovelData", true },
        new object[] { "overlord", BookType.Manga, @"MangaMateOverlordMangaData", true },
        new object[] { "07-ghost", BookType.Manga, @"MangaMate07GhostMangaData", true },
        new object[] { "Fullmetal Alchemist", BookType.Manga, @"MangaMateFMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, @"MangaMateBerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"MangaMateToiletMangaData", true },
        new object[] { "classroom of the elite", BookType.Manga, @"MangaMateCOTENovelData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, @"MangaMateCOTEMangaData", false },
        new object[] { "Boruto", BookType.Manga, @"MangaMateBorutoMangaData", false }
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
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@$"C:\MangaAndLightNovelWebScrape\Tests\Websites\MangaMate\{expectedFilePath}.txt")));
    }
}