namespace Tests.Websites
{
    public class IndigoTests
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.Canada, Browser.Chrome);
            WebsiteList = new HashSet<Website>() {Website.Indigo};
        }

        [Test, Description("Test Manga Series w/ Box Set & Omnibus in Dif Formats & Color Editions")]
        public async Task Indigo_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack on Titan", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task Indigo_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoOnePieceMangaData.txt")));
        }

        [Test]
        public async Task Indigo_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoNarutoMangaData.txt")));
        }

        [Test]
        public async Task Indigo_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoNarutoNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Character Book and Novels")]
        public async Task Indigo_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoBleachMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries")]
        public async Task Indigo_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoOverlordNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries & Damaged Entry not Included in Final Data Set")]
        public async Task Indigo_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task Indigo_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Novel that doesn't have LN identifier")]
        public async Task Indigo_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoFMABMangaData.txt")));
        }

        [Test, Description("Validates Novel Series w/ Novel that doesn't have LN identifier")]
        public async Task Indigo_FMAB_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoFMABNovelData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover Volumes")]
        public async Task Indigo_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set w/ No Vol")]
        public async Task Indigo_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoToiletMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task Indigo_Member_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList, false, false, false, true);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task Indigo_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoCOTENovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public async Task Indigo_COTE_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Indigo\IndigoCOTEMangaData.txt")));
        }
    }
}