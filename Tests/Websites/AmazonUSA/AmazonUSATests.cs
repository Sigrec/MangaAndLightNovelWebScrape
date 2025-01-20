namespace Tests.Websites
{
    [TestFixture, Description("Validations for AmazonUSA")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class AmazonUSATests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = new HashSet<Website>() { Website.AmazonUSA };

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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAAkaneBanashiMangaData.txt", false },
            new object[] { "jujutsu kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAJujutsuKaisenMangaData.txt", false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAAdventuresOfDaiMangaData.txt", false },
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAOnePieceMangaData.txt", false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSANarutoMangaData.txt", false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSANarutoNovelData.txt", false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSABleachMangaData.txt", false },
            new object[] { "attack on titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAAttackOnTitanMangaData.txt", false },
            new object[] { "Goodbye, Eri", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAGoodbyeEriMangaData.txt", false },
            new object[] { "2.5 Dimensional Seduction", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSADimensionalSeductionMangaData.txt", false },
            new object[] { "Overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAOverlordMangaData.txt", false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAOverlordNovelData.txt", false },
            new object[] { "07-ghost", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSA07GhostMangaData.txt", false },
            new object[] { "fullmetal alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAFMABMangaData.txt", false },
            new object[] { "fullmetal alchemist", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAFMABNovelData.txt", true },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSABerserkMangaData.txt", false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAToiletMangaData.txt", false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSACOTENovelData.txt", false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSABorutoMangaData.txt", false }
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task AmazonUSA_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
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