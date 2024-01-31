# [MangaAndLightNovelWebScrape](https://www.nuget.org/packages/MangaAndLightNovelWebScrape/1.1.6#readme-body-tab)
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
- [ ] AmazonUSA (Paused)
- [x] Wordery
- [x] SciFier
- [x] MerryManga

#### Canada
- [x] Wordery
- [x] Indigo
- [x] SciFier

#### Britain
- [x] Wordery
- [ ] ForBiddenPlanet (In Progress)
- [x] Waterstones
- [x] SciFier

#### Japan
- [ ] AmazonJP (Not Started)
- [ ] CDJapan (Paused)

#### Europe
- [x] Wordery
- [x] SciFier

#### Australia
- [x] Wordery
- [x] SciFier
- [ ] MangaMate (Not Started)
 
***
#### Demo
```cs
private static async Task Main(string[] args)
{
    // Create the MasterScrape object it defaults to America Region & Chrome Browser but you can still change them outside of the constructor & debug mode is disabled by default. There is no default StockStatusFilter
    MasterScrape Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
    scrape.Region = Region.Canada;
    scrape.Browser = Browser.FireFox;
    scrape.Filter = StockstatusFilter.EXCLUDE_OOS_AND_PO_FILTER;

    // Alternativly you can do everything in the constructor and enable debug mode which will print to log and txt files
    // Chaining Regions like so Region.America | Region.Britain will not work
    MasterScrape Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, StockStatusFilter.EXCLUDE_ALL_FILTER, Region.Britain, Browser.Edge).EnableDebugMode();

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