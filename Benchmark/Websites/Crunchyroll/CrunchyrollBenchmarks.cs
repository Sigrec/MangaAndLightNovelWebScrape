using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;

namespace Benchmark.Websites.Crunchyroll;

[MemoryDiagnoser]
public class CrunchyrollBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.Crunchyroll? _instance;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MangaAndLightNovelWebScrape.Websites.Crunchyroll();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _instance = null;
    }

    [Benchmark]
    [WarmupCount(8)]
    public async Task GetMangaBenchmark()
    {
        await _instance!.GetData("one piece", BookType.Manga);
    }
}