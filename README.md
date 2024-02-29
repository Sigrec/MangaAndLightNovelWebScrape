# [MangaAndLightNovelWebScrape](https://www.nuget.org/packages/MangaAndLightNovelWebScrape/2.0.0#readme-body-tab)
### *(Manga & Light Novel Web Scrape Framework for .NET) - [ChangeLog](https://github.com/Sigrec/MangaAndLightNovelWebScrape/blob/master/ChangeLog.txt)*
.NET Framework that scrapes various websites for manga or light novel data for a specifc user inputted series. Then it compares the various prices for each available volume across the websites chosen and outputs a list of the volumes available and the website and price for the lowest volume.
***
### *Website Completion List*
If you want more websites just notify me and I will look into seeing if I can add them
#### America
- [ ] AmazonUSA (Paused)
- [x] Barnes & Noble
- [x] Books-A-Million
- [x] Crunchyroll
- [x] InStockTrades
- [x] Kinokuniya USA
- [x] MerryManga
- [x] RobertsAnimeCornerStore
- [x] SciFier
- [x] Wordery

#### Australia
- [ ] AmazonAU (Not Started)
- [x] MangaMate
- [x] SciFier
- [x] Wordery

#### Britain
- [ ] AmazonUK (Not Started)
- [ ] ForBiddenPlanet (Paused)
- [x] SciFier
- [x] SpeedyHen
- [x] Waterstones
- [x] Wordery

#### Canada
- [ ] AmazonCanada (Not Started)
- [x] Indigo
- [x] SciFier
- [x] Wordery

#### Europe
- [x] SciFier
- [x] Wordery

#### Japan
- [ ] AmazonJP (Not Started)
- [ ] CDJapan (Paused)
 
***
#### Demo
```cs
private static async Task Main(string[] args)
{
    // Create the MasterScrape object it defaults to America Region, Chrome Browser, & all memberships are default false (it is better to set them), but you can still change them outside of the constructor & debug mode is disabled by default. There is no default StockStatusFilter
    MasterScrape Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
    scrape.Region = Region.Canada;
    scrape.Browser = Browser.FireFox;
    scrape.Filter = StockstatusFilter.EXCLUDE_OOS_AND_PO_FILTER;
    scrape.IsBarnesAndNobleMember = true;
    scrape.IsBooksAMillionMember = false;
    scrape.IsKinokuniyaUSAMember = true;
    scrape.IsIndigoMember = false;

    // Alternativly you can do everything in the constructor and enable debug mode which will print to log and txt files
    // Chaining Regions like so Region.America | Region.Britain will not work
    MasterScrape Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, StockStatusFilter.EXCLUDE_ALL_FILTER, Region.Britain, Browser.Edge, false, false, false, true).EnableDebugMode();

    // Initialize the Scrape
    await scrape.InitializeScrapeAsync(
        "one piece", // Title
        BookType.Manga, // BookType
        scrape.GenerateWebsiteList(new List<string>() { InStockTrades.WEBSITE_TITLE }), // Website List
    );

    // Get Final data Results
    List<EntryModel> finalData = scrape.GetResults();
    Dictionary<string, string> finalUrls = scrape.GetResultsUrls();

    // Print final result data either to console, logger, or file (can be printed in a ascii table format)
    scrape.PrintResultsToConsole(true, "world trigger", BookType.Manga);
    scrape.PrintResultToLogger(LOGGER, NLog.LogLevel.Info);
    scrape.PrintResultsToFile("FinalData.txt");

    // Example AsciiTable Format Output
    Title: "world trigger"
    BookType: Manga
    Region: America
    ┏━━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━┳━━━━━━━━┳━━━━━━━━━━━━━━━━━━━━━━━━━┓
    ┃ Title                ┃ Price ┃ Status ┃ Website                 ┃
    ┣━━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━╋━━━━━━━━╋━━━━━━━━━━━━━━━━━━━━━━━━━┫
    ┃ World Trigger Vol 20 ┃ $8.98 ┃ IS     ┃ RobertsAnimeCornerStore ┃
    ┃ World Trigger Vol 21 ┃ $8.98 ┃ IS     ┃ RobertsAnimeCornerStore ┃
    ┃ World Trigger Vol 22 ┃ $8.98 ┃ IS     ┃ RobertsAnimeCornerStore ┃
    ┃ World Trigger Vol 23 ┃ $8.98 ┃ IS     ┃ RobertsAnimeCornerStore ┃
    ┃ World Trigger Vol 24 ┃ $8.98 ┃ IS     ┃ RobertsAnimeCornerStore ┃
    ┗━━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━┻━━━━━━━━┻━━━━━━━━━━━━━━━━━━━━━━━━━┛
    Links:
    RobertsAnimeCornerStore => https://www.animecornerstore.com/wotrbgrno.html
}
```