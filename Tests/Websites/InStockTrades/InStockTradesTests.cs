namespace Tests.Websites.InStockTrades;

[TestFixture, Description("Validations for InStockTrades")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class InStockTradesTests
{
    private MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [ Website.InStockTrades ];

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
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", true },
        new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "AttackOnTitanMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false },
        new object[] { "Overlord", BookType.Manga, "OverlordMangaData", false },
        new object[] { "Fullmetal Alchemist", BookType.Manga, "FMABMangaData", false },
        new object[] { "Fullmetal Alchemist", BookType.LightNovel, "FMABNovelData", false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false },
        new object[] { "classroom of elite", BookType.LightNovel, "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false },
        new object[] { "Spice & Wolf", BookType.LightNovel, "Spice&WolfNovelData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task InStockTrades_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTrades{expectedFilePath}.txt")));
    }

    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            MangaAndLightNovelWebScrape.Websites.InStockTrades.REGION.HasFlag(Region.America) && !MangaAndLightNovelWebScrape.Websites.InStockTrades.REGION.HasFlag(Region.Australia) && !MangaAndLightNovelWebScrape.Websites.InStockTrades.REGION.HasFlag(Region.Britain) && !MangaAndLightNovelWebScrape.Websites.InStockTrades.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.InStockTrades.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.InStockTrades.REGION.HasFlag(Region.Japan)
        );
    }
}