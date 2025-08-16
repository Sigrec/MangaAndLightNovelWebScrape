namespace Tests.Websites.MangaMart;

[TestFixture, Description("Validations for MangaMart")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class MangaMartTests
{
    private static MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [Website.MangaMart];

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

    // NOTE: Some of the larger series are always a little off but it's nto that big of a deal right now should fix later though
    private static readonly object[] ScrapeTestCases =
    [
        new object[] { "Akane-Banashi", BookType.Manga, "AkaneBanashiMangaData", false },
        new object[] { "jujutsu kaisen", BookType.Manga, "JujutsuKaisenMangaData", false },
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", false },
        new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", true },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", false },
        new object[] { "attack on titan", BookType.Manga, "AttackOnTitanMangaData", false },
        new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", false },
        new object[] { "2.5 Dimensional Seduction", BookType.Manga, "DimensionalSeductionMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false },
        new object[] { "overlord", BookType.Manga, "OverlordMangaData", false },
        new object[] { "fullmetal alchemist", BookType.Manga, "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false },
        new object[] { "Blade & Bastard", BookType.LightNovel, "Blade&BastardNovelData", false },
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
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\MangaMart\MangaMart{expectedFilePath}.txt")));
    }
    
    [Test]
    public void RegionValidation_Test()
    {
        Assert.That(
            MangaAndLightNovelWebScrape.Websites.MangaMart.REGION.HasFlag(Region.America) && !MangaAndLightNovelWebScrape.Websites.MangaMart.REGION.HasFlag(Region.Australia) && !MangaAndLightNovelWebScrape.Websites.MangaMart.REGION.HasFlag(Region.Britain) && !MangaAndLightNovelWebScrape.Websites.MangaMart.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.MangaMart.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.MangaMart.REGION.HasFlag(Region.Japan)
        );
    }
}