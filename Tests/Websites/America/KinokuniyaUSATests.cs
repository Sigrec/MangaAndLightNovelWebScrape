namespace Tests.Websites
{
    public class KinokuniyaUSATests
    {
        MasterScrape Scrape;
        List<Website> WebsiteList;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.America, Browser.Chrome);
            WebsiteList = new List<Website>() {Website.KinokuniyaUSA};
        }

        [Test, Description("Validates Manga w/ Box Sets & Omnibus Volumes")]
        public async Task KinokuniyaUSA_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSAOnePieceMangaData.txt")));
        }

        [Test, Description("Valides Non Manga Volumes & Manga Volume w/ no Number")]
        public async Task KinokuniyaUSA_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSANarutoMangaData.txt")));
        }

        [Test]
        public async Task KinokuniyaUSA_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSANarutoNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ 2-in-1 Omnibus")]
        public async Task KinokuniyaUSA_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSABleachMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public async Task KinokuniyaUSA_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSAJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Validates Member Status & Series w/ Non Letter or Digit Char in Title")]
        public async Task KinokuniyaUSA_Member_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSAAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga w/ Based on Series that is a Novel")]
        public async Task KinokuniyaUSA_COTE_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSACOTEMangaData.txt")));
        }

         [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task KinokuniyaUSA_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSACOTENovelData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task KinokuniyaUSA_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSADimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Special Edition, Paperback, & Omni Volumes")]
        public async Task KinokuniyaUSA_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSAFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task KinokuniyaUSA_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSABerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set without Box Set Indicator")]
        public async Task KinokuniyaUSA_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSAToiletMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task KinokuniyaUSA_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\KinokuniyaUSA\KinokuniyaUSAGoodbyeEriMangaData.txt")));
        }
    }
}