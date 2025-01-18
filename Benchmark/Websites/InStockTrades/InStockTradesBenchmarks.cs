using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Websites;

namespace Benchmark.Websites
{
    [MemoryDiagnoser]
    public class InStockTradesBenchmarks
    {
        private InStockTrades? _instance;

        [GlobalSetup]
        public void Setup()
        {
            _instance = new InStockTrades(); // Initialize your instance here
        }

        // Global cleanup to run once after all benchmarks
        [GlobalCleanup]
        public void Cleanup()
        {
            _instance = null; // Cleanup if necessary
        }

        [Benchmark]
        [WarmupCount(20)]
        public void GetMangaBenchmark()
        {
            // Call the method you want to benchmark
            _instance?.GetInStockTradesData("one piece", BookType.Manga);
        }

        // [Benchmark]
        // public void GetLightNovelBenchmark()
        // {
        //     // Call the method you want to benchmark
        //     _instance.GetInStockTradesData("one piece", BookType.LightNovel);
        // }
    }
}