namespace Tests.Websites.Crunchyroll;

[TestFixture, Description("Validations for Crunchyroll")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class CrunchyrollTests
{
    private MasterScrape Scrape;
    private readonly HashSet<Website> WebsiteList = [Website.Crunchyroll];

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
        new object[] { "Akane-Banashi", BookType.Manga, "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "JujutsuKaisenMangaData", false },
        new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", false },
        new object[] { "attack on titan", BookType.Manga, "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "DimensionalSeductionMangaData", false},
        new object[] { "overlord", BookType.Manga, "OverlordMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "Blade&BastardNovelData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task Crunchyroll_Scrape_Test(string title, BookType bookType, string fileName, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: {title}");
            return;
        }

        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\Crunchyroll{fileName}.txt")));
    }
    
    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            MangaAndLightNovelWebScrape.Websites.Crunchyroll.REGION.HasFlag(Region.America) && !MangaAndLightNovelWebScrape.Websites.Crunchyroll.REGION.HasFlag(Region.Australia) && !MangaAndLightNovelWebScrape.Websites.Crunchyroll.REGION.HasFlag(Region.Britain) && !MangaAndLightNovelWebScrape.Websites.Crunchyroll.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.Crunchyroll.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.Crunchyroll.REGION.HasFlag(Region.Japan)
        );
    }
}