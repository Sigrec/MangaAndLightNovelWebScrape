namespace Tests.Websites
{
    [TestFixture, Description("Validations for Crunchyroll")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class CrunchyrollTests
    {
        MasterScrape Scrape;
        private readonly HashSet<Website> WebsiteList = [ Website.Crunchyroll ];

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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollAkaneBanashiMangaData.txt", false },
            new object[] { "jujutsu kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollJujutsuKaisenMangaData.txt", false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollAdventuresOfDaiMangaData.txt", true },  // Skip this test
            new object[] { "Dragon Quest Monsters+", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollDragonQuestMonster+MangaData.txt", false },
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollOnePieceMangaData.txt", false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollNarutoMangaData.txt", false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollNarutoNovelData.txt", false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollBleachMangaData.txt", false },
            new object[] { "attack on titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollAttackOnTitanMangaData.txt", false },
            new object[] { "Goodbye, Eri", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollGoodbyeEriMangaData.txt", false },
            new object[] { "2.5 Dimensional Seduction", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollDimensionalSeductionMangaData.txt", false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollOverlordNovelData.txt", false },
            new object[] { "overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollOverlordMangaData.txt", false },
            new object[] { "07-ghost", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\Crunchyroll07GhostMangaData.txt", true },
            new object[] { "fullmetal alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollFMABMangaData.txt", false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollBerserkMangaData.txt", false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollToiletMangaData.txt", false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollCOTENovelData.txt", false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollBorutoMangaData.txt", false },
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task Crunchyroll_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
        {
            if (skip)
            {
                Assert.Ignore($"Test skipped: {title}");
                return;
            }

            await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(expectedFilePath)));
        }
    }
}