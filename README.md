# [MangaAndLightNovelWebScrape](https://www.nuget.org/packages/MangaAndLightNovelWebScrape/)

### *(Manga & Light Novel Web Scrape Framework for .NET) - [ChangeLog](https://github.com/Sigrec/MangaAndLightNovelWebScrape/blob/master/ChangeLog.txt)*

.NET 10 library that scrapes manga and light-novel retailers, compares the
results across every site you ask for, and hands back the cheapest copy of each
volume. Stock-aware dedup keeps purchasable copies over cheaper-but-out-of-stock
reprints, per-site failures stay isolated from siblings, every scrape supports
cooperative cancellation, and a reachability probe lets you short-circuit
known-down sites before launching. Provider-agnostic logging through
`Microsoft.Extensions.Logging`.

***

### Installation

```sh
dotnet add package MangaAndLightNovelWebScrape
```

Targets `net10.0`. Sites that require JavaScript rendering drive
[Playwright](https://playwright.dev/dotnet/) under the hood — currently
**AmazonUSA, AmazonJP, BooksAMillion, ForbiddenPlanet, KinokuniyaUSA,
MangaMart, MangaMate, MerryManga**. The runtime downloads browsers on first
use; you can also pre-install them via the CLI:

```sh
pwsh bin/Debug/net10.0/playwright.ps1 install msedge
```

HTML-only sites — **AllStarComics, Crunchyroll, InStockTrades, KingsComics,
OKComics, RobertsAnimeCornerStore, SciFier, TravellingMan** — skip Playwright
entirely, so a scrape that only includes them never launches a browser.

***

### Quick Start

```cs
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;

MasterScrape scrape = new(StockStatusFilter.EXCLUDE_OOS_AND_PO_FILTER)
{
    Region = Region.America,
};

await scrape.InitializeScrapeAsync(
    title: "jujutsu kaisen",
    bookType: BookType.Manga,
    siteList: [Website.InStockTrades, Website.RobertsAnimeCornerStore]);

scrape.PrintResultsToConsole(isAsciiTable: true,
    title: "jujutsu kaisen", bookType: BookType.Manga);
```

The same `MasterScrape` instance can be reused for any number of subsequent
calls — see [Creating Master Scrape](#creating-master-scrape) below.

***

### Website Completion List

Status legend: **✅ Working** · **⌛ Planned / Paused** · **❌ Retired or
non-working**. Render column tells you whether a scrape that only includes
that site needs Playwright. Manga / Light Novel columns reflect what the site
itself stocks — sites that don't stock LightNovel silently return zero
entries for `BookType.LightNovel` (see [Manga-only sites](#manga-only-sites)).

If you want a website or region to be added, file an
[issue request](https://github.com/Sigrec/MangaAndLightNovelWebScrape/issues/new/choose).

#### America

| Site | Status | Render | Manga | Light Novel | Notes |
|---|---|---|---|---|---|
| `AmazonUSA` | ✅ | Playwright | ✅ | ✅ | |
| `BooksAMillion` | ✅ | Playwright | ✅ | ✅ | Membership: `Membership.BooksAMillion` |
| `Crunchyroll` | ✅ | HTML | ✅ | ✅ | |
| `InStockTrades` | ✅ | HTML | ✅ | ✅ | |
| `KinokuniyaUSA` | ✅ | Playwright | ✅ | ✅ | Membership: `Membership.KinokuniyaUSA`. Manga entries occasionally drop when the on-page Manga facet excludes them. |
| `MangaMart` | ✅ | Playwright | ✅ | ✅ | |
| `MerryManga` | ✅ | Playwright | ✅ | ✅ | |
| `RobertsAnimeCornerStore` | ✅ | HTML | ✅ | ✅ | |
| `SciFier` | ✅ | HTML | ✅ | ✅ | Multi-region — same site backs Australia, Britain, Canada, Europe too. |
| Barnes & Noble | ❌ | — | — | — | Retired — robots.txt change. |

#### Australia

| Site | Status | Render | Manga | Light Novel | Notes |
|---|---|---|---|---|---|
| `AllStarComics` | ✅ | HTML | ✅ | ❌ | Manga-only — silently skips LightNovel scrapes. |
| `KingsComics` | ✅ | HTML | ✅ | ❌ | Manga-only — silently skips LightNovel scrapes. |
| `MangaMate` | ✅ | Playwright | ✅ | ✅ | |
| `SciFier` | ✅ | HTML | ✅ | ✅ | |
| AmazonAU | ⌛ | — | — | — | Not started. |

#### Britain

| Site | Status | Render | Manga | Light Novel | Notes |
|---|---|---|---|---|---|
| `ForbiddenPlanet` | ✅ | Playwright | ✅ | ✅ | |
| `OKComics` | ✅ | HTML | ✅ | ❌ | Manga-only — silently skips LightNovel scrapes. |
| `SciFier` | ✅ | HTML | ✅ | ✅ | |
| `TravellingMan` | ✅ | HTML | ✅ | ✅ | |
| AmazonUK | ⌛ | — | — | — | Not started. |
| SpeedyHen | ❌ | — | — | — | Retired — Cloudflare CAPTCHA. |
| Waterstones | ❌ | — | — | — | Retired — Cloudflare CAPTCHA. |

#### Canada

| Site | Status | Render | Manga | Light Novel | Notes |
|---|---|---|---|---|---|
| `SciFier` | ✅ | HTML | ✅ | ✅ | |
| AmazonCanada | ⌛ | — | — | — | Not started. |
| Indigo | ❌ | — | — | — | Not working — pending re-audit. |

#### Europe

| Site | Status | Render | Manga | Light Novel | Notes |
|---|---|---|---|---|---|
| `SciFier` | ✅ | HTML | ✅ | ✅ | |

#### Japan

| Site | Status | Render | Manga | Light Novel | Notes |
|---|---|---|---|---|---|
| AmazonJP | ⌛ | — | — | — | Not started. |
| CDJapan | ⌛ | — | — | — | Paused — partial implementation, not registered. |

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
scrape.Browser = Browser.FireFox;
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

#### Manga-only sites

Some retailers don't stock prose light novels — **AllStarComics, KingsComics,
OKComics**. Passing `BookType.LightNovel` to a scrape that includes them is
allowed; those sites log a `"site does not stock LightNovel — skipping"`
message and return zero entries, while the rest of the sites in the same call
run normally. Nothing lands in `Errors` — the skip is intentional, not a
failure.

***

### Getting Results

`GetResults()` returns the consolidated, deduped, sorted entries.
`GetResultUrls()` returns the per-site landing URL used so callers can
deep-link back into the source.

```cs
IReadOnlyList<EntryModel> results = scrape.GetResults();
IReadOnlyDictionary<Website, string> urls = scrape.GetResultUrls();
```

#### `EntryModel`

Each row is a `public struct` with four public fields:

| Field | Type | Example |
|---|---|---|
| `Entry` | `string` | `"Jujutsu Kaisen Vol 12"` |
| `Price` | `string` | `"$9.99"` (display form with currency symbol) |
| `StockStatus` | `StockStatus` | `IS`, `PO`, `BO`, `CS`, `OOS`, `NA` |
| `Website` | `string` | `"Crunchyroll"` (the source site's `TITLE` constant) |

Plus a `ParsePrice()` method that returns a `decimal` (handles currency
prefixes/suffixes and returns `0m` for unparseable rows).

```cs
foreach (EntryModel row in scrape.GetResults())
{
    if (row.StockStatus == StockStatus.IS && row.ParsePrice() < 10m)
    {
        Console.WriteLine($"{row.Entry} — {row.Price} at {row.Website}");
    }
}
```

#### Built-in renderers

Three convenience renderers ship with the library — all three take the same
`isAsciiTable`, `title`, `bookType`, `includeLinks` parameters.

```cs
// To console
scrape.PrintResultsToConsole(isAsciiTable: true,
    title: "jujutsu kaisen", bookType: BookType.Manga);

// To a logger at a chosen level
logger.PrintResults(scrape, LogLevel.Information, isAsciiTable: true,
    title: "jujutsu kaisen", bookType: BookType.Manga);

// To a file
scrape.PrintResultsToFile("FinalData.txt", isAsciiTable: true,
    title: "jujutsu kaisen", bookType: BookType.Manga);
```

`isAsciiTable: true` → table view, useful for human review:

```txt
Title: "jujutsu kaisen"
BookType: Manga
Region: America
┏━━━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━┳━━━━━━━━┳━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ Title                 ┃ Price ┃ Status ┃ Website                 ┃
┣━━━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━╋━━━━━━━━╋━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃ Jujutsu Kaisen Vol 1  ┃ $7.49 ┃ IS     ┃ InStockTrades           ┃
┃ Jujutsu Kaisen Vol 2  ┃ $7.49 ┃ IS     ┃ InStockTrades           ┃
┃ Jujutsu Kaisen Vol 3  ┃ $8.98 ┃ IS     ┃ RobertsAnimeCornerStore ┃
┃ Jujutsu Kaisen Vol 4  ┃ $7.49 ┃ IS     ┃ InStockTrades           ┃
┗━━━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━┻━━━━━━━━┻━━━━━━━━━━━━━━━━━━━━━━━━━┛
Links:
InStockTrades => https://www.instocktrades.com/search?title=jujutsu+kaisen
RobertsAnimeCornerStore => https://www.animecornerstore.com/jjkmgrno.html
```

`isAsciiTable: false` (the default) → one bracketed line per row, suitable
for piping into log aggregators or diff tooling:

```txt
[Jujutsu Kaisen Vol 1, $7.49, IS, InStockTrades]
[Jujutsu Kaisen Vol 2, $7.49, IS, InStockTrades]
[Jujutsu Kaisen Vol 3, $8.98, IS, RobertsAnimeCornerStore]
[Jujutsu Kaisen Vol 4, $7.49, IS, InStockTrades]
[InStockTrades,https://www.instocktrades.com/search?title=jujutsu+kaisen]
[RobertsAnimeCornerStore,https://www.animecornerstore.com/jjkmgrno.html]
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

***

### Enum Reference

The enums you'll touch most often. Full XML docs ship with the package so IDE
tooltips have the same details.

**`BookType`** — what to search for. Picked per scrape call.

| Value | Meaning |
|---|---|
| `Manga` | Graphic-novel format. |
| `LightNovel` | Prose light-novel format. Manga-only sites silently skip — see [Manga-only sites](#manga-only-sites). |

**`Region`** — `[Flags]` enum but one region per scrape (combining throws).

| Value | Notes |
|---|---|
| `America` | Default. |
| `Australia` | |
| `Britain` | |
| `Canada` | Currently SciFier only. |
| `Europe` | Currently SciFier only. |
| `Japan` | No working sites yet. |

**`StockStatus`** — appears on every `EntryModel`.

| Value | Meaning |
|---|---|
| `IS` | In stock — purchasable and shippable now. |
| `PO` | Pre-order — committed, ships at release. |
| `BO` | Backorder — site will fulfill when restocked. |
| `CS` | Coming soon — announced, no pre-order yet. |
| `OOS` | Out of stock — listed but currently unavailable. |
| `NA` | Status not exposed by the site. |

**`StockStatusFilter`** — passed to the `MasterScrape` constructor. Each one
is a static `StockStatus[]` of statuses to drop from results.

| Value | Drops |
|---|---|
| `EXCLUDE_NONE_FILTER` | Nothing — keeps every entry. |
| `EXCLUDE_PO_FILTER` | `PO` |
| `EXCLUDE_OOS_FILTER` | `OOS` |
| `EXCLUDE_BO_FILTER` | `BO` |
| `EXCLUDE_OOS_AND_PO_FILTER` | `OOS`, `PO` |
| `EXCLUDE_OOS_AND_BO_FILTER` | `OOS`, `BO` |
| `EXCLUDE_PO_AND_BO_FILTER` | `PO`, `BO` |
| `EXCLUDE_ALL_FILTER` | `PO`, `OOS`, `BO` |

**`Browser`** — Playwright channel for JS-rendered sites. Ignored when the
scrape includes only HTML-only sites.

| Value | Channel |
|---|---|
| `Chrome` | `chrome` |
| `Edge` | `msedge` (default) |
| `FireFox` | `firefox` |

**`Membership`** — `[Flags]`, combine with `\|`. Tag sites where the user holds
a membership account; the scraper picks the discounted price column for those
sites only.

| Value | Site |
|---|---|
| `None` | Default — public price columns only. |
| `BooksAMillion` | Millionaire's Club pricing. |
| `KinokuniyaUSA` | Bookweb pricing. |

***

### Stock-Aware Deduplication

The same volume often appears multiple times within a single site's results
(reprints, new-printing variants, edition markers) and again across sites.
The merge pass picks survivors by **availability first, then price**:

1. Lowest availability rank wins: `IS` > `PO` > `BO` > `CS` > `OOS` > `NA`.
2. Ties break by lowest `EntryModel.ParsePrice()`.

This deliberately drops a cheaper-but-out-of-stock listing in favor of a more
expensive listing the user can actually buy, which is the common shape for
Diamond-distributed manga where old printings linger as OOS rows after a
reprint replaces them.
