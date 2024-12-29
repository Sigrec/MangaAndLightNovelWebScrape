using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Websites;
using OpenQA.Selenium;

namespace Benchmark.Websites
{
    [MemoryDiagnoser]
    public class BooksAMillionBenchmarks
    {
        private BooksAMillion? _instance;
        private WebDriver? _driver;

        [GlobalSetup]
        public void Setup()
        {
            _instance = new BooksAMillion(); // Initialize your instance here
            _driver = Program.SetupBrowserDriver(true);
        }

        // Global cleanup to run once after all benchmarks
        [GlobalCleanup]
        public void Cleanup()
        {
            _driver?.Quit();
            _instance = null; // Cleanup if necessary
        }

        [Benchmark]
        [WarmupCount(1)]
        public void GetMangaBenchmark()
        {
            // Call the method you want to benchmark
            _instance?.GetBooksAMillionData("one piece", BookType.Manga, true, _driver);
        }

        // [Benchmark]
        // public void GetLightNovelBenchmark()
        // {
        //     // Call the method you want to benchmark
        //     _instance.GetBooksAMillionData("one piece", BookType.LightNovel);
        // }
    }
}