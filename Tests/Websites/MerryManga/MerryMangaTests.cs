namespace Tests.Websites.MerryManga;

[TestFixture, Description("Validations for MerryManga")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class MerryMangaTests
{
    private static MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [Website.MerryManga];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Scrape = null;
    }

    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "JujutsuKaisenMangaData", false },
        new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", true },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", true },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "DimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, "OverlordMangaData", false },
        new object[] { "07-ghost", BookType.Manga, "07GhostMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", true },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", true },
        new object[] { "Noragami", BookType.Manga, "NoragamiMangaData", false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task MerryManga_Scrape_Test(string title, BookType bookType, string fileName, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\MerryManga\MerryManga{fileName}.txt")));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            MangaAndLightNovelWebScrape.Websites.MerryManga.REGION.HasFlag(Region.America) && !MangaAndLightNovelWebScrape.Websites.MerryManga.REGION.HasFlag(Region.Australia) && !MangaAndLightNovelWebScrape.Websites.MerryManga.REGION.HasFlag(Region.Britain) && !MangaAndLightNovelWebScrape.Websites.MerryManga.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.MerryManga.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.MerryManga.REGION.HasFlag(Region.Japan)
        );
    }
}
