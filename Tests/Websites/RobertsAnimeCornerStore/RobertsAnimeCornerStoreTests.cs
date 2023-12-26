namespace Tests.Websites
{
    [TestFixture]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class RobertsAnimeCornerStoreTests
    {
        MasterScrape Scrape;
        HashSet<Website> WebsiteList;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.America, Browser.Chrome);
            WebsiteList = new HashSet<Website>() {Website.RobertsAnimeCornerStore};
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public async Task RobertsAnimeCornerStore_AkaneBanashi_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Akane-Banashi", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Vol 0")]
        public async Task RobertsAnimeCornerStore_JujutsuKaisen_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("jujutsu kaisen", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Test Title that contains a keyword to skip and contains ':'")]
        public async Task RobertsAnimeCornerStore_AdventuresOfDai_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Dragon Quest: The Adventure of Dai", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAdventuresOfDaiMangaData.txt")));
        }

        [Test, Description("Validates series with multiple loads, box sets, & omnibus editions & contains Backorder volumes")]
        public async Task RobertsAnimeCornerStore_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("one piece", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOnePieceMangaData.txt")));
        }

        [Test]
        public async Task RobertsAnimeCornerStore_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreNarutoMangaData.txt")));
        }

        [Test, Description("Validates Novel series w/ manga original source & entries without vol #'s")]
        public async Task RobertsAnimeCornerStore_Naruto_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.LightNovel, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreNarutoNovelData.txt")));
        }

        [Test]
        public async Task RobertsAnimeCornerStore_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBleachMangaData.txt")));
        }

        [Test, Description("Test Manga Series w/ Box Set & Omnibus in Dif Formats & Color Editions")]
        public async Task RobertsAnimeCornerStore_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack on Titan", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAttackOnTitanMangaData.txt")));
        }

        [Test, Description("Validates One Shot Manga Series")]
        public async Task RobertsAnimeCornerStore_GoodbyeEri_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Goodbye, Eri", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreGoodbyeEriMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number & Non Letter Character")]
        public async Task RobertsAnimeCornerStore_DimensionalSeduction_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("2.5 Dimensional Seduction", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        public async Task RobertsAnimeCornerStore_Overlord_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("Overlord", BookType.LightNovel, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOverlordNovelData.txt")));
        }

        // Currently there A La Carte are under Subject Comic and not Manga so theyh aren't grabbed
        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public async Task RobertsAnimeCornerStore_Overlord_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("overlord", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOverlordMangaData.txt")));
        }

        [Test, Description("Validates Series w/ '-' in book title & special edition Volume")]
        public async Task RobertsAnimeCornerStore_AntiRomance_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Anti-Romance", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAntiRomanceMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Special Edition & Paperback Volumes")]
        public async Task RobertsAnimeCornerStore_FMAB_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("fullmetal alchemist", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public async Task RobertsAnimeCornerStore_Berserk_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Berserk", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set with Specific name")]
        public async Task RobertsAnimeCornerStore_ToiletBound_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Toilet-bound Hanako-kun", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreToiletMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public async Task RobertsAnimeCornerStore_COTE_Novel_Test()
        {
            await Scrape.InitializeScrapeAsync("classroom of the elite", BookType.LightNovel, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreCOTENovelData.txt")));
        }

        [Test, Description("Ensure consistency")]
        public async Task RobertsAnimeCornerStore_Boruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Boruto", BookType.Manga, MasterScrape.EXCLUDE_NONE_FILTER, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaAndLightNovelWebScrape\Tests\Websites\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBorutoMangaData.txt")));
        }
    }
}