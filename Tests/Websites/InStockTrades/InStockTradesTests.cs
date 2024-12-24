namespace Tests.Websites
{
    [TestFixture, Description("Validations for InStockTrades")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class InStockTradesTests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [Website.InStockTrades];

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
            new object[] { "Akane-Banashi", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesAkaneBanashiMangaData.txt", false },
            new object[] { "jujutsu kaisen", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesJujutsuKaisenMangaData.txt", false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesAdventuresOfDaiMangaData.txt", true },
            new object[] { "One Piece", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesOnePieceMangaData.txt", false },
            new object[] { "Naruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesNarutoMangaData.txt", false },
            new object[] { "Naruto", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesNarutoNovelData.txt", false },
            new object[] { "Bleach", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesBleachMangaData.txt", false },
            new object[] { "Attack on Titan", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesAttackOnTitanMangaData.txt", false },
            new object[] { "Overlord", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesOverlordNovelData.txt", false },
            new object[] { "Overlord", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesOverlordMangaData.txt", false },
            new object[] { "Fullmetal Alchemist", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesFMABMangaData.txt", false },
            new object[] { "Fullmetal Alchemist", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesFMABNovelData.txt", false },
            new object[] { "Berserk", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesBerserkMangaData.txt", false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesToiletMangaData.txt", false },
            new object[] { "classroom of elite", BookType.LightNovel, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesCOTENovelData.txt", false },
            new object[] { "Boruto", BookType.Manga, @"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesBorutoMangaData.txt", false },
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task InStockTrades_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip)
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
