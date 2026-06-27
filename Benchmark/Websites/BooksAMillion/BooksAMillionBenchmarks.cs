using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace Benchmark.Websites.BooksAMillion;

[MemoryDiagnoser]
public class BooksAMillionBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.BooksAMillion _site = null!;
    private readonly PlaywrightFixture _fixture = new();

    [GlobalSetup]
    public async Task Setup()
    {
        _site = new MangaAndLightNovelWebScrape.Websites.BooksAMillion();
        await _fixture.InitializeAsync();
    }

    [GlobalCleanup]
    public async Task Cleanup() => await _fixture.DisposeAsync();

    [Benchmark]
    [WarmupCount(3)]
    public async Task GetMangaDataBenchmark()
    {
        IPage page = await PlaywrightFactory.GetPageAsync(_fixture.Browser, needsUserAgent: true);
        try
        {
            await _site.GetData("one piece", BookType.Manga, page, isMember: false);
        }
        finally
        {
            await page.DisposeContextAsync();
        }
    }
}
