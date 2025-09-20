namespace Tests.Websites.ForbiddenPlanet;

[TestFixture, Description("Validations for Forbidden Planet")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class ForbiddenPlanetTests
{
    private MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [Website.ForbiddenPlanet];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.Britain);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Scrape = null;
    }

    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "AkaneBanashiMangaData", false },
        new object[] { "Jujutsu Kaisen", BookType.Manga, "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", false },
        new object[] { "One Piece", BookType.Manga, "OnePieceManga", false },
        new object[] { "Naruto", BookType.Manga, "NarutoManga", false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "BleachManga", false },
        new object[] { "Attack On Titan", BookType.Manga, "AttackOnTitanManga", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "DimensionalSeductionMangaData", false },
        new object[] { "overlord", BookType.Manga, "OverlordMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false },
        new object[] { "07-ghost", BookType.Manga, "07GhostMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false },
        new object[] { "Noragami", BookType.Manga, "NoragamiMangaData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "Blade&BastardNovelData", false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task ForbiddenPlanet_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: {title}");
            return;
        }

        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanet{expectedFilePath}.txt")));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            !MangaAndLightNovelWebScrape.Websites.ForbiddenPlanet.REGION.HasFlag(Region.America) &&
            !MangaAndLightNovelWebScrape.Websites.ForbiddenPlanet.REGION.HasFlag(Region.Australia) &&
            MangaAndLightNovelWebScrape.Websites.ForbiddenPlanet.REGION.HasFlag(Region.Britain) &&
            !MangaAndLightNovelWebScrape.Websites.ForbiddenPlanet.REGION.HasFlag(Region.Canada) &&
            !MangaAndLightNovelWebScrape.Websites.ForbiddenPlanet.REGION.HasFlag(Region.Europe) &&
            !MangaAndLightNovelWebScrape.Websites.ForbiddenPlanet.REGION.HasFlag(Region.Japan)
        );
    }
}