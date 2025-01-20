namespace Tests.Websites
{
    [TestFixture, Description("Validations for Indigo")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class IndigoTests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [Website.Indigo];

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.Canada);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Scrape.IsIndigoMember = false;
        }

        private static readonly object[] ScrapeTestCases =
        [
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoAkaneBanashiMangaData.txt", false, false },
            new object[] { "jujutsu kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoJujutsuKaisenMangaData.txt", false, false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoAdventuresOfDaiMangaData.txt", false, false },
            new object[] { "one piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoOnePieceMangaData.txt", false, false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoNarutoMangaData.txt", false, false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoNarutoNovelData.txt", false, false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoBleachMangaData.txt", false, false },
            new object[] { "Attack on Titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoAttackOnTitanMangaData.txt", false, false },
            new object[] { "Goodbye, Eri", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoGoodbyeEriMangaData.txt", true, false },
            new object[] { "2.5 Dimensional Seduction", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoDimensionalSeductionMangaData.txt", false, false },
            new object[] { "Overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoOverlordMangaData.txt", false, false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoOverlordNovelData.txt", false, false },
            new object[] { "07-ghost", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\Indigo07GhostMangaData.txt", false, false },
            new object[] { "fullmetal alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoFMABMangaData.txt", false, false },
            new object[] { "fullmetal alchemist", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoFMABNovelData.txt", false, false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoBerserkMangaData.txt", false, false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoToiletMangaData.txt", false, false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoCOTENovelData.txt", false, false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoBorutoMangaData.txt", false, false },
            new object[] { "Noragami", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\Indigo\IndigoNoragamiMangaData.txt", false, false }
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task Indigo_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool isMember, bool skip)
        {
            if (skip)
            {
                Assert.Ignore($"Test skipped: {title}");
                return;
            }

            Scrape.IsIndigoMember = isMember;

            await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(expectedFilePath)));
        }
    }
}