namespace Tests.Websites.America
{
    [TestFixture]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class RobertsAnimeCornerStoreTest
    {
        MasterScrape Scrape;
        List<Website> WebsiteList;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.America, Browser.Chrome);
            WebsiteList = new List<Website>() {Website.RobertsAnimeCornerStore};
        }

        [Test]
        public async Task RobertsAnimeCornerStore_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOnePieceMangaData.txt")));
        }

        [Test]
        public async Task RobertsAnimeCornerStore_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreNarutoMangaData.txt")));
        }

        [Test]
        public async Task RobertsAnimeCornerStore_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBleachMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task RobertsAnimeCornerStore_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreCOTENovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public async Task RobertsAnimeCornerStore_COTE_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreCOTEMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number & Non Letter Character")]
        public async Task RobertsAnimeCornerStore_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task RobertsAnimeCornerStore_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Special Edition & Paperback Volumes")]
        public async Task RobertsAnimeCornerStore_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task RobertsAnimeCornerStore_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBerserkMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task RobertsAnimeCornerStore_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreGoodbyeEriMangaData.txt")));
        }
    }
}