namespace Tests.Websites
{
    [TestFixture, Description("Validations for SpeedyHen")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class SpeedyHenTests
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.Britain);
            WebsiteList = new HashSet<Website>() {Website.SpeedyHen};
        }

        [Test, Description("Validates Series w/ '-' in title")]
        public async Task SpeedyHen_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Novel Entries")]
        public async Task SpeedyHen_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Validates series that contains a keyword to skip and contains ':'")]
        public async Task SpeedyHen_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Box Sets, Omnibus, Manga w/ no Vol #, & Non Manga related entries")]
        public async Task SpeedyHen_OnePiece_Test()
        {
            await Scrape.InitializeScrapeAsync("One Piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenOnePieceMangaData.txt")));
        }

        [Test, Description("Validates Series w/ 3-in-1 Entries & Novels")]
        public async Task SpeedyHen_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenNarutoMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Manga Entries")]
        public async Task SpeedyHen_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenNarutoNovelData.txt")));
        }

        [Test, Description("Validates Series w/ 2-in-1 Omni & Collectors Edition (Unavailable)")]
        public async Task SpeedyHen_Bleach_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenBleachMangaData.txt")));
        }

        [Test, Description("Validates Series w/ dif Types of Omnibus & Box Set Entries, No 'Vol' in entry title, & Special Edition")]
        public async Task SpeedyHen_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("attack on titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task SpeedyHen_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number & '.' in title")]
        public async Task SpeedyHen_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Novel Series w/ text after vol #")]
        public async Task SpeedyHen_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenOverlordNovelData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        [Ignore("Currently issue where some volumes don't have a number or anything")]
        public async Task SpeedyHen_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public async Task SpeedyHen_07Ghost_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHen07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes & Imperfect Volume")]
        public async Task SpeedyHen_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task SpeedyHen_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set with Specific name")]
        public async Task SpeedyHen_ToiletBound_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task SpeedyHen_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenCOTENovelData.txt")));
        }

        [Test, Description("Ensure consistency")]
        public async Task SpeedyHen_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\SpeedyHen\SpeedyHenBorutoMangaData.txt")));
        }

        // Cells at Work!
    }
}