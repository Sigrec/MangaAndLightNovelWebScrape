namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Base type for exceptions thrown by the scrape pipeline. Consumers can use a single
/// <c>catch (ScrapeException)</c> to handle any failure originating inside the library,
/// or catch a derived type for more specific recovery (browser launch, single-site failure,
/// etc).
/// </summary>
public abstract class ScrapeException : Exception
{
    protected ScrapeException(string message)
        : base(message)
    {
    }

    protected ScrapeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
