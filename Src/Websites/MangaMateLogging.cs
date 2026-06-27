namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class MangaMateLogging
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Max Page Num = {Num}")]
    public static partial void MaxPageNum(this ILogger logger, uint num);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Clicked AUD Currency")]
    public static partial void ClickedAudCurrency(this ILogger logger);
}
