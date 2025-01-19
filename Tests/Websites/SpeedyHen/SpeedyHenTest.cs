namespace Tests.Websites
{
    [TestFixture, Description("Validations for SpeedyHen")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class SpeedyHenTests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [Website.SpeedyHen];

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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenAkaneBanashiMangaData.txt", false },
            new object[] { "jujutsu kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenJujutsuKaisenMangaData.txt", false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenAdventuresOfDaiMangaData.txt", true },  // Skip test
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenOnePieceMangaData.txt", false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenNarutoMangaData.txt", false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenNarutoNovelData.txt", false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenBleachMangaData.txt", false },
            new object[] { "Attack on Titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenAttackOnTitanMangaData.txt", false },
            new object[] { "Goodbye, Eri", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenGoodbyeEriMangaData.txt", false },
            new object[] { "2.5 Dimensional Seduction", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenDimensionalSeductionMangaData.txt", false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenOverlordNovelData.txt", false },
            new object[] { "overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenOverlordMangaData.txt", false },
            new object[] { "07-ghost", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHen07GhostMangaData.txt", true },  // Skip test
            new object[] { "Fullmetal Alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenFMABMangaData.txt", false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenBerserkMangaData.txt", false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenToiletMangaData.txt", false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenCOTENovelData.txt", false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenBorutoMangaData.txt", false }
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task SpeedyHen_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
        {
            if (skip)
            {
                Assert.Ignore($"Test skipped: {title}");
                return;
            }

            // Scrape data and compare results with the expected data from the file
            await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(expectedFilePath)));
        }
    }
}