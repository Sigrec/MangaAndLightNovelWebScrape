using Benchmark.Websites;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Columns;
using System.Reflection;
using OpenQA.Selenium;
using MangaAndLightNovelWebScrape.Enums;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Chrome;
using HtmlAgilityPack;
using MangaAndLightNovelWebScrape;

public class Program
{
    public static void Main(string[] args)
    {
        // Dictionary to map website names to their corresponding benchmark classes
        var websiteBenchmarks = new Dictionary<string[], Type>
        {
            { [ "Crunchyroll", "CR" ], typeof(CrunchyrollBenchmarks) },
            { [ "InStockTrades", "IST" ], typeof(InStockTradesBenchmarks) },
            { [ "RobertsAnimeCornerStore", "ROB" ], typeof(RobertsAnimeCornerStoreBenchmarks) },
            { [ "BooksAMillion", "BAM" ], typeof(BooksAMillionBenchmarks) },
            { [ "KinokuniyaUSA", "KINOUS" ], typeof(KinokuniyaUSABenchmarks) },
        };


        // Check if an argument is given, if not, run all benchmarks
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided. Running all benchmarks...");
            RunBenchmarks(null, null);
        }
        else
        {
            // If an argument is given, run the specific benchmark
            string website = args[0]; // Assume the first argument is the website name
            if (websiteBenchmarks.Keys.Any(keys => keys.Contains(website)))
            {
                RunBenchmarks(website, websiteBenchmarks);
            }
            else
            {
                Console.WriteLine($"Unknown website: {website}. Please provide a valid website name.");
            }
        }
    }

    private static void RunBenchmarks(string? website, Dictionary<string[], Type>? websiteBenchmarks)
    {

        // Create a custom configuration for BenchmarkDotNet with an output folder
        var config = ManualConfig.CreateEmpty()
            .WithOptions(ConfigOptions.JoinSummary)
            .AddExporter(MarkdownExporter.GitHub)
            .AddLogger(ConsoleLogger.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .WithArtifactsPath(GetArtifactsPath(websiteBenchmarks!
                .FirstOrDefault(entry => entry.Key.Contains(website))
                .Key.FirstOrDefault())
            );

        if (website == null)
        {
            // If no specific website, run all benchmarks
            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run([], config);
        }
        else
        {
            // If a specific website is given, run that website's benchmark
            Type benchmarkType = websiteBenchmarks!
                .FirstOrDefault(entry => entry.Key.Contains(website))
                .Value;
            string filter = $"*{benchmarkType.Name}*";

            // Run the specific benchmark with the filter and output configuration
            BenchmarkSwitcher.FromAssembly(benchmarkType.Assembly).Run(["--filter", filter], config);
        }
    }

    private static string GetArtifactsPath(string? website)
    {
        // If a website is provided, use it to create a folder under 'results'
        // Otherwise, create a generic folder for all results
        string folderName = @$"Websites/{website}" ?? "AllBenchmarks";
        Directory.CreateDirectory(@$"Websites/{folderName}"); // Ensure the directory exists
        return folderName;
    }

    internal static WebDriver SetupBrowserDriver(bool needsUserAgent = false)
    {
        Browser browser = Browser.FireFox;
        switch (browser)
        {
            case Browser.Edge:
                EdgeOptions edgeOptions = new()
                {
                    PageLoadStrategy = PageLoadStrategy.Normal,
                };
                EdgeDriverService edgeDriverService = EdgeDriverService.CreateDefaultService();
                edgeDriverService.HideCommandPromptWindow = true;
                edgeOptions.AddArguments(MasterScrape.CHROME_BROWSER_ARGUMENTS);
                edgeOptions.AddExcludedArgument("disable-popup-blocking");
                edgeOptions.AddUserProfilePreference("profile.default_content_settings.geolocation", 2);
                edgeOptions.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
                if (needsUserAgent) edgeOptions.AddArgument($"user-agent={new HtmlWeb().UserAgent}");
                return new EdgeDriver(edgeDriverService, edgeOptions);
            case Browser.Chrome:
                ChromeOptions chromeOptions = new()
                {
                    PageLoadStrategy = PageLoadStrategy.Normal,
                };
                ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;
                chromeOptions.AddArguments(MasterScrape.CHROME_BROWSER_ARGUMENTS);
                chromeOptions.AddExcludedArgument("disable-popup-blocking");
                chromeOptions.AddUserProfilePreference("profile.default_content_settings.geolocation", 2);
                chromeOptions.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
                if (needsUserAgent) chromeOptions.AddArgument($"user-agent={new HtmlWeb().UserAgent}");
                return new ChromeDriver(chromeDriverService, chromeOptions);
            case Browser.FireFox:
            default:
                FirefoxOptions firefoxOptions = new()
                {
                    PageLoadStrategy = PageLoadStrategy.Normal,
                    AcceptInsecureCertificates = true
                };
                FirefoxDriverService fireFoxDriverService = FirefoxDriverService.CreateDefaultService();
                fireFoxDriverService.HideCommandPromptWindow = true;

                firefoxOptions.AddArguments(MasterScrape.FIREFOX_BROWSER_ARGUMENTS);
                firefoxOptions.SetPreference("javascript.enabled", true);
                firefoxOptions.SetPreference("media.peerconnection.enabled", false);  // Disable WebRTC
                firefoxOptions.SetPreference("devtools.console.stdout.content", false);
                firefoxOptions.SetPreference("profile.default_content_settings.geolocation", 2);
                firefoxOptions.SetPreference("profile.default_content_setting_values.notifications", 2);
                firefoxOptions.SetPreference("dom.webdriver.enabled", false);  // Disable WebDriver detection
                firefoxOptions.SetPreference("browser.safebrowsing.enabled", false);  // Disable safe browsing
                firefoxOptions.SetPreference("layers.acceleration.disabled", true);  // Disable hardware acceleration
                firefoxOptions.SetPreference("privacy.trackingprotection.enabled", false);  // Disable tracking protection
                firefoxOptions.SetPreference("webgl.disabled", true);  // Disable WebGL
                firefoxOptions.SetPreference("extensions.enabled", false);  // Disable extensions
                firefoxOptions.SetPreference("app.update.enabled", false);  // Disable automatic updates

                // Optional additional settings for improved performance
                firefoxOptions.SetPreference("browser.preferences.animateFadeIn", false);  // Disable fade-in animations
                firefoxOptions.SetPreference("toolkit.cosmeticAnimations.enabled", false);  // Disable cosmetic animations
                firefoxOptions.SetPreference("layout.css.prefers-reduced-motion.enabled", true);  // Disable CSS animations
                firefoxOptions.SetPreference("browser.sessionstore.restore_on_demand", true);  // Load tabs on demand
                firefoxOptions.SetPreference("browser.sessionstore.max_tabs_undo", 0);  // Disable undo closed tabs
                firefoxOptions.SetPreference("datareporting.healthreport.service.enabled", false);  // Disable health report
                firefoxOptions.SetPreference("toolkit.telemetry.enabled", false);  // Disable telemetry
                firefoxOptions.SetPreference("toolkit.telemetry.unified", false);  // Disable unified telemetry
                firefoxOptions.SetPreference("app.shield.optoutstudies.enabled", false);  // Disable Shield studies
                firefoxOptions.SetPreference("browser.sessionhistory.max_entries", 0);  // Disable session history

                if (needsUserAgent) firefoxOptions.SetPreference("general.useragent.override", new HtmlWeb().UserAgent);

                return new FirefoxDriver(fireFoxDriverService, firefoxOptions);
        }
    }
}