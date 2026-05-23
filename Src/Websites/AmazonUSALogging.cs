namespace MangaAndLightNovelWebScrape.Websites;

internal static partial class AmazonUSALogging
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Titles} | {EntryInfo}")]
    public static partial void NodeCounts(this ILogger logger, string titles, string entryInfo);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "BEFORE ({Index}) = {Title} | {Info}")]
    public static partial void BeforeEntry(this ILogger logger, int index, string title, string info);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "No Valid Price, Entry Info for {Title} = {Info}")]
    public static partial void NoValidPrice(this ILogger logger, string title, string info);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Applying Coupon {Coupon} to {Title} for {Price}")]
    public static partial void ApplyingCoupon(this ILogger logger, decimal coupon, string title, string price);
}
