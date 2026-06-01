namespace MangaAndLightNovelWebScrape.Enums
{
    /// <summary>
    /// Playwright browser channel used for sites that need JS rendering. Only relevant when
    /// the scrape includes a site listed in <c>InternalHelpers.NeedPlaywright</c>; HTML-only
    /// sites ignore this property. Defaults to <see cref="Edge"/>.
    /// </summary>
    public enum Browser
    {
        /// <summary>Google Chrome (the <c>chrome</c> Playwright channel).</summary>
        Chrome,
        /// <summary>Microsoft Edge (the <c>msedge</c> Playwright channel). Default.</summary>
        Edge,
        /// <summary>Mozilla Firefox (the <c>firefox</c> Playwright channel).</summary>
        FireFox
    }
}
