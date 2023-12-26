namespace Tests.Websites
{
    public class InStockTradesTests
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList;

        // Look to add Classroom of the Elite LN Check
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.America, Browser.Chrome);
            WebsiteList = new HashSet<Website>() {Website.InStockTrades};
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task InStockTrades_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Vol 0 that does not contain 'Vol' stirng & Novel entries")]
        public async Task InStockTrades_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesJujutsuKaisenMangaData.txt")));
        }

        [Ignore("IST Search is cancer")]
        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task InStockTrades_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task InStockTrades_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesOnePieceMangaData.txt")));
        }

        [Test]
        public async Task InStockTrades_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesNarutoMangaData.txt")));
        }

        [Test]
        public async Task InStockTrades_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesNarutoNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Character Book and Novels")]
        public async Task InStockTrades_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesBleachMangaData.txt")));
        }

        [Test, Description("Test Manga Series w/ Box Set & Omnibus in Dif Formats & Color Editions & Special Edition Volumes")]
        public async Task InStockTrades_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack on Titan", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries")]
        public async Task InStockTrades_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesOverlordNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries & Damaged Entry not Included in Final Data Set")]
        public async Task InStockTrades_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesOverlordMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Novel that doesn't have LN identifier")]
        public async Task InStockTrades_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesFMABMangaData.txt")));
        }

        [Test, Description("Validates Novel Series w/ Novel that doesn't have LN identifier")]
        public async Task InStockTrades_FMAB_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.LightNovel, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesFMABNovelData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover Volumes")]
        public async Task InStockTrades_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set w/ No Vol")]
        public async Task InStockTrades_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        [Ignore("Does not work")]
        public async Task InStockTrades_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesCOTENovelData.txt")));
        }

        [Test, Description("Ensure consistency")]
        public async Task InStockTrades_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesBorutoMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task InStockTrades_Soichi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Soichi", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\InStockTrades\InStockTradesSoichiMangaData.txt")));
        }
    }
}