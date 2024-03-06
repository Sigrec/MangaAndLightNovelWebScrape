namespace Tests.Websites
{
    public class TravellingManTests
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList = new HashSet<Website>() {Website.TravellingMan};
    
        [SetUp]
        public void Setup()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.Britain);
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task TravellingMan_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public async Task TravellingMan_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Jujutsu Kaisen", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':', and has 'Vol.'")]
        public async Task TravellingMan_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Tests series w/ Box Sets, Omnibus, & Novel Entry")]
        public async Task TravellingMan_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManOnePieceMangaData.txt")));
        }

        [Test, Description("Remove statue & figurine entries & 3-in-1 omni formatting")]
        public async Task TravellingMan_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManNarutoMangaData.txt")));
        }

        [Test, Description("Series that is manga original & adding 'novel' to LN entries if missing")]
        public async Task TravellingMan_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManNarutoNovelData.txt")));
        }

        [Test, Description("Series w/ 2-in-1 Omni & Anniversary Edition Vol")]
        public async Task TravellingMan_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManBleachMangaData.txt")));
        }

        [Test, Description("Manga Series w/ Omnibus & Box Sets in Dif Formats, Color Editions, Hardcover, & Nendoroids")]
        public async Task TravellingMan_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack on Titan", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Test One Shot Series")]
        public async Task TravellingMan_Member_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ number & '.' in book title")]
        [Ignore("Does not Exist at TravellingMan")]
        public async Task TravellingMan_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManDimensionalSeductionMangaData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries")]
        public async Task TravellingMan_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManOverlordMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Light Novel in title & Miniature entry")]
        public async Task TravellingMan_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManOverlordNovelData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public async Task TravellingMan_07Ghost_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingMan07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes, Unique Omni, Box Set w/out Number, & Four Coma")]
        public async Task TravellingMan_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Editions & Removal of Series w/ same book title")]
        public async Task TravellingMan_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        [Ignore("Not Needed")]
        public async Task TravellingMan_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManToiletMangaData.txt")));
        }

        [Test, Description("Validates Series with 'Manga' in entry")]
        public async Task TravellingMan_COTE_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManCOTEMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task TravellingMan_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManCOTENovelData.txt")));
        }

        [Test]
        public async Task TravellingMan_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\TravellingMan\TravellingManBorutoMangaData.txt")));
        }
    }
}