namespace Tests.Websites
{
    public class KinokuniyaUSATests
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList = new HashSet<Website>() {Website.KinokuniyaUSA};
        
        [SetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
        }

        [TearDown]
        public void TearDown()
        {
            Scrape.IsKinokuniyaUSAMember = false;
        }

        [Test, Description("Validates Member Status & Series w/ Non Letter or Digit Char in Title")]
        public async Task KinokuniyaUSA_Member_AkaneBanashi_Manga_Test()
        {
            Scrape.IsKinokuniyaUSAMember = true;
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public async Task KinokuniyaUSA_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task KinokuniyaUSA_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Validates Manga w/ Box Sets & Omnibus Volumes")]
        public async Task KinokuniyaUSA_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAOnePieceMangaData.txt")));
        }

        [Test, Description("Valides Non Manga Volumes & Manga Volume w/ no Number")]
        public async Task KinokuniyaUSA_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSANarutoMangaData.txt")));
        }

        [Test]
        public async Task KinokuniyaUSA_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSANarutoNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ 2-in-1 Omnibus")]
        public async Task KinokuniyaUSA_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSABleachMangaData.txt")));
        }

        [Test, Description("Test Manga Series w/ Box Set & Omnibus in Dif Formats & Color Editions")]
        public async Task KinokuniyaUSA_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack on Titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task KinokuniyaUSA_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task KinokuniyaUSA_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSADimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        public async Task KinokuniyaUSA_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAOverlordNovelData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task KinokuniyaUSA_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ '-' in book title")]
        public async Task KinokuniyaUSA_07Ghost_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSA07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Special Edition, Paperback, & Omni Volumes")]
        public async Task KinokuniyaUSA_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task KinokuniyaUSA_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSABerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set without Box Set Indicator")]
        public async Task KinokuniyaUSA_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSAToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task KinokuniyaUSA_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSACOTENovelData.txt")));
        }

        [Test, Description("Ensure consistency")]
        public async Task KinokuniyaUSA_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSABorutoMangaData.txt")));
        }

        [Test, Description("Ensure consistency")]
        public async Task KinokuniyaUSA_Noragami_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Noragami", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\KinokuniyaUSA\KinokuniyaUSANoragamiMangaData.txt")));
        }
    }
}