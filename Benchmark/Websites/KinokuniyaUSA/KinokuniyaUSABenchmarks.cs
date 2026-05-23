using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace Benchmark.Websites.KinokuniyaUSA;

[MemoryDiagnoser]
public class KinokuniyaUSABenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA _site = null!;
    private readonly PlaywrightFixture _fixture = new();

    [GlobalSetup]
    public async Task Setup()
    {
        _site = new MangaAndLightNovelWebScrape.Websites.KinokuniyaUSA();
        await _fixture.InitializeAsync();
    }

    [GlobalCleanup]
    public async Task Cleanup() => await _fixture.DisposeAsync();

    [Benchmark]
    [WarmupCount(5)]
    public async Task GetMangaBenchmark()
    {
        IPage page = await PlaywrightFactory.GetPageAsync(_fixture.Browser, needsUserAgent: true);
        try
        {
            await _site.GetData("one piece", BookType.Manga, page, isMember: true);
        }
        finally
        {
            await page.DisposeContextAsync();
        }
    }
}
