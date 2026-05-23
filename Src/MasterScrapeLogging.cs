namespace MangaAndLightNovelWebScrape;

internal static partial class MasterScrapeLogging
{
    #region Scrape lifecycle

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "User inputted a multi region instead of a singular region")]
    public static partial void MultiRegionRejected(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting scrape for {Title} ({BookType}), against website(s) [{Websites}]")]
    public static partial void ScrapeStarting(this ILogger logger, string title, BookType bookType, string websites);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Region set to {Region}")]
    public static partial void RegionSet(this ILogger logger, Region region);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Running on {Browser} browser")]
    public static partial void BrowserSet(this ILogger logger, Browser browser);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Applying stock filters")]
    public static partial void ApplyingStockFilters(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting price comparisons")]
    public static partial void StartingPriceComparisons(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unknown error thrown during scrape execution")]
    public static partial void ScrapeExecutionFailed(this ILogger logger, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Elapsed time: {Seconds:F3} seconds")]
    public static partial void ElapsedTime(this ILogger logger, double seconds);

    #endregion
}
