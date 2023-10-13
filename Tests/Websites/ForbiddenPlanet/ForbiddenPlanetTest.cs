namespace Tests.Websites.Britain
{
    public class ForbiddenPlanetTest
    {
        MasterScrape Scrape;
        List<Website> WebsiteList;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Scrape = new MasterScrape(Region.Britain, Browser.Chrome);
            WebsiteList = new List<Website>() { Website.ForbiddenPlanet };
        }

        [Test, Description("Test Manga Series w/ Omnibus of Dif Text Format & Light Novels")]
        public async Task ForbiddenPlanet_OnePiece_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("One Piece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetOnePieceManga.txt")));
        }

        [Test, Description("Test Manga Series w/ Omnibus of Dif Text Format & Light Novels")]
        public async Task ForbiddenPlanet_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetNarutoManga.txt")));
        }

        [Test, Description("Test Manga Series w/ Box Set w/out Num Indicator")]
        public async Task ForbiddenPlanet_Bleach_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Bleach", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetBleachManga.txt")));
        }

        [Test, Description("Test Manga Series w/ Box Set in Dif Formats, Color Editions, & Dif Omnibus Types")]
        public async Task ForbiddenPlanet_AttackOnTitan_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Attack On Titan", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\ForbiddenPlanet\ForbiddenPlanetAttackOnTitanManga.txt")));
        }
    }
}