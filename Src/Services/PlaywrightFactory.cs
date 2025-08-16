using System.Net;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Services;

internal static class PlaywrightFactory
{
    private static readonly string[] CHROMIUM_ARGS_PLAYWRIGHT =
    [
        "--disable-dev-shm-usage",
        "--disable-extensions",
        "--disable-notifications",
        "--disable-gpu",
        "--disable-software-rasterizer",
        "--no-sandbox",
        "--incognito"
    ];

    public static async Task<IBrowser>
        SetupPlaywrightBrowserAsync(
            Browser target = Browser.Edge,
            bool headless = true)
    {
        IPlaywright playwright = await Playwright.CreateAsync();

        bool isChromium = target == Browser.Edge || target == Browser.Chrome;
        string? channel = null;
        string fireFoxExePath = string.Empty;
        IBrowser browser;

        // Validate requested browser exists (Windows-only)
        switch (target)
        {
            case Browser.Edge:
                if (!IsEdgeInstalled())
                {
                    throw new InvalidOperationException("Microsoft Edge is not installed on this system.");
                }
                channel = "msedge";
                break;

            case Browser.Chrome:
                if (!IsChromeInstalled())
                {
                    throw new InvalidOperationException("Google Chrome is not installed on this system.");
                }
                channel = "chrome";
                break;

            case Browser.FireFox:
                fireFoxExePath = FirefoxInstallPath();
                if (string.IsNullOrWhiteSpace(fireFoxExePath))
                {
                    throw new InvalidOperationException("Mozilla Firefox is not installed on this system.");
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(target), $"Unsupported browser: {target}");
        }

        // Launch=
        if (isChromium)
        {
            BrowserTypeLaunchOptions launch = new()
            {
                Headless = headless,
                Args = CHROMIUM_ARGS_PLAYWRIGHT,
                Channel = channel
            };
            browser = await playwright.Chromium.LaunchAsync(launch);
        }
        else
        {
            BrowserTypeLaunchOptions launch = new()
            {
                ExecutablePath = fireFoxExePath,
                Headless = headless
            };
            browser = await playwright.Firefox.LaunchAsync(launch);
        }

        return browser;
    }

    /// <summary>
    /// Creates and returns a new Playwright <see cref="IPage"/> instance with optional
    /// User-Agent override, image request blocking, and custom default timeouts.
    /// </summary>
    /// <param name="browser">The Playwright <see cref="IBrowser"/> to create the page from.</param>
    /// <param name="needsUserAgent">
    /// If <c>true</c>, a default User-Agent string will be resolved and applied to the browser context.
    /// </param>
    /// <param name="defaultTimeout">
    /// The default timeout in milliseconds to apply for all page operations and navigation.
    /// </param>
    /// <param name="userAgentOverride">
    /// A specific User-Agent string to use instead of the default. Overrides <paramref name="needsUserAgent"/>.
    /// </param>
    /// <param name="blockImages">
    /// If <c>true</c>, blocks requests for common image file types (.png, .jpg, .jpeg, .webp, .gif)
    /// to reduce bandwidth and improve performance.
    /// </param>
    /// <returns>
    /// A configured <see cref="IPage"/> instance ready for navigation and interaction.
    /// </returns>
    public static async Task<IPage>
        GetPageAsync(
            IBrowser browser,
            bool needsUserAgent = false,
            int defaultTimeout = 15000,
            string? userAgentOverride = null,
            bool blockImages = true
        )
    {
        // Context (only set UA if asked; otherwise preserve user’s locale/region)
        IBrowserContext context;
        if (needsUserAgent || !string.IsNullOrWhiteSpace(userAgentOverride))
        {
            string ua = ResolveUserAgent(needsUserAgent, userAgentOverride);
            BrowserNewContextOptions opts = new() { UserAgent = ua };
            context = await browser.NewContextAsync(opts);
        }
        else
        {
            context = await browser.NewContextAsync();
        }

        if (blockImages)
        {
            await context.RouteAsync("**/*", async route =>
            {
                string u = route.Request.Url;
                if (u.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    u.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    u.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    u.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                    u.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    await route.AbortAsync();
                }
                else
                {
                    await route.ContinueAsync();
                }
            });
        }

        IPage page = await context.NewPageAsync();
        page.SetDefaultTimeout(defaultTimeout);
        page.SetDefaultNavigationTimeout(defaultTimeout);

        return page;
    }

    private static string ResolveUserAgent(bool needsUserAgent, string? userAgentOverride)
    {
        if (!string.IsNullOrWhiteSpace(userAgentOverride))
            return userAgentOverride;

        if (needsUserAgent)
        {
            HtmlWeb web = new();
            if (!string.IsNullOrWhiteSpace(web.UserAgent))
                return web.UserAgent;
        }

        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
               "AppleWebKit/537.36 (KHTML, like Gecko) " +
               "Chrome/124.0.0.0 Safari/537.36";
    }

    private static bool IsEdgeInstalled()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft", "Edge", "Application", "msedge.exe");
        return File.Exists(path);
    }

    private static bool IsChromeInstalled()
    {
        string localPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google", "Chrome", "Application", "chrome.exe");

        string programFilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Google", "Chrome", "Application", "chrome.exe");

        return File.Exists(localPath) || File.Exists(programFilesPath);
    }

    private static string FirefoxInstallPath()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Mozilla Firefox", "firefox.exe");
        return path;
    }

    /// <summary>
    /// After clicking a “Load all” button (or similar), scrolls the page to the bottom in steps
    /// and waits until lazy-loaded content stops changing. Designed to make headless scraping
    /// reliably load all items before calling <see cref="IPage.ContentAsync"/>.
    /// </summary>
    /// <param name="page">The Playwright <see cref="IPage"/> to operate on.</param>
    /// <param name="watchSelector">
    /// A selector that appears once per loaded item (e.g., a product title link). The method
    /// polls this selector’s element count to detect when no more items are being appended.
    /// </param>
    /// <param name="maxScrolls">Upper bound on scroll iterations to avoid infinite loops.</param>
    /// <param name="stabilityMs">
    /// Duration, in milliseconds, that the watched element count must remain unchanged
    /// (with no document height growth) before the page is considered settled.
    /// </param>
    /// <param name="stepPx">Vertical scroll step, in pixels, per iteration.</param>
    /// <remarks>
    /// For each scroll step, the method waits for <see cref="LoadState.NetworkIdle"/> and a short delay,
    /// then checks both document height and the watched element count. Completion occurs when the height
    /// stops increasing and the count is stable for <paramref name="stabilityMs"/>. Finally, it jumps to
    /// the absolute bottom to trigger any last viewport-dependent loaders.
    /// </remarks>
    public static async Task ScrollToBottomUntilStableAsync(
        this IPage page,
        string watchSelector,
        int maxScrolls = 50,
        int stabilityMs = 800,
        int stepPx = 1200)
    {
        // measure document height (body vs documentElement)
        async Task<int> getHeight() =>
            await page.EvaluateAsync<int>(
                "() => Math.max(document.body.scrollHeight, document.documentElement.scrollHeight)");

        // scroll by a chunk
        async Task scrollBy(int dy) =>
            await page.EvaluateAsync("dy => window.scrollBy(0, dy)", dy);

        int lastHeight = await getHeight();
        int stableFor = 0;
        int previousCount = -1;
        const int pollMs = 150;

        for (int i = 0; i < maxScrolls; i++)
        {
            // step down
            await scrollBy(stepPx);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(150);

            int newHeight = await getHeight();

            // watch a selector's count to ensure lazy content finished appending
            int count = await page.Locator(watchSelector).CountAsync();
            if (count != previousCount)
            {
                previousCount = count;
                stableFor = 0;                 // reset stability timer on change
            }
            else
            {
                stableFor += pollMs;
            }

            // if page stopped growing AND item count stayed stable long enough, we're done
            if (newHeight <= lastHeight && stableFor >= stabilityMs)
                break;

            lastHeight = newHeight;
        }

        // final jump to absolute bottom
        await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
    }
}