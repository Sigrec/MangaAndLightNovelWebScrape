namespace Tests.Websites
{
    public class BarnesAndNobleTests
    {
        MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [ Website.BarnesAndNoble ];
    
        [OneTimeSetUp]
        public void Setup()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
        }

        [TearDown]
        public void TearDown()
        {
            Scrape.IsBarnesAndNobleMember = false;
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task BarnesAndNoble_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public async Task BarnesAndNoble_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Jujutsu Kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task BarnesAndNoble_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task BarnesAndNoble_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleOnePieceMangaData.txt")));
        }

        [Test]
        public async Task BarnesAndNoble_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleNarutoMangaData.txt")));
        }

        [Test]
        public async Task BarnesAndNoble_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleNarutoNovelData.txt")));
        }

        [Test]
        public async Task BarnesAndNoble_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleBleachMangaData.txt")));
        }

        [Test, Description("Test Manga Series w/ Box Set & Omnibus in Dif Formats & Color Editions")]
        public async Task BarnesAndNoble_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack on Titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task BarnesAndNoble_Member_GoodbyeEri_Manga_Test()
        {
            Scrape.IsBarnesAndNobleMember = true;
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ number & '.' in book title")]
        public async Task BarnesAndNoble_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleDimensionalSeductionMangaData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task BarnesAndNoble_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleOverlordMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        public async Task BarnesAndNoble_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleOverlordNovelData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public async Task BarnesAndNoble_07Ghost_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNoble07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes")]
        public async Task BarnesAndNoble_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Editions & Removal of Series w/ same book title")]
        public async Task BarnesAndNoble_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        public async Task BarnesAndNoble_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task BarnesAndNoble_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleCOTENovelData.txt")));
        }

        [Test]
        public async Task BarnesAndNoble_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleBorutoMangaData.txt")));
        }

        [Test, Description("Tests series with omnibus that has leading text after vol #")]
        public async Task BarnesAndNoble_Noragami_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Noragami", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BarnesAndNoble\BarnesAndNobleNoragamiMangaData.txt")));
        }
    }
}