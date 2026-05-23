namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class CDJapanLogging
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Alt Title = {AltTitle}")]
    public static partial void AltTitle(this ILogger logger, string? altTitle);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "{BookTitle} | {BookType} Does Not Exist @ {Website}")]
    public static partial void SeriesNotFound(this ILogger logger, Exception ex, string bookTitle, BookType bookType, string website);
}
