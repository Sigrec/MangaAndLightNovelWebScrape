namespace Tests.Websites.RobertsAnimeCornerStore;

[TestFixture, Description("Validations for RobertsAnimeCornerStore")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public class RobertsAnimeCornerStoreTests
{
    private MasterScrape Scrape;
    private static readonly HashSet<Website> WebsiteList = [Website.RobertsAnimeCornerStore];

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
        new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", false },
        new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", false },
        new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false },
        new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false },
        new object[] { "Bleach", BookType.Manga, "BleachMangaData", false },
        new object[] { "Attack on Titan", BookType.Manga, "AttackOnTitanMangaData", false },
        new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false },
        new object[] { "Overlord", BookType.Manga, "OverlordMangaData", false },
        new object[] { "Fullmetal Alchemist", BookType.Manga, "FMABMangaData", false },
        new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false },
        new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false },
        new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false },
        new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false },
        new object[] { "Persona 4", BookType.Manga, "Persona4MangaData", false },
        new object[] {"2.5 Dimensional Seduction", BookType.Manga, "DimensionalSeductionMangaData", false},
        new object[] { "Blade & Bastard", BookType.LightNovel, "Blade&BastardNovelData", false },
    ];

    [TestCaseSource(nameof(ScrapeTestCases))]
    public async Task RobertsAnimeCornerStore_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
    {
        if (skip)
        {
            Assert.Ignore($"Test skipped: '{title}'");
            return;
        }

        await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
        Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@$"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStore{expectedFilePath}.txt")));
    }
}