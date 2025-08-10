using System.Net;

namespace MangaAndLightNovelWebScrape.Services;

/// <summary>
/// Provides a centralized factory for creating performance-tuned <see cref="HtmlWeb"/> and 
/// <see cref="HtmlDocument"/> instances optimized for high-speed HTML scraping.
/// <para>
/// Consolidates configuration for network requests and HTML parsing so all scraping code 
/// benefits from consistent, low-overhead settings. Key goals:
/// <list type="bullet">
///   <item>
///     <description>Reduce network latency with compression, connection tuning, and short timeouts.</description>
///   </item>
///   <item>
///     <description>Improve parsing speed by disabling unnecessary checks and enforcing UTF-8 decoding.</description>
///   </item>
///   <item>
///     <description>Support optional caching and cookie handling for session-aware scraping.</description>
///   </item>
///   <item>
///     <description>Prevent parser blow-ups by capping nested node depth.</description>
///   </item>
/// </list>
/// </para>
/// Designed for GET-heavy, stateless scraping where speed and low memory usage 
/// take priority over full HTTP feature support or detailed HTML error reporting.
/// </summary>
public static class HtmlFactory
{
    /// <summary>
    /// Creates a lightweight, performance-optimized <see cref="HtmlWeb"/> instance 
    /// preconfigured for high-speed scraping scenarios.
    /// <para>
    /// Key optimizations:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Disables automatic encoding detection to skip costly pre-parsing checks.</description>
    ///   </item>
    ///   <item>
    ///     <description>Forces UTF-8 decoding to avoid fallback conversions.</description>
    ///   </item>
    ///   <item>
    ///     <description>Enables <c>GZip</c> and <c>Deflate</c> decompression for reduced transfer size.</description>
    ///   </item>
    ///   <item>
    ///     <description>Sets aggressive timeouts to prevent hanging connections.</description>
    ///   </item>
    ///   <item>
    ///     <description>Turns off Nagle’s algorithm and the 100-Continue handshake for lower latency.</description>
    ///   </item>
    ///   <item>
    ///     <description>Optional caching and cookie usage to fit scraping needs.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// This factory is designed for GET-heavy, stateless scraping where 
    /// speed and low overhead are more important than full HTTP feature support.
    /// </summary>
    /// <param name="timeoutMs">Maximum time (in milliseconds) to wait for connections and reads before timing out.</param>
    /// <param name="useCache">If <c>true</c>, uses HtmlAgilityPack’s built-in cache if available.</param>
    /// <param name="useCookies">If <c>true</c>, enables cookies for the request session.</param>
    /// <returns>A configured <see cref="HtmlWeb"/> ready for scraping.</returns>
    public static HtmlWeb CreateWeb(int timeoutMs = 10_000, bool useCache = true, bool useCookies = false)
    {
        return new HtmlWeb
        {
            UsingCacheIfExists = useCache,
            AutoDetectEncoding = false, // avoid costly auto-detect pass
            OverrideEncoding = Encoding.UTF8, // consistent encoding handling
            UseCookies = useCookies,
            PreRequest = request =>
            {
                HttpWebRequest http = request;

                // Network perf tweaks
                http.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                http.KeepAlive = true;
                http.Timeout = timeoutMs;
                http.ReadWriteTimeout = timeoutMs; // covers stream read/write stalls
                http.AllowWriteStreamBuffering = false; // no buffering on send
                http.AllowAutoRedirect = true; // still follow redirects
                http.MaximumAutomaticRedirections = 3; // limit redirect loops
                http.ServicePoint.Expect100Continue = false; // skip 100-Continue handshake
                http.ServicePoint.UseNagleAlgorithm = false; // no Nagle delay for small requests

                return true;
            }
        };
    }

    /// <summary>
    /// Creates a lightweight, performance-optimized <see cref="HtmlDocument"/> instance
    /// preconfigured for typical scraping where HTML is well-formed enough to parse 
    /// without full syntax checking.
    /// <para>
    /// Recommended usage:
    /// <list type="bullet">
    ///   <item>
    ///     <description>When processing HTML fetched from trusted sources or cleaned before parsing.</description>
    ///   </item>
    ///   <item>
    ///     <description>When parsing speed is more important than catching every HTML error.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="maxNestedNodes">The maximum allowed nested child nodes to prevent parser blowups.</param>
    /// <returns>A configured <see cref="HtmlDocument"/> ready for parsing.</returns>
    public static HtmlDocument CreateDocument(int maxNestedNodes = 10_000)
    {
        return new HtmlDocument
        {
            OptionAutoCloseOnEnd = true,            // Auto-close unclosed tags at end of parsing
            OptionFixNestedTags = true,             // Attempt to fix obvious nesting issues
            OptionExtractErrorSourceText = false,   // Skip storing error source text (saves memory)
            OptionMaxNestedChildNodes = maxNestedNodes, // Prevent deep recursion bombs
            OptionCheckSyntax = false,              // Skip slow syntax validation
            OptionReadEncoding = false,             // Avoid re-checking encoding (already handled by HtmlWeb)
            OptionUseIdAttribute = true             // Keep fast lookup by ID enabled
        };
    }

    /// <summary>
    /// Applies performance-oriented parsing options to an existing <see cref="HtmlDocument"/> 
    /// instance, matching the configuration used by <see cref="CreateDocument"/>.
    /// <para>
    /// Recommended when you already have an <see cref="HtmlDocument"/> instance (e.g., 
    /// deserialized, pooled, or reused) but want to ensure it’s optimized for scraping.
    /// </para>
    /// </summary>
    /// <param name="doc">The <see cref="HtmlDocument"/> to configure.</param>
    /// <param name="maxNestedNodes">The maximum allowed nested child nodes to prevent parser blowups.</param>
    public static void ConfigurePerf(this HtmlDocument doc, int maxNestedNodes = 10_000)
    {
        doc.OptionAutoCloseOnEnd = true;            // Auto-close unclosed tags at end of parsing
        doc.OptionFixNestedTags = true;             // Attempt to fix obvious nesting issues
        doc.OptionExtractErrorSourceText = false;   // Skip storing error source text (saves memory)
        doc.OptionMaxNestedChildNodes = maxNestedNodes; // Prevent deep recursion bombs
        doc.OptionCheckSyntax = false;              // Skip slow syntax validation
        doc.OptionReadEncoding = false;             // Avoid re-checking encoding (already handled by HtmlWeb)
        doc.OptionUseIdAttribute = true;            // Keep fast lookup by ID enabled
    }
}