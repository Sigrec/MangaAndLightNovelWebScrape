namespace Tests.Websites
{
    public class BooksAMillionTests
    {
        MasterScrape Scrape;
        List<Website> WebsiteList;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.America, Browser.Chrome);
            WebsiteList = new List<Website>() {Website.BooksAMillion};
        }

        [Test, Description("Test Manga Series w/ Box Set & Omnibus in Dif Formats & Color Editions")]
        public async Task BooksAMillion_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack on Titan", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task BooksAMillion_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionOnePieceMangaData.txt")));
        }

        [Test]
        public async Task BooksAMillion_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionNarutoMangaData.txt")));
        }

        [Test, Description("Validates Novel Series w/ Entries that have no 'Vol' & 'Vol' in title")]
        public async Task BooksAMillion_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionNarutoNovelData.txt")));
        }

        [Test, Description("Manga w/ 2-in-1 Omni & Omni w/ Missing Info")]
        public async Task BooksAMillion_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionBleachMangaData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public async Task BooksAMillion_COTE_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionCOTEMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries")]
        public async Task BooksAMillion_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionCOTENovelData.txt")));
        }

        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        public async Task BooksAMillion_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionOverlordNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task BooksAMillion_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task BooksAMillion_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task BooksAMillion_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionAkaneBanashiMangaData.txt")));
        }

        // [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        // public async Task BooksAMillion_07Ghost_Manga_Test()
        // {
        //     Assert.That(BooksAMillion.GetBooksAMillionData("07-ghost", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillion07GhostMangaData.txt")));
        // }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public async Task BooksAMillion_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes")]
        public async Task BooksAMillion_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task BooksAMillion_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionBerserkMangaData.txt")));
        }

        // [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        // public async Task BooksAMillion_Toilet_Manga_Test()
        // {
        //     Assert.That(BooksAMillion.GetBooksAMillionData("Toilet-bound Hanako-kun", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionToiletMangaData.txt")));
        // }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task BooksAMillion_Member_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList, false, true);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillionGoodbyeEriMangaData.txt")));
        }
    }
}