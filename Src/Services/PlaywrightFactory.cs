using System.Net;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Services;

public static class PlaywrightFactory
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
}