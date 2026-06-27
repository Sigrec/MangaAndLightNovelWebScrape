# Error Handling

`MangaAndLightNovelWebScrape` is a library, so its error contract is defined by what
consumers can catch and what they can inspect. This page covers the exception
hierarchy, the `Errors` collection, cancellation, and the patterns you should use
when wrapping `MasterScrape` in your own app.

## Exception Hierarchy

All library-thrown exceptions inherit from `ScrapeException`:

```text
ScrapeException                       (abstract base — catch this to handle every library failure)
├── ScrapeBrowserLaunchException      (Playwright browser couldn't launch — catastrophic)
└── SiteScrapeException               (a single site's scrape failed — non-fatal, recorded in Errors)
```

| Type                            | When you'll see it                                          | Where it surfaces           |
| ------------------------------- | ----------------------------------------------------------- | --------------------------- |
| `ScrapeException`               | Abstract — never instantiated; useful as a catch-all base. | n/a                         |
| `ScrapeBrowserLaunchException`  | The host can't launch the chosen Playwright browser channel (e.g. msedge / chrome not installed, Node driver crashed). The original Playwright exception is preserved as `InnerException`. | **Thrown** from `InitializeScrapeAsync`. |
| `SiteScrapeException`           | One scraper threw — bad page schema, timeout, anti-bot block, network blip. Carries the offending `Site` and the original exception in `InnerException`. | **Recorded** in `MasterScrape.Errors`, never thrown. |

Non-library exceptions still escape: `ArgumentException` for bad input,
`NotSupportedException` for unsupported regions, `OperationCanceledException` for
cancelled scrapes. Catch them separately if relevant.

## The Errors Collection

```csharp
public IReadOnlyDictionary<Website, Exception> Errors { get; }
```

After `InitializeScrapeAsync` returns, `Errors` holds one entry per site that
failed. The value is always a `SiteScrapeException` whose `InnerException` is
whatever the site threw. A successful scrape leaves the dictionary empty.

Important properties of the contract:

- **One site failing never aborts the others.** The shared scrape helper catches
  per-site exceptions, wraps them, and records them. The remaining sites keep
  running.
- **The dictionary is cleared at the top of every `InitializeScrapeAsync` call.**
  You don't need to clear it yourself between scrapes.
- **Cancellation is excluded.** If you cancel a scrape, `OperationCanceledException`
  propagates out of `InitializeScrapeAsync` and `Errors` is *not* populated with
  it.

### Reading Errors

```csharp
await scrape.InitializeScrapeAsync(title, BookType.Manga, sites);

if (scrape.Errors.Count > 0)
{
    foreach ((Website site, Exception ex) in scrape.Errors)
    {
        logger.LogWarning(ex, "{Site} scrape failed", site);
    }
}

foreach (EntryModel entry in scrape.GetResults())
{
    // ...
}
```

`GetResults()` returns whatever the surviving sites produced — even when some
sites are in `Errors`. Decide per app whether a partial result set is acceptable
or whether you want to bail when `Errors` is non-empty.

## Recommended Catch Shape

```csharp
try
{
    await scrape.InitializeScrapeAsync(title, bookType, sites, cancellationToken);
}
catch (OperationCanceledException)
{
    // User cancelled — re-throw or surface a "cancelled" state.
    throw;
}
catch (ScrapeBrowserLaunchException ex)
{
    // Host-setup problem. Cannot scrape any Playwright site until this is fixed.
    // `ex.InnerException` is the underlying Playwright error.
    logger.LogCritical(ex, "Playwright browser failed to launch");
    return Result.Unavailable;
}
catch (ArgumentException ex)
{
    // Bad input (empty title, sites not valid for the current region).
    return Result.BadRequest(ex.Message);
}
catch (ScrapeException ex)
{
    // Defensive catch-all for any other library exception added in the future.
    logger.LogError(ex, "Unexpected library failure");
    return Result.Error;
}

// Per-site soft failures — non-fatal, recorded after the scrape.
foreach ((Website site, Exception ex) in scrape.Errors)
{
    logger.LogWarning(ex, "{Site} failed during {Title} scrape", site, title);
}
```

You can also catch `ScrapeException` alone if you don't need to distinguish
launch failures from per-site failures — but in practice the two warrant
different responses, so the split-catch above is recommended.

## Cancellation

`InitializeScrapeAsync` accepts a `CancellationToken`. When cancelled, the
method throws `OperationCanceledException` at the next checkpoint.

Checkpoints are between major phases:

1. After input validation, before browser launch.
2. After `Task.WhenAll` over the site tasks (the cooperative wait honors
   cancellation via `WaitAsync`).
3. Between price-comparison merge rounds.

Important nuance: an **in-flight network call inside a site cannot be aborted
mid-request.** `HtmlAgilityPack.HtmlWeb` does not accept a `CancellationToken`,
and Playwright's older .NET binding doesn't either. So cancellation is
*cooperative* — the surrounding orchestration stops waiting on in-flight tasks
but those tasks keep running until they finish (or time out) on their own.

For UI/CLI cancellation that needs to feel immediate, this is usually fine —
you stop responding to whatever the scrape produces, and the background tasks
release their resources when each network call completes.

```csharp
using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));

try
{
    await scrape.InitializeScrapeAsync(title, bookType, sites, cts.Token);
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    logger.LogInformation("Scrape timed out after 2 minutes");
}
```

