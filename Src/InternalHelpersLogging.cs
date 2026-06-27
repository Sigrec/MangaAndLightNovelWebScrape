namespace MangaAndLightNovelWebScrape;

internal static partial class InternalHelpersLogging
{
    #region Website data dump

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Website data entry: {Data}")]
    public static partial void WebsiteDataEntry(this ILogger logger, EntryModel data);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "{BookTitle} ({BookType}) Does Not Exist @ {Website}")]
    public static partial void WebsiteDataMissing(this ILogger logger, string bookTitle, BookType bookType, string website);

    #endregion

    #region Duplicate removal

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Duplicate input pair: {First} vs {Second}")]
    public static partial void DuplicateInputPair(this ILogger logger, EntryModel first, EntryModel second);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Removed duplicate {Entry}")]
    public static partial void RemovedDuplicate(this ILogger logger, EntryModel entry);

    #endregion
}
