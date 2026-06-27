namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class ForbiddenPlanetLogging
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Snapshot = {Snapshot}")]
    public static partial void Snapshot(this ILogger logger, string snapshot);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Counts: title={Titles} price={Prices} minor={Minor} format={Format} stock={Stock}")]
    public static partial void NodeCounts(this ILogger logger, int titles, int prices, int minor, int format, int stock);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Some input value is null for {Title} Skipping")]
    public static partial void NullInputValue(this ILogger logger, string? title);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Unable to retrieve url path for entry desc at pos {Pos}")]
    public static partial void UnableToRetrieveUrlPath(this ILogger logger, int pos);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Checking Desc {Title} => {Desc}")]
    public static partial void CheckingDesc(this ILogger logger, string title, string desc);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Final Title = {Title}")]
    public static partial void FinalTitle(this ILogger logger, string title);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Checking Comics & Graphic Novel Category")]
    public static partial void CheckingComicsCategory(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loading more entries...")]
    public static partial void ForbiddenPlanetLoadingMoreEntries(this ILogger logger);
}
