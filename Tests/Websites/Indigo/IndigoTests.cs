namespace Tests.Websites.Indigo;

[TestFixture, Description("Validations for Indigo")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class IndigoTests
{
    private MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [Website.Indigo];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.Canada);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Scrape.IsIndigoMember = false;
    }

    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "AkaneBanashiMangaData", false, false },
        new object[] { "jujutsu kaisen", BookType.Manga, "JujutsuKaisenMangaData", false, false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", false, false },
        new object[] { "one piece", BookType.Manga, "OnePieceMangaData", false, false },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false, false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false, false },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", false, false },
        new object[] { "Attack on Titan", BookType.Manga, "AttackOnTitanMangaData", false, false },
        new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", true, false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "DimensionalSeductionMangaData", false, false },
        new object[] { "Overlord", BookType.Manga, "OverlordMangaData", false, false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false, false },
        new object[] { "07-ghost", BookType.Manga, "07GhostMangaData", false, false },
        new object[] { "fullmetal alchemist", BookType.Manga, "FMABMangaData", false, false },
        new object[] { "fullmetal alchemist", BookType.LightNovel, "FMABNovelData", false, false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false, false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false, false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false, false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false, false },
        new object[] { "Noragami", BookType.Manga, "NoragamiMangaData", false, false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task Indigo_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool isMember, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: {title}");
            return;
        }

        Scrape.IsIndigoMember = isMember;

        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\Indigo{expectedFilePath}.txt")));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            !MangaAndLightNovelWebScrape.Websites.Indigo.REGION.HasFlag(Region.America) && !MangaAndLightNovelWebScrape.Websites.Indigo.REGION.HasFlag(Region.Australia) && !MangaAndLightNovelWebScrape.Websites.Indigo.REGION.HasFlag(Region.Britain) && MangaAndLightNovelWebScrape.Websites.Indigo.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.Indigo.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.Indigo.REGION.HasFlag(Region.Japan)
        );
    }
}