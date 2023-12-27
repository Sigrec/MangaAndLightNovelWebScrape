# [MangaAndLightNovelWebScrape](https://www.nuget.org/packages/MangaAndLightNovelWebScrape/1.1.3#versions-body-tab)
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
- [ ] AmazonUSA (In Progress)
- [x] SciFier
- [x] MerryManga

#### Canada
- [x] Indigo
- [x] SciFier

#### Britain
- [ ] ForBiddenPlanet (In Progress)
- [x] Waterstones
- [x] SciFier

#### Japan
- [ ] AmazonJP
- [ ] CDJapan (In Progress)

#### Europe
- [x] SciFier
 
***
#### Demo
```cs
private static async Task Main(string[] args)
{
    // Create the MasterScrape object it defaults to America Region & Chrome Browser but you can still change them outside of the constructor & debug mode is disabled by default. There is no default StockStatusFilter
    MasterScrape scrape = new MasterScrape(StockStatusFilter.EXCLUDE_OOS_FILTER);
    scrape.Region = Region.America;
    scrape.Browser = Browser.FireFox;

    // Alternativly you can do everything in the constructor and enable debug mode which will print to log and txt files
    // Chaining Regions like so Region.America | Region.Britain will not work
    MasterScrape scrape = new MasterScrape(StockStatusFilter.EXCLUDE_ALL_FILTER, Region.Britain, Browser.Edge).EnableDebugMode();

    // Initialize the Scrape
    await scrape.InitializeScrapeAsync(
        "one piece", // Title
        BookType.Manga, // BookType
        scrape.GenerateWebsiteList(new List<string>() { InStockTrades.WEBSITE_TITLE }), // Website List
        false, // isBarnesAndNobleMember
        false, // isBooksAMillionMember
        false, // isKinokuniyaUSAMember
        false // isIndigoMember
    );

    // Get Final data Results
    List<EntryModel> finalData = scrape.GetResults();
    Dictionary<string, string> finalUrls = scrape.GetResultsUrls();

    // Print final result data either to console, logger, or file
    scrape.PrintResultsToConsole();
    scrape.PrintResultToLogger(LOGGER, NLog.LogLevel.Info);
    scrape.PrintResultsToFile("FinalData.txt");
}
```