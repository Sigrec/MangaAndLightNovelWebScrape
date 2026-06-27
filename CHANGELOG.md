# Changelog

## Legend

- ✅ Added / Removed features
- 🔥 Bug fixes
- ⌛ Performance improvements
- 📜 Site-specific changes

---

## v6.0.0 – June 27, 2026

### 🚀 Major Changes

- ✅ Migrated to **.NET 10**
  - New `<TargetFramework>net10.0</TargetFramework>`
  - Switched from `ILogger.LogX(...)` calls to source-generated logging via `[LoggerMessage]`
- ✅ Added three new website scrapers — all HTML-only, no Playwright required
  - **All Star Comics Melbourne** (`Region.Australia`) — Shopify, Diamond catalog feed
  - **Kings Comics Sydney** (`Region.Australia`) — Shopify, includes inline stock + Last-one signaling
  - **OK Comics Leeds** (`Region.Britain`) — Shopify with per-product detail fetch for prices
- ✅ Added **`ScrapeException` hierarchy** for structured error handling
  - `ScrapeBrowserLaunchException` — thrown when Playwright can't launch
  - `SiteScrapeException` — recorded in `MasterScrape.Errors` per site, never thrown
  - `SiteUnavailableException` — recorded when `SkipUnavailableSites` drops an unreachable site
  - Per-site failures no longer abort the rest of the scrape — siblings keep running
  - Full contract in [`docs/ErrorHandling.md`](https://github.com/Sigrec/MangaAndLightNovelWebScrape/blob/master/docs/ErrorHandling.md)
- ✅ Added **reachability probes** so callers can short-circuit known-down sites
  - `IsSiteAvailableAsync(Website)` — single-site check
  - `CheckSitesAvailableAsync(IEnumerable<Website>)` — parallel multi-site check
  - `SkipUnavailableSites` property — pre-flight check inside `InitializeScrapeAsync`
- ✅ Added **stock-aware deduplication** during merge
  - Survivors picked by availability rank (`IS > PO > BO > CS > OOS > NA`), price tiebreak
  - Drops cheaper-but-out-of-stock listings in favor of buyable ones — common shape for Diamond reprints
- ✅ Added **manga-only site handling** — passing `BookType.LightNovel` to AllStarComics / KingsComics / OKComics now logs a skip message and returns zero entries instead of throwing; no `Errors` entry
- ✅ Added **cooperative cancellation** — `InitializeScrapeAsync` and the reachability probes accept `CancellationToken`
- ✅ Migrated all 15 sites to **fixture-based parser tests** with `[Explicit]` Regenerate tasks for refreshing saved HTML snapshots — tests run offline, no live network
- ✅ Added internal `WebsiteEnumSurfaceTests` guard — every implemented site must be registered in every helper registry (title map, region map, URL switch, scraper factory)
- ✅ Expanded XML docs across public enums and helpers — every public type/member has an IDE-readable summary
- ⌛ **Per-entry hot path optimizations** across all sites
  - `_entryRemovalTerms` FrozenSet → `SearchValues<string>` — one vectorized scan replaces ~47 sequential ignore-case `Contains` calls per entry
  - `bookTitle` normalization now cached once per scrape via `EntryTitleContainsNormalizedBookTitle` instead of re-normalized per entry
  - `ShouldRemoveEntry` split — no more `Concat` allocation or boxed FrozenSet enumerator on the fast path
  - `SortByVolume` closure-capturing lambda → value-type `IComparer<int>` struct (no per-sort delegate alloc)
  - OKComics detail-page fan-out bounded by `SemaphoreSlim` (cap 8) — avoids hammering small Shopify shops
- ⌛ Switched several site title-cleanup pipelines to `string.Create` + `Span<T>` — no per-entry StringBuilder allocation
- ⌛ `FilterBookTitle` uses `TryFormat` into a hoisted stack span for hex encoding — no per-char string allocation
- ⌛ **JSON migrated to source generators** — `TranslateAPI` now uses a `JsonSerializerContext` (`AniListJsonContext`) for all serialize/deserialize calls. Removed the `GraphQL.Client` and `GraphQL.Client.Serializer.SystemTextJson` dependencies entirely and replaced them with a hand-rolled `HttpClient` + source-gen request/response records. Flipped `<JsonSerializerIsReflectionEnabledByDefault>` to `false` so reflection-based JSON in the library now fails loudly under trim/AOT instead of silently working
- 🔥 Fixed long-standing bug in `TranslateAPI.Dispose` — previously disposed the shared static GraphQL client on the first instance dispose, killing every other caller's client for the rest of the process. The new implementation has nothing to dispose on instances; the `IDisposable` surface stays for backward compat but is a true no-op
- ✅ Added **one-time accuracy disclaimer** logged at `Warning` level on the first `InitializeScrapeAsync` call per process — sets the expectation that results are a best-effort snapshot of live retailer HTML, not an authoritative price feed. `Interlocked.CompareExchange` ensures exactly one disclaimer even under racing first-time scrapes
- ✅ Migrated `MasterScrape.IsDebugEnabled` from a process-wide `static` property to **per-instance state** with `AsyncLocal<bool>` propagation — two `MasterScrape` instances no longer affect each other's debug mode; sites and helpers read `MasterScrape.IsDebugEnabledForCurrentScrape` with no `IWebsite.GetData` API break
- ✅ Swept **`.ConfigureAwait(false)` across every `await`** in the library (~160 awaits in 19 files) — prevents sync-over-async deadlocks in legacy ASP.NET / WPF consumers; no-op for modern ASP.NET Core and console hosts
- ⌛ **ASCII renderer per-row allocations eliminated** — `StockStatus.ToString()`, `BookType.ToString()`, `Region.ToString()` replaced with switches returning interned literals; zero-alloc after JIT
- ⌛ Dropped wasteful `new HtmlWeb()` allocation in `PlaywrightFactory.ResolveUserAgent` — now reads a `const` UA literal instead of instantiating an HtmlWeb just to peek at its default
- 🔥 Replaced an unusual `[with(15)]` initializer on `MasterScrape._webTasks` with the explicit `new(15)` capacity hint
- ✅ Converted `Models.StockStatusFilter` from `readonly struct` to `static class` — it never had instance members; the struct form let callers write misleading `new StockStatusFilter()` / `default(StockStatusFilter)` no-ops

### 🛠 Build & Tooling

- ✅ `Directory.Build.props` now enforces **`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`** solution-wide — consumers never inherit shipped warnings
- ✅ `Src/MangaAndLightNovelWebScrape.csproj` declares **`<IsAotCompatible>false</IsAotCompatible>`** explicitly — library still uses reflection-based JSON via GraphQL.Client, so the contract is honest until JSON usage migrates to a source-generated `JsonSerializerContext`
- ✅ Removed `<ProduceReferenceAssembly>False</ProduceReferenceAssembly>` from the Src csproj — defaults to `True` for libraries, **enabling consumers' incremental builds** (previously every assembly change forced a full rebuild downstream)
- 🔥 **`Benchmark/Program.cs` cleaned up**
  - Replaced `Dictionary<string[], Type>` keyed by array *reference* (which forced a linear-scan `.Keys.Any(...)` per lookup) with a flat case-insensitive `Dictionary<string, Type>` registering both full name and short alias
  - Fixed `GetArtifactsPath` building paths like `Websites/Websites/{website}` — the `??` was dead and `folderName` was being re-prefixed on use
  - Added `.AddValidator(JitOptimizationsValidator.FailOnError)` to the `ManualConfig` so an accidental Debug-mode benchmark run fails loud instead of publishing misleading numbers
- ✅ Removed hard-coded `<Configuration>Release</Configuration>` from `Benchmark.csproj` — IDE debug sessions and `dotnet run -c Debug` now work; pass `-c Release` on the command line for benchmark runs

### 🔐 CI/CD

- ✅ Migrated NuGet release publishing to **Trusted Publishing via OIDC** — workflow uses `NuGet/login@v1` to exchange the OIDC token for a short-lived API key, eliminating the long-lived `NUGET_API_KEY` secret; added `id-token: write` permission to the release job
- ✅ Bumped `actions/checkout` and `actions/setup-dotnet` to `@v5`
- ✅ Updated workflow's .NET SDK from `9.0.x` to `10.0.x` so it can actually build the `net10.0` target

### 📜 Site-Specific Fixes & Improvements

#### AllStarComics (new)

- ✅ Australian Shopify retailer added for `Region.Australia`
- ✅ SHOUTING Diamond-catalog titles normalized to canonical `Title Vol N` form
- ✅ Leading-zero strip on single-digit volume numbers (`Vol 01` → `Vol 1`) so cross-site dedup matches other retailers' `Vol 1`
- ✅ Trailing edition-marker strip — `Vol 8 New Ptg (Mr)` and `Vol 8 (Mr)` collapse to `Vol 8` for clean dedup
- ✅ Multi-digit volume number preservation (`Vol 10` no longer truncated to `Vol 1`)
- ✅ `SearchValues<string>` replaces 6 sequential `Contains` checks per card

#### KingsComics (new)

- ✅ Australian Shopify retailer added for `Region.Australia`
- ✅ Listing card carries title + price + availability — no per-product detail fetch
- ✅ Stock parsing covers all four theme variants: `--in` (IS), `--last` (IS — still buyable), `--out` (OOS), `--preorder` (PO)
- ✅ Shares the AllStarComics title-cleanup pipeline (same Diamond catalog feed)

#### OKComics (new)

- ✅ British Shopify retailer added for `Region.Britain`
- ✅ Per-product detail-page fetches for prices — listing only carries title + availability
- ✅ Bounded concurrency via `SemaphoreSlim` (cap 8) to avoid hammering the shop on popular searches

#### Crunchyroll

- 🔥 Rebuilt scraper for new product-tile DOM (post-2025 redesign)
- 🔥 Updated User-Agent to current Edge / Chrome string — the site started rejecting older UAs
- ✅ Migrated to fixture-based parser tests

#### InStockTrades

- ✅ Migrated to fixture-based parser tests

#### BooksAMillion

- ⌛ Per-entry description fetches now batched via `Task.WhenAll` — previously serial round-trips inside the entry loop
- 🔥 Hardened scraper flow against malformed product tiles

#### MangaMate

- ⌛ Per-entry detail-page fetches batched via `Task.WhenAll`
- 🔥 Fixed wrong scroll-wait predicate that caused timeouts on short-result searches

#### SciFier

- ✅ Migrated to fixture-based parser tests

---

## v5.0.2 – Sept 20, 2025

### 🚀 Major Changes

- ✅ Removed **Indigo** and **Waterstones** websites  
  - Waterstones removed due to new captcha and robots.txt rules
  - Indigo removed due to security blockers
- ✅ Added **MangaMart** website scraper for the `America` region
- ✅ Introduced `IWebsite` interface for all sites  
  - Improves code structure  
  - Simplifies adding new/custom sites
- ✅ Added URL constants (`const string`) for all website objects
- ✅ Replaced **Selenium** with **Playwright**
- ✅ Improved comments/documentation for all public-facing members
- ⌛ Updated several collections to use `Frozen` for performance

### 📜 Site-Specific Fixes & Improvements

#### RobertsAnimeCornerStore

- 🔥 Fixed box set numbering
- 🔥 Fixed issue where series containing ampersand (`&`) were not scraped

#### InStockTrades

- 🔥 Fixed box set numbering
- 🔥 Fixed issue where series with ampersand (`&`) were not scraped

#### Crunchyroll

- 🔥 Fixed stock status mapping (was always mapping to Backorder `BO`)
- 🔥 Fixed incorrect filtering of Blu-ray and Funko Pop entries
- 🔥 Fixed issue with titles containing HTML-encoded characters

#### MerryManga

- 🔥 Fixed page loading issue preventing scraping
- 🔥 Fixed duplication of Box Set entries
- ⌛ General performance improvements

#### Amazon USA

- 🔥 Fixed incorrect price parsing causing missing items
- 🔥 Fixed issue with leading text after volume numbers
- 🔥 Fixed multiple box set parsing issues

#### MangaMate

- 🔥 Fixed page loading issue for series without multiple pages
- 🔥 Fixed incorrect stock status mapping for some OOS entries

#### Forbidden Planet

- 🔥 Fixed parsing of HTML-encoded characters
- 🔥 Fixed incorrect parsing of titles with text after `:`
- 🔥 Fixed light novel parsing issues

#### Books-A-Million

- 🔥 Fixed issue where series with ampersands (`&`) in titles were not scraped

#### SciFier

- 🔥 Fixed missing `"Novel"` suffix for novel entries
- 🔥 Fixed inconsistent removal of author text

#### Kinokuniya USA

- 🔥 Fixed scraping hang for certain series

#### TravellingMan

- 🔥 Fixed incorrect matching of entries to book type
- 🔥 Fixed parsing issues with ASCII characters
- 🔥 Fixed parsing of entries with extra text after volume/novel numbers
