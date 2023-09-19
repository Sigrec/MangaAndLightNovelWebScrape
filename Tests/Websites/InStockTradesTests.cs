namespace Tests.Websites
{
    public class InStockTradesTests
    {
        [SetUp]
        public void Setup()
        {
            InStockTrades.ClearData();
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public void InStockTrades_OnePiece_Manga_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("one piece", Book.Manga, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesOnePieceData.txt")));
        }

        [Test]
        public void InStockTrades_Naruto_Manga_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("Naruto", Book.Manga, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesNarutoMangaData.txt")));
        }

        [Test]
        public void InStockTrades_Naruto_Novel_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("Naruto", Book.LightNovel, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesNarutoNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Character Book and Novels")]
        public void InStockTrades_Bleach_Manga_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("Bleach", Book.Manga, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesBleachData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries")]
        public void InStockTrades_Overlord_Novel_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("Overlord", Book.LightNovel, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesOverlordNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries & Damaged Entry not Included in Final Data Set")]
        public void InStockTrades_Overlord_Manga_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("overlord", Book.Manga, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public void InStockTrades_AkaneBanashi_Manga_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("Akane-Banashi", Book.Manga, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesAkaneBanashiData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Novel that doesn't have LN identifier")]
        public void InStockTrades_FMAB_Manga_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("fullmetal alchemist", Book.Manga, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesFMABMangaData.txt")));
        }

        [Test, Description("Validates Novel Series w/ Novel that doesn't have LN identifier")]
        public void InStockTrades_FMAB_Novel_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("fullmetal alchemist", Book.LightNovel, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesFMABNovelData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover Volumes")]
        public void InStockTrades_Berserk_Manga_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("Berserk", Book.Manga, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesBerserkData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set w/ No Vol")]
        public void InStockTrades_Toilet_Manga_Test()
        {
            Assert.That(InStockTrades.GetInStockTradesData("Toilet-bound Hanako-kun", Book.Manga, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\InStockTrades\InStockTradesToiletData.txt")));
        }
    }
}