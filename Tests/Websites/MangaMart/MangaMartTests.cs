namespace Tests.Websites;

[TestFixture, Description("Validations for MangaMart")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class MangaMartTests
{
    private MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [ Website.MangaMart ];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Scrape = null;
    }

    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "MangaMartAkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "MangaMartJujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "MangaMartAdventuresOfDaiMangaData", false },
        new object[] { "One Piece", BookType.Manga, "MangaMartOnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "MangaMartNarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "MangaMartNarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "MangaMartBleachMangaData", false },
        new object[] { "attack on titan", BookType.Manga, "MangaMartAttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "MangaMartGoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "MangaMartDimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "MangaMartOverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, "MangaMartOverlordMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "MangaMartFMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "MangaMartBerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "MangaMartToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "MangaMartCOTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "MangaMartBorutoMangaData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task MangaMart_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: {title}");
            return;
        }

        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\MangaMart\{expectedFilePath}.txt")));
    }
}