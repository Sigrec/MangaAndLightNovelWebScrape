using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Websites;

namespace Benchmark.Websites
{
    [MemoryDiagnoser]
    public class CrunchyrollBenchmarks
    {
        private Crunchyroll? _instance;

        [GlobalSetup]
        public void Setup()
        {
            _instance = new Crunchyroll(); // Initialize your instance here
        }

        // Global cleanup to run once after all benchmarks
        [GlobalCleanup]
        public void Cleanup()
        {
            _instance = null; // Cleanup if necessary
        }

        [Benchmark]
        [WarmupCount(8)]
        public void GetMangaBenchmark()
        {
            // Call the method you want to benchmark
            _instance?.GetCrunchyrollData("one piece", BookType.Manga);
        }

        // [Benchmark]
        // public void GetLightNovelBenchmark()
        // {
        //     // Call the method you want to benchmark
        //     _instance.GetCrunchyrollData("one piece", BookType.LightNovel);
        // }
    }
}