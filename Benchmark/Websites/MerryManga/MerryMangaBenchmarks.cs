using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using OpenQA.Selenium;

namespace Benchmark.Websites.MerryManga;

[MemoryDiagnoser]
public class MerryMangaBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.MerryManga? _instance;
    private WebDriver? _driver;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MangaAndLightNovelWebScrape.Websites.MerryManga();
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
        await _instance!.GetData("one piece", BookType.Manga, _driver!);
    }
}