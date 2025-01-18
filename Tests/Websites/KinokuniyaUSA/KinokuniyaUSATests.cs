namespace Tests.Websites
{
    [TestFixture, Description("Validations for KinokuniyaUSA")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class KinokuniyaUSATests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [ Website.KinokuniyaUSA ];

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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAAkaneBanashiMangaData.txt", false, true },
            new object[] { "jujutsu kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAJujutsuKaisenMangaData.txt", false, false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAAdventuresOfDaiMangaData.txt", false, false },
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAOnePieceMangaData.txt", false, false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSANarutoMangaData.txt", false, false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSANarutoNovelData.txt", false, false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSABleachMangaData.txt", false, false },
            new object[] { "Attack on Titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAAttackOnTitanMangaData.txt", false, false },
            new object[] { "Goodbye, Eri", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAGoodbyeEriMangaData.txt", false, false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAOverlordNovelData.txt", false, false },
            new object[] { "Overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAOverlordMangaData.txt", false, false },
            new object[] { "07-ghost", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSA07GhostMangaData.txt", false, false },
            new object[] { "fullmetal alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAFMABMangaData.txt", false, false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSABerserkMangaData.txt", false, false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAToiletMangaData.txt", false, false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSACOTENovelData.txt", false, false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSABorutoMangaData.txt", false, false },
            new object[] { "Noragami", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSANoragamiMangaData.txt", false, false }
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task KinokuniyaUSA_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip, bool isMember)
        {
            if (skip)
            {
                Assert.Ignore($"Test skipped: '{title}'");
                return;
            }

            Scrape.IsKinokuniyaUSAMember = isMember;
            await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(expectedFilePath)));
        }
    }
}