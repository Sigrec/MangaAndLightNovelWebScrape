using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using OpenQA.Selenium;

namespace Benchmark.Websites.BooksAMillion;

[MemoryDiagnoser]
public class BooksAMillionBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.BooksAMillion? _instance;
    private WebDriver? _driver;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MangaAndLightNovelWebScrape.Websites.BooksAMillion();
        _driver = MasterScrape.SetupBrowserDriver(Browser.FireFox, true);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _driver?.Quit();
        _instance = null;
    }

    [Benchmark]
    [WarmupCount(1)]
    public void GetMangaBenchmark()
    {
        _instance?.GetData("one piece", BookType.Manga, true, _driver!);
    }
}