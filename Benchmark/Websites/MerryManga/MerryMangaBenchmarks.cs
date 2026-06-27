using BenchmarkDotNet.Attributes;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace Benchmark.Websites.MerryManga;

[MemoryDiagnoser]
public class MerryMangaBenchmarks
{
    private MangaAndLightNovelWebScrape.Websites.MerryManga _site = null!;
    private readonly PlaywrightFixture _fixture = new();

    [GlobalSetup]
    public async Task Setup()
    {
        _site = new MangaAndLightNovelWebScrape.Websites.MerryManga();
        await _fixture.InitializeAsync();
    }

    [GlobalCleanup]
    public async Task Cleanup() => await _fixture.DisposeAsync();

    [Benchmark]
    [WarmupCount(5)]
    public async Task GetMangaBenchmark()
    {
        IPage page = await PlaywrightFactory.GetPageAsync(_fixture.Browser);
        try
        {
            await _site.GetData("one piece", BookType.Manga, page);
        }
        finally
        {
            await page.DisposeContextAsync();
        }
    }
}
