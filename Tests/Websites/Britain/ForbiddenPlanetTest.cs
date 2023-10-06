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
            await Scrape.InitializeScrapeAsync("OnePiece", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Britain\Data\ForbiddenPlanet\ForbiddenPlanetOnePieceManga.txt")));
        }

        [Test, Description("Test Manga Series w/ Omnibus of Dif Text Format & Light Novels")]
        public async Task ForbiddenPlanet_Naruto_Manga_Test()
        {
            await Scrape.InitializeScrapeAsync("Naruto", BookType.Manga, Array.Empty<StockStatus>(), WebsiteList);
            Assert.That(Scrape.GetResults(), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Britain\Data\ForbiddenPlanet\ForbiddenPlanetNarutoManga.txt")));
        }
    }
}