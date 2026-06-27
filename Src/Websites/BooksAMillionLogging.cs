namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class BooksAMillionLogging
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Title} | {FirstOmni} | {SecondOmni} | {ThirdOmni}")]
    public static partial void OmnibusDebug(this ILogger logger, string title, string firstOmni, string secondOmni, string thirdOmni);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Initial Url {Url}")]
    public static partial void InitialUrl(this ILogger logger, string url);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "One of the helm node collections returned no data")]
    public static partial void HelmCollectionEmpty(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Entry Title is null")]
    public static partial void EntryTitleNull(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Title} | {ContainsBookTitle} | {NovelMatch} | {NoLibraryBinding} | {NovelExtra}")]
    public static partial void EntryDebug(this ILogger logger, string title, bool containsBookTitle, bool novelMatch, bool noLibraryBinding, bool novelExtra);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Desc link = {Link}")]
    public static partial void DescLink(this ILogger logger, string link);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Next Page: {Url}")]
    public static partial void NextPage(this ILogger logger, string url);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Box Set Url: {Url}")]
    public static partial void BoxSetUrl(this ILogger logger, string url);
}
