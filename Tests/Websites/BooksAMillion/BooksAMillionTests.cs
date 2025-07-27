namespace Tests.Websites
{
    [TestFixture, Description("Validations for BooksAMillion")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class BooksAMillionTests
    {
        private MasterScrape Scrape;
        private static readonly HashSet<Website> WebsiteList = [Website.BooksAMillion];

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Scrape = null;
        }

        private static readonly object[] ScrapeTestCases =
        [
            new object[] { "Akane-Banashi", BookType.Manga, "AkaneBanashiMangaData", false, false },
            new object[] { "jujutsu kaisen", BookType.Manga, "JujutsuKaisenMangaData", false, false },
            new object[] { "Dragon Quest: The Adventure of Dai", BookType.Manga, "AdventuresOfDaiMangaData", false, false },
            new object[] { "One Piece", BookType.Manga, "OnePieceMangaData", false, false },
            new object[] { "Goodbye, Eri", BookType.Manga, "GoodbyeEriMangaData", false, true },
            new object[] { "Naruto", BookType.Manga, "NarutoMangaData", false, false },
            new object[] { "Naruto", BookType.LightNovel, "NarutoNovelData", false, false },
            new object[] { "Bleach", BookType.Manga, "BleachMangaData", false, false },
            new object[] { "Attack on Titan", BookType.Manga, "AttackOnTitanMangaData", false, false },
            new object[] { "Overlord", BookType.LightNovel, "OverlordNovelData", false, false },
            new object[] { "Overlord", BookType.Manga, "OverlordMangaData", false, false },
            new object[] { "Fullmetal Alchemist", BookType.Manga, "FMABMangaData", false, false },
            new object[] { "Berserk", BookType.Manga, "BerserkMangaData", false, false },
            new object[] { "Toilet-bound Hanako-kun", BookType.Manga, "ToiletMangaData", false, false },
            new object[] { "classroom of the elite", BookType.LightNovel, "COTENovelData", false, false },
            new object[] { "Boruto", BookType.Manga, "BorutoMangaData", false, false },
            new object[] { "Noragami", BookType.Manga, "NoragamiMangaData", false, false }
        ];

        [TestCaseSource(nameof(ScrapeTestCases))]
        public async Task BooksAMillion_Scrape_Test(string title, BookType bookType, string expectedFilePath, bool skip, bool isMember)
        {
            if (skip)
            {
                Assert.Ignore($"Test skipped: '{title}'");
                return;
            }

            Scrape.IsBooksAMillionMember = isMember;
            await Scrape.InitializeScrapeAsync(title, bookType, WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList($@"C:\MangaAndLightNovelWebScrape\Tests\Websites\BooksAMillion\BooksAMillion{expectedFilePath}.txt")));
        }
        
        [Test]
        public void RegionValidation_Test()
        {
            Assert.That(
                MangaAndLightNovelWebScrape.Websites.BooksAMillion.REGION.HasFlag(Region.America) && !MangaAndLightNovelWebScrape.Websites.BooksAMillion.REGION.HasFlag(Region.Australia) && !MangaAndLightNovelWebScrape.Websites.BooksAMillion.REGION.HasFlag(Region.Britain) && !MangaAndLightNovelWebScrape.Websites.BooksAMillion.REGION.HasFlag(Region.Canada) && !MangaAndLightNovelWebScrape.Websites.BooksAMillion.REGION.HasFlag(Region.Europe) && !MangaAndLightNovelWebScrape.Websites.BooksAMillion.REGION.HasFlag(Region.Japan)
            );
        }
    }
}