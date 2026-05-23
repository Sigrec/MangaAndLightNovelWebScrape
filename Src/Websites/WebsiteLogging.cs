namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class WebsiteLogging
{
    #region Cross-cutting scrape lifecycle

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Url = {Url}")]
    public static partial void UrlGenerated(this ILogger logger, string url);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Page {PageNum} => {Url}")]
    public static partial void PageUrlGenerated(this ILogger logger, int pageNum, string url);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Next Page => {Url}")]
    public static partial void NextPageUrl(this ILogger logger, string url);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "{BookTitle} ({BookType}) Error @ {Website}")]
    public static partial void ScrapeError(this ILogger logger, Exception ex, string bookTitle, BookType bookType, string website);

    #endregion

    #region Entry removal

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Removed ({Variant}) {Title}")]
    public static partial void EntryRemoved(this ILogger logger, int variant, string title);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Removed ({Variant}) {Title}")]
    public static partial void EntryRemovedDebug(this ILogger logger, int variant, string title);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Removed {Title}")]
    public static partial void EntryRemovedSimple(this ILogger logger, string title);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Removed {Title}")]
    public static partial void EntryRemovedSimpleDebug(this ILogger logger, string title);

    #endregion
}
