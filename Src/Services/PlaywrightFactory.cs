using System.Runtime.InteropServices;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Services;

/// <summary>
/// Bundles an <see cref="IPlaywright"/> driver with the launched <see cref="IBrowser"/>.
/// Disposing closes the browser then disposes the driver subprocess, so no resources leak
/// between scrape runs.
/// </summary>
internal sealed class PlaywrightSession : IAsyncDisposable
{
    public IPlaywright Playwright { get; }
    public IBrowser Browser { get; }

    internal PlaywrightSession(IPlaywright playwright, IBrowser browser)
    {
        Playwright = playwright;
        Browser = browser;
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.CloseAsync();
        Playwright.Dispose();
    }
}

internal static class PlaywrightFactory
{
    // Trimmed Chromium args:
    //   removed --incognito (NewContextAsync already isolates each context)
    //   removed --disable-dev-shm-usage (Linux/Docker workaround, no-op on Windows)
    //   removed --no-sandbox (Docker/Linux workaround; unsafe and unneeded on desktop)
    //   removed --disable-quic / --disable-http2 (forced HTTP/1.1 hurts throughput)
    private static readonly string[] CHROMIUM_ARGS_PLAYWRIGHT =
    [
        "--disable-extensions",
        "--disable-notifications",
        "--disable-gpu",
        "--disable-software-rasterizer",
    ];

    public static async Task<PlaywrightSession>
        SetupPlaywrightBrowserAsync(
            Browser target = Browser.Edge,
            bool headless = true)
    {
        IPlaywright playwright = await Playwright.CreateAsync();

        try
        {
            IBrowser browser = target switch
            {
                Browser.Edge => await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = headless,
                    Channel = "msedge",
                    Args = CHROMIUM_ARGS_PLAYWRIGHT,
                }),
                Browser.Chrome => await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = headless,
                    Channel = "chrome",
                    Args = CHROMIUM_ARGS_PLAYWRIGHT,
                }),
                Browser.FireFox => await playwright.Firefox.LaunchAsync(new()
                {
                    Headless = headless,
                    ExecutablePath = ResolveFirefoxExecutablePath(),
                }),
                _ => throw new ArgumentOutOfRangeException(nameof(target), $"Unsupported browser: {target}"),
            };

            return new PlaywrightSession(playwright, browser);
        }
        catch
        {
            playwright.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a new Playwright <see cref="IPage"/> from <paramref name="browser"/>, in its own
    /// fresh context (isolated cookies/storage).
    /// </summary>
    /// <remarks>
    /// The page and its context are NOT auto-disposed. Call
    /// <see cref="DisposeContextAsync(IPage)"/> in a finally block once the per-site scrape is done.
    /// </remarks>
    public static async Task<IPage>
        GetPageAsync(
            IBrowser browser,
            bool needsUserAgent = false,
            int defaultTimeout = 30_000,
            string? userAgentOverride = null,
            bool blockImages = true)
    {
        BrowserNewContextOptions opts = new()
        {
            ServiceWorkers = ServiceWorkerPolicy.Block,
            JavaScriptEnabled = true,
            IgnoreHTTPSErrors = true,
        };

        if (needsUserAgent || !string.IsNullOrWhiteSpace(userAgentOverride))
        {
            opts.UserAgent = ResolveUserAgent(needsUserAgent, userAgentOverride);
        }

        IBrowserContext context = await browser.NewContextAsync(opts);

        if (blockImages)
        {
            await context.RouteAsync("**/*", async route =>
            {
                string type = route.Request.ResourceType;
                // Never abort the HTML itself
                if (type is "document")
                {
                    await route.ContinueAsync();
                    return;
                }

                // Block heavy/visual assets the scraper never reads
                if (type is "image" or "font" or "media" or "stylesheet")
                {
                    await route.AbortAsync();
                    return;
                }

                await route.ContinueAsync();
            });
        }

        IPage page = await context.NewPageAsync();
        page.SetDefaultTimeout(defaultTimeout);
        page.SetDefaultNavigationTimeout(defaultTimeout);

        return page;
    }

    /// <summary>
    /// Closes the page and its owning context. Safe to call in a finally block after a per-site
    /// scrape — releases the context's storage, cookie jar, and route handler.
    /// </summary>
    public static async Task DisposeContextAsync(this IPage page)
    {
        IBrowserContext context = page.Context;
        try
        {
            await page.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    private static string ResolveUserAgent(bool needsUserAgent, string? userAgentOverride)
    {
        if (!string.IsNullOrWhiteSpace(userAgentOverride))
        {
            return userAgentOverride;
        }

        if (needsUserAgent)
        {
            HtmlWeb web = new();
            if (!string.IsNullOrWhiteSpace(web.UserAgent))
            {
                return web.UserAgent;
            }
        }

        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
               "AppleWebKit/537.36 (KHTML, like Gecko) " +
               "Chrome/124.0.0.0 Safari/537.36";
    }

    /// <summary>
    /// Returns a system Firefox executable path on Windows; on other OSes returns null so
    /// Playwright falls back to its bundled Firefox.
    /// </summary>
    private static string? ResolveFirefoxExecutablePath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Mozilla Firefox", "firefox.exe");

        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// After clicking a "Load all" button (or similar), scrolls the page to the bottom in steps
    /// and waits until lazy-loaded content stops changing. Designed to make headless scraping
    /// reliably load all items before calling <see cref="IPage.ContentAsync"/>.
    /// </summary>
    /// <param name="page">The Playwright <see cref="IPage"/> to operate on.</param>
    /// <param name="watchSelector">
    /// A selector that appears once per loaded item (e.g., a product title link). The method
    /// polls this selector's element count to detect when no more items are being appended.
    /// </param>
    /// <param name="maxScrolls">Upper bound on scroll iterations to avoid infinite loops.</param>
    /// <param name="stabilityMs">
    /// Duration, in milliseconds, that the watched element count must remain unchanged
    /// (with no document height growth) before the page is considered settled.
    /// </param>
    /// <param name="stepPx">Vertical scroll step, in pixels, per iteration.</param>
    public static async Task ScrollToBottomUntilStableAsync(
        this IPage page,
        string watchSelector,
        int maxScrolls = 50,
        int stabilityMs = 800,
        int stepPx = 1200)
    {
        async Task<int> getHeight() =>
            await page.EvaluateAsync<int>(
                "() => Math.max(document.body.scrollHeight, document.documentElement.scrollHeight)");

        async Task scrollBy(int dy) =>
            await page.EvaluateAsync("dy => window.scrollBy(0, dy)", dy);

        int lastHeight = await getHeight();
        int stableFor = 0;
        int previousCount = -1;
        const int pollMs = 150;

        for (int i = 0; i < maxScrolls; i++)
        {
            await scrollBy(stepPx);
            await Task.Delay(250);

            int newHeight = await getHeight();

            int count = await page.Locator(watchSelector).CountAsync();
            if (count != previousCount)
            {
                previousCount = count;
                stableFor = 0;
            }
            else
            {
                stableFor += pollMs;
            }

            if (newHeight <= lastHeight && stableFor >= stabilityMs)
                break;

            lastHeight = newHeight;
        }

        await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
    }

    /// <summary>
    /// Attempts to click the target element, waiting until it is visible within the given timeout.
    /// Falls back to a forced JavaScript click if the normal Playwright click fails (e.g. overlays).
    /// </summary>
    public static async Task ForceClickAsync(this ILocator locator, int timeout = 5_000)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeout
            });
            await locator.ClickAsync();
        }
        catch
        {
            await locator.ClickAsync(new LocatorClickOptions { Force = true });
        }
    }
}
