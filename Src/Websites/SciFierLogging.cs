namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class SciFierLogging
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Ending Scrape Early -> '{LastFirstChar}' {Comparator} '{FirstEntryFirstChar}'")]
    public static partial void EndingScrapeEarly(this ILogger logger, char lastFirstChar, char comparator, char firstEntryFirstChar);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping Page -> '{FirstChar}' {Comparator} '{BookTitleFirstChar}' && '{LastChar}' {Comparator2} '{BookTitleFirstChar2}'")]
    public static partial void SkippingPage(this ILogger logger, char firstChar, char comparator, char bookTitleFirstChar, char lastChar, char comparator2, char bookTitleFirstChar2);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Checking desc for {Title}")]
    public static partial void CheckingDescription(this ILogger logger, string title);
}
