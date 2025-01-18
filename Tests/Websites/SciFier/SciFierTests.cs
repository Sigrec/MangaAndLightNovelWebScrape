namespace Tests.Websites
{
    [TestFixture, Description("Validations for SciFier")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class SciFierTests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = new HashSet<Website> { Website.SciFier };

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
            new object[] { "Akane-Banashi", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierAkaneBanashiMangaData.txt", false },
            new object[] { "jujutsu kaisen", BookType.Manga, Region.Europe, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierJujutsuKaisenMangaData.txt", false }, // Issues
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, Region.Canada, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierAdventuresOfDaiMangaData.txt", false },
            new object[] { "one piece", BookType.Manga, Region.Canada, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierOnePieceMangaData.txt", false },
            new object[] { "Naruto", BookType.Manga, Region.Britain, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierNarutoMangaData.txt", false },
            new object[] { "Naruto", BookType.LightNovel, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierNarutoNovelData.txt", true }, // Issues
            new object[] { "Bleach", BookType.Manga, Region.Europe, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierBleachMangaData.txt", false },
            new object[] { "attack on titan", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierAttackOnTitanMangaData.txt", false }, // Issues
            new object[] { "Goodbye, Eri", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierGoodbyeEriMangaData.txt", false },
            new object[] { "2.5 Dimensional Seduction", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierDimensionalSeductionMangaData.txt", false },
            new object[] { "Overlord", BookType.LightNovel, Region.Britain, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierOverlordNovelData.txt", true }, // Issues
            new object[] { "overlord", BookType.Manga, Region.Europe, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierOverlordMangaData.txt", false }, // Issues
            new object[] { "07-ghost", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFier07GhostMangaData.txt", false },
            new object[] { "fullmetal alchemist", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierFMABMangaData.txt", false },
            new object[] { "Berserk", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierBerserkMangaData.txt", false }, // Issues
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierToiletMangaData.txt", false }, // Issues
            new object[] { "classroom of the elite", BookType.LightNovel, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierCOTENovelData.txt", false },
            new object[] { "Boruto", BookType.Manga, Region.Australia, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierBorutoMangaData.txt", false },
            new object[] { "Noragami", BookType.Manga, Region.America, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierNoragamiMangaData.txt", false },
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task SciFier_Scrape_Test(string title, BookType bookType, Region region, string expectedFilePath, bool skip)
        {
            if (skip)
            {
                Assert.Ignore($"Test skipped: '{title}'");
                return;
            }

            Scrape.Region = region;
            await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(expectedFilePath)));
        }
    }
}