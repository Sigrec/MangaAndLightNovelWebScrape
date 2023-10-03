namespace Tests.Websites
{
    public class InStockTradesTests
    {
        MasterScrape Scrape;
        List<Website> WebsiteList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.America, Browser.Chrome);
            WebsiteList = new List<Website>() {Website.InStockTrades};
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task InStockTrades_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesOnePieceMangaData.txt")));
        }

        [Test]
        public async Task InStockTrades_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesNarutoMangaData.txt")));
        }

        [Test]
        public async Task InStockTrades_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesNarutoNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Character Book and Novels")]
        public async Task InStockTrades_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesBleachMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries")]
        public async Task InStockTrades_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesOverlordNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries & Damaged Entry not Included in Final Data Set")]
        public async Task InStockTrades_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task InStockTrades_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Novel that doesn't have LN identifier")]
        public async Task InStockTrades_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesFMABMangaData.txt")));
        }

        [Test, Description("Validates Novel Series w/ Novel that doesn't have LN identifier")]
        public async Task InStockTrades_FMAB_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesFMABNovelData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover Volumes")]
        public async Task InStockTrades_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set w/ No Vol")]
        public async Task InStockTrades_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesToiletMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task InStockTrades_Soichi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Soichi", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\InStockTrades\InStockTradesSoichiMangaData.txt")));
        }
    }
}