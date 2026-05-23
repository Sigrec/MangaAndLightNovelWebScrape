namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class MerryMangaLogging
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Text}")]
    public static partial void ProductTitleSeen(this ILogger logger, string text);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Proceeded from 18+ popup")]
    public static partial void ProceededFromAgePopup(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No box set entries found, Checking Manga Only Link")]
    public static partial void NoBoxSetEntries(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loading more entries...")]
    public static partial void MerryMangaLoadingMoreEntries(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Finished loading more entries")]
    public static partial void FinishedLoadingMoreEntries(this ILogger logger);

}
