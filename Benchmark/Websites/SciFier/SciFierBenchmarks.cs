using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Websites;

namespace Benchmark.Websites
{
    [MemoryDiagnoser]
    public class SciFierBenchmarks
    {
        private SciFier? _instance;

        [GlobalSetup]
        public void Setup()
        {
            _instance = new SciFier(); // Initialize your instance here
        }

        // Global cleanup to run once after all benchmarks
        [GlobalCleanup]
        public void Cleanup()
        {
            _instance = null; // Cleanup if necessary
        }

        [Benchmark]
        [WarmupCount(10)]
        public void GetMangaBenchmark()
        {
            // Call the method you want to benchmark
            _instance?.GetSciFierData("one piece", BookType.Manga, Region.America);
        }

        // [Benchmark]
        // public void GetLightNovelBenchmark()
        // {
        //     // Call the method you want to benchmark
        //     _instance.GetSciFierData("one piece", BookType.LightNovel);
        // }
    }
}