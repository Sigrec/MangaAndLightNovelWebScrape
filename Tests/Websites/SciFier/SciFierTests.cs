namespace Tests.Websites.SciFier;

[TestFixture, Description("Validations for SciFier")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class SciFierTests
{
    private static MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = new HashSet<Website> { Website.SciFier };

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
        new object[] { "Akane-Banashi", BookType.Manga, Region.Australia, "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, Region.Europe, "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, Region.Canada, "AdventuresOfDaiMangaData", false },
        new object[] { "one piece", BookType.Manga, Region.America, "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, Region.America, "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, Region.America, "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, Region.America, "BleachMangaData", false },
        new object[] { "attack on titan", BookType.Manga, Region.America, "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, Region.Britain, "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, Region.America, "DimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, Region.America, "OverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, Region.America, "OverlordMangaData", false },
        new object[] { "07-ghost", BookType.Manga, Region.America, "07GhostMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, Region.America, "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, Region.America, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, Region.America, "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, Region.America, "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, Region.America, "BorutoMangaData", false },
        new object[] { "Noragami", BookType.Manga, Region.America, "NoragamiMangaData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task SciFier_Scrape_Test(string title, BookType bookType, Region region, string expectedFilePath, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        Scrape.Region = region;
        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFier{expectedFilePath}.txt")));
    }
    
    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            MangaAndLightNovelWebScrape.Websites.SciFier.REGION.HasFlag(Region.America) && MangaAndLightNovelWebScrape.Websites.SciFier.REGION.HasFlag(Region.Australia) && MangaAndLightNovelWebScrape.Websites.SciFier.REGION.HasFlag(Region.Britain) && MangaAndLightNovelWebScrape.Websites.SciFier.REGION.HasFlag(Region.Canada) && MangaAndLightNovelWebScrape.Websites.SciFier.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.SciFier.REGION.HasFlag(Region.Japan)
        );
    }
}