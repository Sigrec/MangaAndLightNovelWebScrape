namespace Tests.Websites
{
    public class CDJapanTests
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList;
    
        [SetUp]
        public void Setup()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.Japan, Browser.Chrome);
            WebsiteList = new HashSet<Website>() {Website.CDJapan};
        }

        [Test, Description("Test Manga Series w/ Box Set & Omnibus in Dif Formats & Color Editions")]
        public async Task CDJapan_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("進撃の巨人", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public async Task CDJapan_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("ONE PIECE", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanOnePieceMangaData.txt")));
        }

        [Test, Description("Tests series that has both roman and japanese characters in the title")]
        public async Task CDJapan_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("NARUTO -ナルト-", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanNarutoMangaData.txt")));
        }

        [Test]
        public async Task CDJapan_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanBorutoMangaData.txt")));
        }

        [Test]
        public async Task CDJapan_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("NARUTO -ナルト-", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanNarutoNovelData.txt")));
        }

        [Test]
        public async Task CDJapan_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("BLEACH", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanBleachMangaData.txt")));
        }

        [Test, Description("Validates Manga w/ Based on Series that is a Novel")]
        public async Task CDJapan_COTE_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("ようこそ実力至上主義の教室へ", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanCOTEMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task CDJapan_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("ようこそ実力至上主義の教室へ", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanCOTENovelData.txt")));
        }

        // Not done, is having issues
        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        public async Task CDJapan_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("オーバーロード", BookType.LightNovel, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanOverlordNovelData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task CDJapan_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("オーバーロード", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public async Task CDJapan_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public async Task CDJapan_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("呪術廻戦", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes")]
        public async Task CDJapan_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("鋼の錬金術師", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task CDJapan_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        public async Task CDJapan_Toilet_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("地縛少年 花子くん", BookType.Manga, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanToiletMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task CDJapan_Member_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("さよなら絵梨", BookType.Manga, WebsiteList, true);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\CDJapan\CDJapanGoodbyeEriMangaData.txt")));
        }
    }
}