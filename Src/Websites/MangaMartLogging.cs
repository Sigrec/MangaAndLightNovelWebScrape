namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class MangaMartLogging
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "URL #{PageNum} -> {Url}")]
    public static partial void PageUrl(this ILogger logger, uint pageNum, string url);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Max Pages = {Max}")]
    public static partial void MaxPages(this ILogger logger, int max);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Checking {Title} for Novel")]
    public static partial void CheckingForNovel(this ILogger logger, string title);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found Novel entry in Manga Scrape")]
    public static partial void FoundNovelInMangaScrape(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Title} | {IsNullOrWhiteSpace} | {StockStatusNode}")]
    public static partial void StockStatusDebug(this ILogger logger, string title, bool isNullOrWhiteSpace, string stockStatusNode);
}
