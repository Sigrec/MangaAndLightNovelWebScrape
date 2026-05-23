namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class InStockTradesLogging
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Title}")]
    public static partial void EntrySeen(this ILogger logger, string title);
}
