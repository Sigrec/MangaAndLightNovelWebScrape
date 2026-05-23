using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;
using BrowserEnum = MangaAndLightNovelWebScrape.Enums.Browser;

namespace Benchmark;

/// <summary>
/// Owns a single Playwright driver + browser across the lifetime of one benchmark class. Spinning
/// up Playwright (~1-2s of Node-driver bootstrap) is far too slow to repeat per iteration, so the
/// session is created once in <c>[GlobalSetup]</c> and disposed in <c>[GlobalCleanup]</c>. Each
/// <c>[Benchmark]</c> body still rents a fresh <see cref="IPage"/> and disposes it — that's the
/// part we're actually measuring.
/// </summary>
internal sealed class PlaywrightFixture : IAsyncDisposable
{
    private PlaywrightSession? _session;

    public IBrowser Browser =>
        _session?.Browser
        ?? throw new InvalidOperationException("InitializeAsync must be called before Browser is read.");

    public async Task InitializeAsync(BrowserEnum target = BrowserEnum.Edge)
    {
        _session = await PlaywrightFactory.SetupPlaywrightBrowserAsync(target);
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.DisposeAsync();
            _session = null;
        }
    }
}
