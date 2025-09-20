namespace Tests.Websites.KinokuniyaUSA;

[TestFixture, Description("Validations for KinokuniyaUSA")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class KinokuniyaUSATests
{
    private MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [Website.KinokuniyaUSA];

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
        new object[] { "Akane-Banashi", BookType.Manga, "AkaneBanashiMangaData", false, true },
        new object[] { "jujutsu kaisen", BookType.Manga, "JujutsuKaisenMangaData", false, false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", false, false },
        new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", false, false },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false, false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false, false },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", false, false },
        new object[] { "Attack on Titan", BookType.Manga, "AttackOnTitanMangaData", false, false },
        new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", false, false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false, false },
        new object[] { "Overlord", BookType.Manga, "OverlordMangaData", false, false },
        new object[] { "07-ghost", BookType.Manga, "07GhostMangaData", false, false },
        new object[] { "fullmetal alchemist", BookType.Manga, "FMABMangaData", false, false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false, false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false, false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false, false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false, false },
        new object[] { "Noragami", BookType.Manga, "NoragamiMangaData", false, false }
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task KinokuniyaUSA_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip, bool isMember)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        Scrape.IsKinokuniyaUSAMember = isMember;
        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSA{expectedFilePath}.txt")));
    }
    
    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA.REGION.HasFlag(Region.America) && !MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA.REGION.HasFlag(Region.Australia) && !MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA.REGION.HasFlag(Region.Britain) && !MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA.REGION.HasFlag(Region.Japan)
        );
    }
}