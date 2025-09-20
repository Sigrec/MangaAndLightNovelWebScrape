using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;

namespace Benchmark.Websites.SciFier;

[MemoryDiagnoser]
public class SciFierBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.SciFier? _instance;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MangaAndLightNovelWebScrape.Websites.SciFier();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _instance = null;
    }

    [Benchmark]
    [WarmupCount(10)]
    public async Task GetMangaBenchmark()
    {
        await _instance!.GetData("one piece", BookType.Manga, curRegion: Region.America);
    }
}