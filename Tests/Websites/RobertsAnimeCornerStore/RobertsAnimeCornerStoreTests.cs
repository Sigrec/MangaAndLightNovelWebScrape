namespace Tests.Websites
{
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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAkaneBanashiMangaData.txt", false },
            new object[] { "jujutsu kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreJujutsuKaisenMangaData.txt", false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAdventuresOfDaiMangaData.txt", false },
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOnePieceMangaData.txt", false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreNarutoMangaData.txt", false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreNarutoNovelData.txt", false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBleachMangaData.txt", false },
            new object[] { "Attack on Titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAttackOnTitanMangaData.txt", false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOverlordNovelData.txt", false },
            new object[] { "Overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOverlordMangaData.txt", false },
            new object[] { "Fullmetal Alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreFMABMangaData.txt", false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBerserkMangaData.txt", false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreToiletMangaData.txt", false },
            new object[] { "classroom of the elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreCOTENovelData.txt", false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBorutoMangaData.txt", false },
            new object[] { "Persona 4", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStorePersona4MangaData.txt", false }
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
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(expectedFilePath)));
        }
    }
}