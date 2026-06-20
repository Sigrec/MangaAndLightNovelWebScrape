# [MangaAndLightNovelWebScrape](https://www.nuget.org/packages/MangaAndLightNovelWebScrape/)

### *(Manga & Light Novel Web Scrape Framework for .NET) - [ChangeLog](https://github.com/Sigrec/MangaAndLightNovelWebScrape/blob/master/ChangeLog.txt)*

.NET library that scrapes various retailers for manga or light-novel pricing,
compares the results across every site you ask for, and hands back the cheapest
copy of each volume. Provider-agnostic logging via `Microsoft.Extensions.Logging`,
per-site failure isolation, cooperative cancellation, and a reachability probe
so you can short-circuit known-down sites before launching a scrape.

***

### Installation

```sh
dotnet add package MangaAndLightNovelWebScrape
```

Sites that require JavaScript rendering (BooksAMillion, KinokuniyaUSA,
ForbiddenPlanet, MerryManga, MangaMart, MangaMate, AmazonUSA, AmazonJP) drive
[Playwright](https://playwright.dev/dotnet/) under the hood. The runtime
downloads browsers on first use; you can also pre-install them via the CLI:

```sh
pwsh bin/Debug/net10.0/playwright.ps1 install msedge
```

HTML-only sites (SciFier, InStockTrades, Crunchyroll, MangaMart, RobertsAnimeCornerStore,
TravellingMan) skip Playwright entirely.

***

### Website Completion List

If you want a website or region to be added, file an [issue request](https://github.com/Sigrec/MangaAndLightNovelWebScrape/issues/new/choose).

#### America

```txt
✅ AmazonUSA
❌ Barnes & Noble (No longer supported — robots.txt change)
✅ Books-A-Million
✅ Crunchyroll
✅ InStockTrades
✅ Kinokuniya USA (Manga entries occasionally drop when the on-page Manga facet excludes them)
✅ MangaMart
✅ MerryManga
✅ RobertsAnimeCornerStore
✅ SciFier
```

#### Australia

```txt
✅ All Star Comics
⌛ AmazonAU (Not Started)
✅ MangaMate
✅ SciFier
```

#### Britain

```txt
⌛ AmazonUK (Not Started)
✅ ForbiddenPlanet
✅ OK Comics
✅ SciFier
❌ SpeedyHen (No longer supported — Cloudflare CAPTCHA)
✅ TravellingMan
❌ Waterstones (No longer supported — Cloudflare CAPTCHA)
```

#### Canada

```txt
⌛ AmazonCanada (Not Started)
❌ Indigo (Not Working)
✅ SciFier
```

#### Europe

```txt
✅ SciFier
```

#### Japan

```txt
⌛ AmazonJP (Not Started)
⌛ CDJapan (Paused)
```

***

### Creating Master Scrape

`MasterScrape` is designed to be **constructed once and reused** across many
scrape runs — `InitializeScrapeAsync` clears all per-run state at the top of
the call (results, URL map, error map). Allocating a fresh instance per call
just wastes the constructor's work.

Defaults: `Region.America`, `Browser.Edge`, `Membership.None`, debug mode off.

```cs
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;

// Minimal — only the stock-status filter is required.
MasterScrape scrape = new(StockStatusFilter.EXCLUDE_NONE_FILTER);

// You can also mutate the properties after construction.
scrape.Region = Region.Canada;
scrape.Browser = Browser.Firefox;
scrape.Filter = StockStatusFilter.EXCLUDE_OOS_AND_PO_FILTER;
// Memberships is a [Flags] enum — combine sites with `|`.
scrape.Memberships = Membership.BooksAMillion | Membership.KinokuniyaUSA;
```

Alternatively, set everything in the constructor. Multi-region scrapes (e.g.
`Region.America | Region.Britain`) are not supported — one region per scrape.

```cs
MasterScrape scrape = new(
    Filter: StockStatusFilter.EXCLUDE_NONE_FILTER,
    Region: Region.Britain,
    Browser: Browser.Edge,
    Memberships: Membership.BooksAMillion | Membership.KinokuniyaUSA);
```

#### Logging

The library writes through `Microsoft.Extensions.Logging.Abstractions` — pass
an `ILoggerFactory` (Serilog, NLog, the console provider, your own) to the
constructor and the scrape will emit per-site events to it. When no factory
is supplied, logging is a no-op.

```cs
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b
    .SetMinimumLevel(LogLevel.Information)
    .AddSimpleConsole());

MasterScrape scrape = new(
    Filter: StockStatusFilter.EXCLUDE_NONE_FILTER,
    Region: Region.America,
    loggerFactory: loggerFactory);
```

Calling `.EnableDebugMode()` additionally writes per-site result dumps to
`<cwd>/Data/<Site>Data.txt` — useful for local diagnosis.

***

### Initializing a Scrape

Pass a `HashSet<Website>` of sites to scrape. The optional `CancellationToken`
lets you cancel between major phases (site fan-out, merge rounds).

```cs
await scrape.InitializeScrapeAsync(
    title: "one piece",
    bookType: BookType.Manga,
    siteList: [Website.RobertsAnimeCornerStore, Website.Crunchyroll]);
```

```cs
// With cancellation
using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
await scrape.InitializeScrapeAsync(
    title: "overlord",
    bookType: BookType.LightNovel,
    siteList: [Website.SciFier, Website.BooksAMillion],
    cancellationToken: cts.Token);
```

***

### Getting Results

```cs
IReadOnlyList<EntryModel> results = scrape.GetResults();
IReadOnlyDictionary<Website, string> urls = scrape.GetResultUrls();
```

Three convenience renderers ship with the library:

```cs
// To console
scrape.PrintResultsToConsole(isAsciiTable: true, title: "world trigger", bookType: BookType.Manga);

// To a logger at a chosen level
logger.PrintResults(scrape, LogLevel.Information, isAsciiTable: true,
    title: "world trigger", bookType: BookType.Manga);

// To a file
scrape.PrintResultsToFile("FinalData.txt", isAsciiTable: true,
    title: "world trigger", bookType: BookType.Manga);
```

ASCII-table output:

```txt
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

***

### Error Handling

A single site failing never aborts the rest of the scrape — per-site failures
land in `scrape.Errors` (`IReadOnlyDictionary<Website, Exception>`). Catastrophic
failures (Playwright browser launch, cancellation) propagate as thrown
exceptions.

```cs
try
{
    await scrape.InitializeScrapeAsync(title, bookType, sites, ct);
}
catch (OperationCanceledException)
{
    // User cancelled — re-throw or surface as cancelled.
    throw;
}
catch (ScrapeBrowserLaunchException ex)
{
    // Host setup problem — no Playwright sites can run until fixed.
    logger.LogCritical(ex, "Playwright browser failed to launch");
}
catch (ArgumentException ex)
{
    // Bad input (empty title, sites not valid for the current region).
}

foreach ((Website site, Exception ex) in scrape.Errors)
{
    logger.LogWarning(ex, "{Site} failed for {Title}", site, title);
}
```

Exception hierarchy:

| Type | When |
|---|---|
| `ScrapeException` | Abstract base — catch this to handle every library failure. |
| `ScrapeBrowserLaunchException` | Playwright browser couldn't launch. **Thrown** from `InitializeScrapeAsync`. |
| `SiteScrapeException` | A single site's scrape threw. **Recorded** in `Errors`, never thrown. |

Full contract details live in
[`docs/ErrorHandling.md`](https://github.com/Sigrec/MangaAndLightNovelWebScrape/blob/master/docs/ErrorHandling.md).

***

### Reachability Probes

Sites occasionally go down (DNS issues, maintenance, CDN outages). Use
`IsSiteAvailableAsync` / `CheckSitesAvailableAsync` to short-circuit known-down
sites instead of waiting on the scrape's per-site timeout.

```cs
// Single site
bool up = await scrape.IsSiteAvailableAsync(Website.MangaMate);

// Many at once (runs in parallel)
IReadOnlyDictionary<Website, bool> status = await scrape.CheckSitesAvailableAsync([
    Website.Crunchyroll, Website.MangaMate, Website.SciFier
]);

HashSet<Website> reachable = [.. status.Where(p => p.Value).Select(p => p.Key)];
if (reachable.Count > 0)
{
    await scrape.InitializeScrapeAsync(title, BookType.Manga, reachable);
}
```

Returns `true` when the host resolves and the server answers — including
auth-gated (401/403) and CDN-challenged (Cloudflare) responses. DNS failures,
connection refusals, timeouts, and 5xx returns `false`.

#### Auto-Skipping Unavailable Sites

Set `SkipUnavailableSites = true` to have `InitializeScrapeAsync` run the
pre-flight check itself and silently drop any site that doesn't respond.
Skipped sites are still recorded in `Errors` as `SiteUnavailableException`
so the caller can see what was omitted without writing the filter manually.

```cs
scrape.SkipUnavailableSites = true;

await scrape.InitializeScrapeAsync(title, BookType.Manga, [
    Website.Crunchyroll, Website.MangaMate, Website.SciFier
]);

foreach ((Website site, Exception ex) in scrape.Errors)
{
    if (ex is SiteUnavailableException)
    {
        logger.LogInformation("{Site} was skipped (unreachable)", site);
    }
}
```
