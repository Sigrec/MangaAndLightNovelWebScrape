using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;

namespace Benchmark.Websites.RobertsAnimeCornerStore;

[MemoryDiagnoser]
public class RobertsAnimeCornerStoreBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.RobertsAnimeCornerStore? _instance;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MangaAndLightNovelWebScrape.Websites.RobertsAnimeCornerStore();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _instance = null;
    }

    [Benchmark]
    [WarmupCount(20)]
    public async Task GetMangaBenchmark()
    {
        await _instance!.GetData("one piece", BookType.Manga);
    }
}