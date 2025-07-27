using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using OpenQA.Selenium;

namespace Benchmark.Websites.MangaMart;

[MemoryDiagnoser]
public class MangaMartBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.MangaMart? _instance;
    private WebDriver? _driver;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MangaAndLightNovelWebScrape.Websites.MangaMart();
        _driver = MasterScrape.SetupBrowserDriver(Browser.FireFox, true);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _driver?.Quit();
        _instance = null;
    }

    [Benchmark]
    [WarmupCount(5)]
    public void GetMangaBenchmark()
    {
        _instance?.GetMangaMartData("one piece", BookType.Manga, _driver!);
    }
}