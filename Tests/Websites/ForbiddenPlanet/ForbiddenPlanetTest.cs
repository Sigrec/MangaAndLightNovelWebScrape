namespace Tests.Websites
{
    [TestFixture, Description("Validations for Forbidden Planet")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    [Ignore("")]
    public class ForbiddenPlanetTest
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList = new HashSet<Website>() { Website.ForbiddenPlanet };
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.Britain);    
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task ForbiddenPlanet_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public async Task ForbiddenPlanet_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Jujutsu Kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task ForbiddenPlanet_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Test Manga Series w/ Omnibus of Dif Text Format & Light Novels")]
        public async Task ForbiddenPlanet_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("One Piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetOnePieceManga.txt")));
        }

        [Test, Description("Test Manga Series w/ Omnibus of Dif Text Format & Light Novels")]
        public async Task ForbiddenPlanet_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetNarutoManga.txt")));
        }

        [Test, Description("Validates Novel series w/ manga original source & entries without vol #'s")]
        [Ignore("Novel Not Working")]
        public async Task Waterstones_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\Waterstones\WaterstonesNarutoNovelData.txt")));
        }

        [Test, Description("Test Manga Series w/ Box Set w/out Num Indicator")]
        public async Task ForbiddenPlanet_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetBleachManga.txt")));
        }

        [Test, Description("Test Manga Series w/ Box Set in Dif Formats, Color Editions, & Dif Omnibus Types")]
        public async Task ForbiddenPlanet_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack On Titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetAttackOnTitanManga.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        [Ignore("One Shots Not Working")]
        public async Task ForbiddenPlanet_Member_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task ForbiddenPlanet_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetDimensionalSeductionMangaData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task ForbiddenPlanet_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetOverlordMangaData.txt")));
        }

        // Not done, is having issues
        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        [Ignore("Novel Not Working")]
        public async Task ForbiddenPlanet_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetOverlordNovelData.txt")));
        }

        // [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        // public async Task ForbiddenPlanet_07Ghost_Manga_Test()
        // {
        //     await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
        //     Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanet07GhostMangaData.txt")));
        // }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes")]
        public async Task ForbiddenPlanet_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task ForbiddenPlanet_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        public async Task ForbiddenPlanet_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetToiletMangaData.txt")));
        }
        
        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        [Ignore("Novel Not Working")]
        public async Task ForbiddenPlanet_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetCOTENovelData.txt")));
        }

        [Test]
        public async Task ForbiddenPlanet_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetBorutoMangaData.txt")));
        }
    }
}