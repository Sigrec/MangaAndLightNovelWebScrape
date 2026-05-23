namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class KinokuniyaUSALogging
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Clicked List Mode")]
    public static partial void ClickedListMode(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Clicked Manga")]
    public static partial void ClickedManga(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Max Page Count = {Count}")]
    public static partial void MaxPageCount(this ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "BEFORE = {Title}")]
    public static partial void TitleBefore(this ILogger logger, string title);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "AFTER = {Title}")]
    public static partial void TitleAfter(this ILogger logger, string title);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Page {PageNum} = {Url}")]
    public static partial void PageVisited(this ILogger logger, int pageNum, string url);
}
