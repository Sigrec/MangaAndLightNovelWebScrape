namespace Tests.Websites
{
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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManAkaneBanashiMangaData.txt", false },
            new object[] { "Jujutsu Kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManJujutsuKaisenMangaData.txt", false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManAdventuresOfDaiMangaData.txt", true },
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManOnePieceMangaData.txt", false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManNarutoMangaData.txt", false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManNarutoNovelData.txt", false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManBleachMangaData.txt", false },
            new object[] { "Attack on Titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManAttackOnTitanMangaData.txt", false },
            new object[] { "Goodbye, Eri", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManGoodbyeEriMangaData.txt", false },
            new object[] { "2.5 Dimensional Seduction", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManDimensionalSeductionMangaData.txt", true },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManOverlordNovelData.txt", false },
            new object[] { "overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManOverlordMangaData.txt", false },
            new object[] { "07-ghost", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingMan07GhostMangaData.txt", true },
            new object[] { "Fullmetal Alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManFMABMangaData.txt", false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManBerserkMangaData.txt", false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManToiletMangaData.txt", false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManCOTENovelData.txt", false },
            new object[] { "classroom of the elite", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManCOTENovelData.txt", false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManBorutoMangaData.txt", false }
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
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(expectedFilePath)));
        }
    }
}