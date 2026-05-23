using System.Diagnostics;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Models;

// Local smoke test harness for the library. Edit and `dotnet run --project Scratch`
// to exercise a single-site scrape end-to-end without going through the test suite.

Stopwatch stopwatch = Stopwatch.StartNew();

MasterScrape scrape = new MasterScrape(
    Filter: StockStatusFilter.EXCLUDE_NONE_FILTER,
    Region: Region.America,
    Browser: Browser.Edge)
    .EnableDebugMode();

const string title = "jujutsu kaisen";
const BookType bookType = BookType.Manga;

await scrape.InitializeScrapeAsync(
    title: title,
    bookType: bookType,
    siteList: [Website.SciFier, Website.RobertsAnimeCornerStore, Website.InStockTrades]);

stopwatch.Stop();

scrape.PrintResultsToConsole(
    isAsciiTable: true,
    title: title,
    bookType: bookType);

Console.WriteLine($"Elapsed time: {stopwatch.Elapsed.TotalSeconds:F3} seconds");
