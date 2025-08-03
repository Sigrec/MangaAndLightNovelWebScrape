using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using OpenQA.Selenium;

namespace Benchmark.Websites.KinokuniyaUSA;

[MemoryDiagnoser]
public class KinokuniyaUSABenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA? _instance;
    private WebDriver? _driver;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA(); 
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
    public async Task GetMangaBenchmark()
    {
        await _instance!.GetData("one piece", BookType.Manga, _driver!, true);
    }
}