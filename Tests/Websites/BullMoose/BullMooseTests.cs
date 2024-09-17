namespace Tests.Websites
{
    [TestFixture, Description("Validations for Bull Moose")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class BullMooseTests
    {
        private static MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [ Website.BullMoose ];

        [SetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task BullMoose_AkaneBanashi_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Vol 0 that does not contain 'Vol' stirng & Novel entries")]
        public async Task BullMoose_JujutsuKaisen_Manga_Europe_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task BullMoose_AdventuresOfDai_Manga_Canada_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task BullMoose_OnePiece_Manga_Canada_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseOnePieceMangaData.txt")));
        }

        [Test]
        public async Task BullMoose_Naruto_Manga_Britain_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseNarutoMangaData.txt")));
        }

        [Test]
        [Ignore("Hard")]
        public async Task BullMoose_Naruto_Novel_America_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseNarutoNovelData.txt")));
        }

        [Test]
        public async Task BullMoose_Bleach_Manga_Europe_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseBleachMangaData.txt")));
        }

        [Test, Description("Validates Series w/ dif Types of Omnibus & Box Set Entries")]
        public async Task BullMoose_AttackOnTitan_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("attack on titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task BullMoose_GoodbyeEri_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task BullMoose_DimensionalSeduction_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        [Ignore("")]
        public async Task BullMoose_Overlord_Novel_Britain_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseOverlordNovelData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task BullMoose_Overlord_Manga_Europe_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public async Task BullMoose_07Ghost_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMoose07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes & Imperfect Volume")]
        public async Task BullMoose_FMAB_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task BullMoose_Berserk_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        public async Task BullMoose_Toilet_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task BullMoose_COTE_Novel_America_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseCOTENovelData.txt")));
        }

        [Test]
        public async Task BullMoose_Boruto_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseBorutoMangaData.txt")));
        }

        [Test]
        public async Task BullMoose_Noragami_Manga_America_Test()
        {
            await Scrape.InitializeScrapeAsync("Noragami", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BullMoose\BullMooseNoragamiMangaData.txt")));
        }
    }
}