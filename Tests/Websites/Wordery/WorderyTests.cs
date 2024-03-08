namespace Tests.Websites
{
    [TestFixture, Description("Validations for Wordery")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class WorderyTests
    {
        private static MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [ Website.Wordery ];

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task Wordery_AkaneBanashi_Manga_Australia_Test()
        {
            Scrape.Region = Region.Australia;
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Vol 0 that does not contain 'Vol' stirng & Novel entries")]
        public async Task Wordery_JujutsuKaisen_Manga_Europe_Test()
        {
            Scrape.Region = Region.Europe;
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task Wordery_AdventuresOfDai_Manga_Canada_Test()
        {
            Scrape.Region = Region.Canada;
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task Wordery_OnePiece_Manga_Australia_Test()
        {
            Scrape.Region = Region.Australia;
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyOnePieceMangaData.txt")));
        }

        [Test]
        public async Task Wordery_Naruto_Manga_Britain_Test()
        {
            Scrape.Region = Region.Britain;
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyNarutoMangaData.txt")));
        }

        [Test, Description("Validates Series w/out volumes numbers")]
        public async Task Wordery_Naruto_Novel_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyNarutoNovelData.txt")));
        }

        [Test]
        public async Task Wordery_Bleach_Manga_Europe_Test()
        {
            Scrape.Region = Region.Europe;
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyBleachMangaData.txt")));
        }

        [Test, Description("Validates Series w/ dif Types of Omnibus & Box Set Entries")]
        public async Task Wordery_AttackOnTitan_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("attack on titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task Wordery_GoodbyeEri_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task Wordery_DimensionalSeduction_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        public async Task Wordery_Overlord_Novel_Britain_Test()
        {
            Scrape.Region = Region.Britain;
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyOverlordNovelData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task Wordery_Overlord_Manga_Europe_Test()
        {
            Scrape.Region = Region.Europe;
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public async Task Wordery_07Ghost_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\Wordery07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes & Imperfect Volume")]
        public async Task Wordery_FMAB_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task Wordery_Berserk_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        public async Task Wordery_Toilet_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task Wordery_COTE_Novel_Canada_Test()
        {
            Scrape.Region = Region.Canada;
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyCOTENovelData.txt")));
        }

        [Test]
        public async Task Wordery_Boruto_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Boruto: Naruto Next Generations", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyBorutoMangaData.txt")));
        }

        [Test]
        public async Task Wordery_Noragami_Manga_America_Test()
        {
            Scrape.Region = Region.America;
            await Scrape.InitializeScrapeAsync("Noragami", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Wordery\WorderyNoragamiMangaData.txt")));
        }
    }
}