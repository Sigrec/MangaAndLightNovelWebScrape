# [MangaAndLightNovelWebScrape](https://www.nuget.org/packages/MangaAndLightNovelWebScrape/4.0.2#readme-body-tab)
### *(Manga & Light Novel Web Scrape Framework for .NET) - [ChangeLog](https://github.com/Sigrec/MangaAndLightNovelWebScrape/blob/master/ChangeLog.txt)*
.NET Library that scrapes various websites based on a region for manga or light novel data for a specifc user inputted series. Then it compares the various prices for each available entry across the websites chosen and outputs a list of the entries available and the website and price for the cheapest entry.
***
### *Website Completion List*
If you want a website or region to be added fill out a [issue request](https://github.com/Sigrec/MangaAndLightNovelWebScrape/issues/new/choose).

##### America
```
✅ AmazonUSA
❌ Barnes & Noble (No longer supported due to robots.txt change) 
✅ Books-A-Million
✅ Crunchyroll
✅ InStockTrades
✅ Kinokuniya USA (Sometimes Manga entries will be left out because when going to the manga tab it leaves some out)
✅ MerryManga
✅ RobertsAnimeCornerStore
✅ SciFier
```
##### Australia
```
⌛ AmazonAU (Not Started)
✅ MangaMate
✅ SciFier
```

##### Britain
```
⌛ AmazonUK (Not Started)
✅ ForbiddenPlanet
✅ SciFier
❌ SpeedyHen (No longer supported due to Cloudflare Captcha) 
✅ TravellingMan
✅ Waterstones
```

##### Canada
```
⌛ AmazonCanada (Not Started)
❌ Indigo (Not Working)
✅ SciFier
```

##### Europe
```
✅ SciFier
```

##### Japan
```
⌛ AmazonJP (Not Started)
⌛ CDJapan (Paused)
```
 
***
#### Demo
```cs
// Create the MasterScrape object it defaults to America Region, Chrome Browser, 
// & all memberships are default false (it is better to set them), 
// but you can still change them outside of the constructor & debug mode is disabled by default.
// There is no default StockStatusFilter
MasterScrape Scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER);
scrape.Region = Region.Canada;
scrape.Browser = Browser.FireFox;
scrape.Filter = StockstatusFilter.EXCLUDE_OOS_AND_PO_FILTER;
scrape.IsBooksAMillionMember = false;
scrape.IsKinokuniyaUSAMember = true;
scrape.IsIndigoMember = false;

// Alternativly you can do everything in the constructor 
// Chaining Regions like so "Region.America | Region.Britain" 
//will not work you can only scrape against one region at a time
MasterScrape Scrape = new MasterScrape(
    Filter: StockStatusFilter.EXCLUDE_NONE_FILTER, 
    Region: Region.Britain, 
    Browser: Browser.Edge, 
    IsBooksAMillionMemnber: false, 
    IsKinokuniyaUSAMember: false, 
    IsIndigoMember: true
);

// You can enable debug mode which will log to files if you have the NLog file
// You can enable persistent webdriver, which will prevent a webdriver from being created and disposed 
// for every website where a WebDriver is needed. This could possibley lead to memory issues 
// it might be a good idea to perform some cleanup for browser executables in the end
MasterScrape Scrape = new MasterScrape(
    Filter: StockStatusFilter.EXCLUDE_NONE_FILTER, 
    Region: Region.Britain, 
    Browser: Browser.Edge, 
    IsBooksAMillionMemnber: false, 
    IsKinokuniyaUSAMember: false, 
    IsIndigoMember: true
)
.EnableDebugMode()
.EnablePersistentWebDriver();
```
##### Initializating Scrape
```cs
// Initialize the Scrape using strings
await scrape.InitializeScrapeAsync(
    title: "one piece",
    bookType: BookType.Manga,
    scrape.GenerateWebsiteList([ RobertsAnimeCornerStore.WEBSITE_TITLE, Crunchyroll.WEBSITE_TITLE ]),
);

// Initialize the Scrape using enums
await scrape.InitializeScrapeAsync(
    title: "one piece",
    bookType: BookType.Manga,
    [ Website.RobertsAnimeCornerStore, Website.Crunchyroll ],
);

// Initialize the Scrape using params
await scrape.InitializeScrapeAsync(
    title: "one piece",
    bookType: BookType.Manga,
    Website.RobertsAnimeCornerStore,
    Website.Crunchyroll,
);
```
##### Getting Results
```cs
// Get Final data Results
List<EntryModel> resultData = scrape.GetResults();
Dictionary<string, string> resultUrls = scrape.GetResultsUrls();

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
```