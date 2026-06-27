using System.Diagnostics;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Models;
using Microsoft.Extensions.Logging;
#if DEBUG
using Scratch;
#endif

// Local smoke test harness for the library.
//
//   dotnet run --project Scratch -- <site[,site,...]> <title> <bookType> <region>
//
// Examples:
//   dotnet run --project Scratch -- ForbiddenPlanet "jujutsu kaisen" Manga America
//   dotnet run --project Scratch -- Crunchyroll,SciFier overlord LightNovel America
//
// Logging policy:
//   * Debug builds: console + per-site log files under <output>/Logs/{Site}_Logs.log.
//   * Release builds: console only; no file IO. Consumers of the NuGet ship their own
//     provider, so the file-logger classes are #if DEBUG-gated and don't appear in
//     Release binaries.

if (args.Length != 4)
{
    PrintUsage();
    return 1;
}

HashSet<Website> siteList = [];
foreach (string raw in args[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
{
    if (!Enum.TryParse(raw, ignoreCase: true, out Website site))
    {
        Console.Error.WriteLine($"Invalid site: '{raw}'. Valid: {string.Join(", ", Enum.GetNames<Website>())}");
        return 1;
    }
    siteList.Add(site);
}

string title = args[1];
if (string.IsNullOrWhiteSpace(title))
{
    Console.Error.WriteLine("Title cannot be empty.");
    return 1;
}

if (!Enum.TryParse(args[2], ignoreCase: true, out BookType bookType))
{
    Console.Error.WriteLine($"Invalid bookType: '{args[2]}'. Valid: {string.Join(", ", Enum.GetNames<BookType>())}");
    return 1;
}

if (!Enum.TryParse(args[3], ignoreCase: true, out Region region))
{
    Console.Error.WriteLine($"Invalid region: '{args[3]}'. Valid: {string.Join(", ", Enum.GetNames<Region>())}");
    return 1;
}

#if DEBUG
// Resolve relative to the current working directory (where you invoked `dotnet run` from),
// not AppContext.BaseDirectory (which would bury logs under bin/Debug/net10.0/). Matches the
// old NLog setup's `${CurrentDir}/Logs/` convention.
string logsDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
Console.WriteLine($"[Scratch] Debug log files: {logsDir}");
Console.WriteLine($"[Scratch] Per-site data:   {dataDir}");
#endif

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);

#if DEBUG
    // Local runs: file logger only. Console stays quiet so the results table is the
    // only thing that lands in stdout.
    builder.AddProvider(new FileLoggerProvider(logsDir));
#else
    builder.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss.fff "; });
#endif
});

Stopwatch stopwatch = Stopwatch.StartNew();

MasterScrape scrape = new MasterScrape(
    Filter: StockStatusFilter.EXCLUDE_NONE_FILTER,
    Region: region,
    Browser: Browser.Edge,
    loggerFactory: loggerFactory)
    .EnableDebugMode();

await scrape.InitializeScrapeAsync(
    title: title,
    bookType: bookType,
    siteList: siteList);

stopwatch.Stop();

// Surface per-site failures so silent swallows can't hide a broken scrape.
if (scrape.Errors.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("[Scratch] Per-site errors:");
    foreach ((Website site, Exception ex) in scrape.Errors)
    {
        Console.WriteLine($"  {site}: {ex.Message}");
        if (ex.InnerException is not null)
        {
            Console.WriteLine($"    └─ {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }
}

#if !DEBUG
// Release: print the table straight to console for CLI users.
// (Debug runs read per-site results from Data/<website>Data.txt instead.)
scrape.PrintResultsToConsole(
    isAsciiTable: true,
    title: title,
    bookType: bookType);
#endif

Console.WriteLine($"Elapsed time: {stopwatch.Elapsed.TotalSeconds:F3} seconds");
return 0;

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: dotnet run --project Scratch -- <site[,site,...]> <title> <bookType> <region>");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  site      One or more comma-separated Website enum names.");
    Console.Error.WriteLine($"            Valid: {string.Join(", ", Enum.GetNames<Website>())}");
    Console.Error.WriteLine("  title     Quoted series title, e.g. \"jujutsu kaisen\".");
    Console.Error.WriteLine($"  bookType  {string.Join(" | ", Enum.GetNames<BookType>())}");
    Console.Error.WriteLine($"  region    {string.Join(" | ", Enum.GetNames<Region>())}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Example:");
    Console.Error.WriteLine("  dotnet run --project Scratch -- ForbiddenPlanet \"jujutsu kaisen\" Manga America");
}
