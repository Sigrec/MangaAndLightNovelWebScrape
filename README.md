# [MangaAndLightNovelWebScrape](https://www.nuget.org/packages/MangaAndLightNovelWebScrape/1.0.1#readme-body-tab)
### *(Manga & Light Novel Web Scrape Framework for .NET) - [ChangeLog](https://github.com/Sigrec/MangaAndLightNovelWebScrape/blob/master/ChangeLog.txt)*
.NET Framework that scrapes various websites for manga or light novel data for a specifc user inputted series. Then it compares the various prices for each available volume across the websites chosen and outputs a list of the volumes available and the website and price for the lowest volume.
***
### *Website Completion List*
If you want more websites just notify me and I will look into seeing if I can add them
#### America
- [x] Crunchyroll
- [x] RobertsAnimeCornerStore
- [x] Books-A-Million
- [x] Barnes & Noble
- [x] InStockTrades
- [x] Kinokuniya USA
- [ ] AmazonUSA
- [x] SciFier

#### Canada
- [x] Indigo
- [x] SciFier

#### Britain
- [ ] ForBiddenPlanet
- [ ] Waterstones
- [x] SciFier

#### Japan
- [ ] AmazonJP
- [ ] CDJapan

#### Europe
- [x] SciFier
 
***
#### Demo
```cs
private static async Task Main(string[] args)
{
    // Create the MasterScrape object it defaults to America Region and Chrome Browser but can still change them outside of the constructor & debug mode disabled by default
    MasterScrape scrape = new MasterScrape();
    scrape.Region = Region.America;
    scrape.Browser = Browser.FireFox;

    // Alternativly you can do everything in the constructor and enable debug mode which will print to log and txt files
    // Chaining Regions like so Region.America | Region.Britain will not work
    MasterScrape scrape = new MasterScrape(Region.Britain, Browser.Edge).EnableDebugMode();

    // Initialize the Scrape
    await scrape.InitializeScrapeAsync(
        "one piece", // Title
        BookType.Manga, // BookType
        MasterScrape.EXCLUDE_NONE_FILTER, // StockStatus Array
        scrape.GenerateWebsiteList(new List<string>() { InStockTrades.WEBSITE_TITLE }), // Website List
        false, // isBarnesAndNobleMember
        false, // isBooksAMillionMember
        false, // isKinokuniyaUSAMember
        false // isIndigoMember
    );

    // Get final result data
    scrape.GetResults().ForEach(Console.WriteLine);

    // Get final url results
    foreach (KeyValuePair<string, string> url in scrape.GetResultUrls())
    {
        Console.WriteLine($"[{url.Key}, {url.Value}]");
    }
}
```