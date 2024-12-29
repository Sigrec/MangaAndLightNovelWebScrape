namespace Tests.Websites
{
    [TestFixture, Description("Validations for BooksAMillion")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class BooksAMillionTests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [ Website.BooksAMillion ];

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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionAkaneBanashiMangaData.txt", false, false },
            new object[] { "jujutsu kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionJujutsuKaisenMangaData.txt", false, false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionAdventuresOfDaiMangaData.txt", false, false },
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionOnePieceMangaData.txt", false, false },
            new object[] { "Goodbye, Eri", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionGoodbyeEriMangaData.txt", false, true },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionNarutoMangaData.txt", false, false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionNarutoNovelData.txt", false, false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionBleachMangaData.txt", false, false },
            new object[] { "Attack on Titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionAttackOnTitanMangaData.txt", false, false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionOverlordNovelData.txt", false, false },
            new object[] { "Overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionOverlordMangaData.txt", false, false },
            new object[] { "Fullmetal Alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionFMABMangaData.txt", false, false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionBerserkMangaData.txt", false, false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionToiletMangaData.txt", false, false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionCOTENovelData.txt", false, false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionBorutoMangaData.txt", false, false },
            new object[] { "Noragami", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionNoragamiMangaData.txt", false, false }
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task BooksAMillion_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip, bool isMember)
        {
            if (skip)
            {
                Assert.Ignore($"Test skipped: '{title}'");
                return;
            }

            Scrape.IsBooksAMillionMember = isMember;
            await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(expectedFilePath)));
        }
    }
}