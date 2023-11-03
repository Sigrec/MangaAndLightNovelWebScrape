namespace Tests.Websites
{
    [TestFixture, Description("Validations for SciFier")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class SciFierTests
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.America, Browser.Chrome);
            WebsiteList = new HashSet<Website>() { Website.SciFier };
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task SciFier_OnePiece_Manga_Canada_Test()
        {
            Scrape.Region = Region.Canada;
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierOnePieceMangaData.txt")));
        }

        [Test]
        public async Task SciFier_Naruto_Manga_Britain_Test()
        {
            Scrape.Region = Region.Britain;
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierNarutoMangaData.txt")));
        }

        [Test]
        public async Task SciFier_Boruto_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierBorutoMangaData.txt")));
        }

        [Test]
        [Ignore("Hard")]
        public async Task SciFier_Naruto_Novel_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierNarutoNovelData.txt")));
        }

        [Test]
        public async Task SciFier_Bleach_Test()
        {
            Scrape.Region = Region.Europe;
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierBleachMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task SciFier_COTE_Novel_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierCOTENovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public async Task SciFier_COTE_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierCOTEMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task SciFier_DimensionalSeduction_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task SciFier_AkaneBanashi_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public async Task SciFier_07Ghost_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFier07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task SciFier_Berserk_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes & Imperfect Volume")]
        public async Task SciFier_FMAB_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierFMABMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task SciFier_GoodbyeEri_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierGoodbyeEriMangaData.txt")));
        }

         [Test, Description("Validates Series w/ dif Types of Omnibus & Box Set Entries")]
        public async Task SciFier_AttackOnTitan_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("attack on titan", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        public async Task SciFier_Toilet_Manga_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SciFier\SciFierToiletMangaData.txt")));
        }
    }
}