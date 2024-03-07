namespace Tests.Websites
{
    [TestFixture, Description("Validations for Crunchyroll")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class CrunchyrollTests
    {
        MasterScrape Scrape;
        private readonly HashSet<Website> WebsiteList = [ Website.Crunchyroll ];

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
            
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task Crunchyroll_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Vol 0 that does not contain 'Vol' stirng & Novel entries")]
        public async Task Crunchyroll_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task Crunchyroll_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task Crunchyroll_OnePiece_Test()
        {
            await Scrape.InitializeScrapeAsync("One Piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollOnePieceMangaData.txt")));
        }

        [Test]
        public async Task Crunchyroll_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollNarutoMangaData.txt")));
        }

        [Test]
        public async Task Crunchyroll_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollNarutoNovelData.txt")));
        }

        [Test]
        public async Task Crunchyroll_Bleach_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollBleachMangaData.txt")));
        }

        [Test, Description("Validates Series w/ dif Types of Omnibus & Box Set Entries")]
        public async Task Crunchyroll_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("attack on titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task Crunchyroll_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task Crunchyroll_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        public async Task Crunchyroll_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollOverlordNovelData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task Crunchyroll_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public async Task Crunchyroll_07Ghost_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\Crunchyroll07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes & Imperfect Volume")]
        public async Task Crunchyroll_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task Crunchyroll_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set with Specific name")]
        public async Task Crunchyroll_ToiletBound_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task Crunchyroll_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollCOTENovelData.txt")));
        }

        [Test, Description("Ensure consistency")]
        public async Task Crunchyroll_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Crunchyroll\CrunchyrollBorutoMangaData.txt")));
        }
    }
}