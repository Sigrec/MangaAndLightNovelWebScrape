namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class CrunchyrollLogging
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "BookTitleRemovalCheck = {Value}")]
    public static partial void BookTitleRemovalCheck(this ILogger logger, bool value);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Trying Second Link")]
    public static partial void TryingSecondLink(this ILogger logger);
}
