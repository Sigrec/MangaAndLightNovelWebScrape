namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Base type for exceptions thrown by the scrape pipeline. Consumers can use a single
/// <c>catch (ScrapeException)</c> to handle any failure originating inside the library,
/// or catch a derived type for more specific recovery (browser launch, single-site failure,
/// etc).
/// </summary>
public abstract class ScrapeException : Exception
{
    /// <summary>
    /// Initializes the base exception with a message. Used by derived types whose failure
    /// has no underlying cause (e.g. pre-flight skips).
    /// </summary>
    protected ScrapeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes the base exception with a message and the underlying exception that
    /// triggered the failure (e.g. the Playwright error, the HTTP error).
    /// </summary>
    protected ScrapeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
