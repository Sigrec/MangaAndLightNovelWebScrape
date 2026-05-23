namespace MangaAndLightNovelWebScrape;

internal static partial class TranslateAPILogging
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Waiting {Seconds} seconds for rate limit to reset")]
    public static partial void WaitingForRateLimit(this ILogger logger, short seconds);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "AniList GetSeriesByTitle w/ {Title} Request Failed -> {Message}")]
    public static partial void AniListRequestFailed(this ILogger logger, string title, string message);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "AniList Rate Remaining = {RateRemaining}")]
    public static partial void RateRemaining(this ILogger logger, short rateRemaining);
}
