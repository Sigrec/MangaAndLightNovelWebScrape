namespace Tests.Websites.TravellingMan;

[TestFixture, Description("Validations for TravellingMan")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class TravellingManTests
{
    private MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [Website.TravellingMan];

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
        new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "DimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, "OverlordMangaData", false },
        new object[] { "07-ghost", BookType.Manga, "07GhostMangaData", false },
        new object[] { "Fullmetal Alchemist", BookType.Manga, "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "Blade&BastardData", false },
        new object[] { "classroom of the elite", BookType.Manga, "COTEMangaData", false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task TravellingMan_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: {title}");
            return;
        }

        // Initialize scraping process and compare results with expected data
        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingMan{expectedFilePath}.txt")));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            !MangaAndLightNovelWebScrape.Websites.TravellingMan.REGION.HasFlag(Region.America) && !MangaAndLightNovelWebScrape.Websites.TravellingMan.REGION.HasFlag(Region.Australia) && MangaAndLightNovelWebScrape.Websites.TravellingMan.REGION.HasFlag(Region.Britain) && !MangaAndLightNovelWebScrape.Websites.TravellingMan.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.TravellingMan.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.TravellingMan.REGION.HasFlag(Region.Japan)
        );
    }
}