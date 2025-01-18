using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Websites;

namespace Benchmark.Websites
{
    [MemoryDiagnoser]
    public class RobertsAnimeCornerStoreBenchmarks
    {
        private RobertsAnimeCornerStore? _instance;

        [GlobalSetup]
        public void Setup()
        {
            _instance = new RobertsAnimeCornerStore(); // Initialize your instance here
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
            _instance?.GetRobertsAnimeCornerStoreData("one piece", BookType.Manga);
        }

        // [Benchmark]
        // public void GetLightNovelBenchmark()
        // {
        //     // Call the method you want to benchmark
        //     _instance.GetRobertsAnimeCornerStoreData("one piece", BookType.LightNovel);
        // }
    }
}