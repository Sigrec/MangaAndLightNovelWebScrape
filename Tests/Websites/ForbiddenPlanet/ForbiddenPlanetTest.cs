namespace Tests.Websites
{
    [TestFixture, Description("Validations for Forbidden Planet")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class ForbiddenPlanetTests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [Website.ForbiddenPlanet];

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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetAkaneBanashiMangaData.txt", false },
            new object[] { "Jujutsu Kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetJujutsuKaisenMangaData.txt", false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetAdventuresOfDaiMangaData.txt", false },
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetOnePieceManga.txt", false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetNarutoManga.txt", false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetNarutoNovelData.txt", false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetBleachManga.txt", false },
            new object[] { "Attack On Titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetAttackOnTitanManga.txt", false },
            new object[] { "Goodbye, Eri", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetGoodbyeEriMangaData.txt", false },
            new object[] { "2.5 Dimensional Seduction", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetDimensionalSeductionMangaData.txt", false },
            new object[] { "overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetOverlordMangaData.txt", false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetOverlordNovelData.txt", false },
            new object[] { "07-ghost", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanet07GhostMangaData.txt", false },
            new object[] { "fullmetal alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetFMABMangaData.txt", false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetBerserkMangaData.txt", false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetToiletMangaData.txt", false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetCOTENovelData.txt", false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetBorutoMangaData.txt", false },
            new object[] { "Noragami", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetNoragamiMangaData.txt", false }
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task ForbiddenPlanet_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
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