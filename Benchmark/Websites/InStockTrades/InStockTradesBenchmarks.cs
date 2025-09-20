using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;

namespace Benchmark.Websites.InStockTrades;

[MemoryDiagnoser]
public class InStockTradesBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.InStockTrades? _instance;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MangaAndLightNovelWebScrape.Websites.InStockTrades();
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