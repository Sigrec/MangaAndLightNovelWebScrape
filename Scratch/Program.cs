using System.Diagnostics;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Models;
using Microsoft.Extensions.Logging;
#if DEBUG
using Scratch;
#endif

// Local smoke test harness for the library. Edit and `dotnet run --project Scratch`
// to exercise a single-site scrape end-to-end without going through the test suite.
//
// Logging policy:
//   * Debug builds: console + per-site log files under <output>/Logs/{Site}_Logs.log.
//   * Release builds: console only; no file IO. Consumers of the NuGet ship their own
//     provider, so the file-logger classes are #if DEBUG-gated and don't appear in
//     Release binaries.

#if DEBUG
// Resolve relative to the current working directory (where you invoked `dotnet run` from),
// not AppContext.BaseDirectory (which would bury logs under bin/Debug/net10.0/). Matches the
// old NLog setup's `${CurrentDir}/Logs/` convention.
string logsDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
Console.WriteLine($"[Scratch] Debug log files: {logsDir}");
#endif

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss.fff "; });

#if DEBUG
    builder.AddProvider(new FileLoggerProvider(logsDir));
#endif
});

Stopwatch stopwatch = Stopwatch.StartNew();

MasterScrape scrape = new MasterScrape(
    Filter: StockStatusFilter.EXCLUDE_NONE_FILTER,
    Region: Region.America,
    Browser: Browser.Edge,
    loggerFactory: loggerFactory)
    .EnableDebugMode();

const string title = "jujutsu kaisen";
const BookType bookType = BookType.Manga;

await scrape.InitializeScrapeAsync(
    title: title,
    bookType: bookType,
    siteList: [Website.Crunchyroll]);

stopwatch.Stop();

scrape.PrintResultsToConsole(
    isAsciiTable: true,
    title: title,
    bookType: bookType);

Console.WriteLine($"Elapsed time: {stopwatch.Elapsed.TotalSeconds:F3} seconds");
