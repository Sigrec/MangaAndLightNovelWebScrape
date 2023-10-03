namespace Tests.Websites
{
    [TestFixture, Description("Validations for RightStufAnime")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class RightStufAnimeTests
    {
        MasterScrape Scrape;
        List<Website> WebsiteList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.America, Browser.Chrome);
            WebsiteList = new List<Website>() {Website.RightStufAnime};
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task RightStufAnime_OnePiece_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeOnePieceMangaData.txt")));
        }

        [Test]
        public async Task RightStufAnime_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeNarutoMangaData.txt")));
        }

        [Test]
        public async Task RightStufAnime_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeNarutoNovelData.txt")));
        }

        [Test]
        public async Task RightStufAnime_Bleach_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeBleachMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task RightStufAnime_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeCOTENovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public async Task RightStufAnime_COTE_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeCOTEMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task RightStufAnime_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task RightStufAnime_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public async Task RightStufAnime_07Ghost_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("07-ghost", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnime07GhostMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task RightStufAnime_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes & Imperfect Volume")]
        public async Task RightStufAnime_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.LightNovel, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeFMABMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task RightStufAnime_Member_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\America\Data\RightStufAnime\RightStufAnimeGoodbyeEriMangaData.txt")));
        }
    }
}