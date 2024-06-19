namespace Tests.Websites
{
    [TestFixture, Description("Validations for AmazonUSA")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class AmazonUSATests
    {
        MasterScrape Scrape;
        private readonly HashSet<Website> WebsiteList = [ Website.AmazonUSA ];

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
            
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task AmazonUSA_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Vol 0 that does not contain 'Vol' stirng & Novel entries")]
        public async Task AmazonUSA_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task AmazonUSA_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, Manga w/ No Vol Number, Manga w/ Hardcover, & Series w/ non book entries")]
        public async Task AmazonUSA_OnePiece_Test()
        {
            await Scrape.InitializeScrapeAsync("One Piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAOnePieceMangaData.txt")));
        }

        [Test]
        [Ignore("")]
        public async Task AmazonUSA_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSANarutoMangaData.txt")));
        }

        [Test]
        [Ignore("")]
        public async Task AmazonUSA_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSANarutoNovelData.txt")));
        }

        [Test]
        [Ignore("")]
        public async Task AmazonUSA_Bleach_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSABleachMangaData.txt")));
        }

        [Test, Description("Validates Series w/ dif Types of Omnibus & Box Set Entries")]
        [Ignore("")]
        public async Task AmazonUSA_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("attack on titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        [Ignore("")]
        public async Task AmazonUSA_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        [Ignore("")]
        public async Task AmazonUSA_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSADimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        [Ignore("")]
        public async Task AmazonUSA_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAOverlordNovelData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        [Ignore("")]
        public async Task AmazonUSA_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        [Ignore("")]
        public async Task AmazonUSA_07Ghost_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSA07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes & Imperfect Volume")]
        [Ignore("")]
        public async Task AmazonUSA_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        [Ignore("")]
        public async Task AmazonUSA_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSABerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set with Specific name")]
        [Ignore("")]
        public async Task AmazonUSA_ToiletBound_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSAToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        [Ignore("")]
        public async Task AmazonUSA_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSACOTENovelData.txt")));
        }

        [Test, Description("Ensure consistency")]
        [Ignore("")]
        public async Task AmazonUSA_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\AmazonUSA\AmazonUSABorutoMangaData.txt")));
        }
    }
}