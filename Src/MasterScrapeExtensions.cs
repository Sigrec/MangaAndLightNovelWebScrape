using MangaAndLightNovelWebScrape.Models;

namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Output extensions for <see cref="MasterScrape"/>. Renders the most recent scrape's
/// results (and optional links) to a logger, the console, or a file.
/// </summary>
public static class MasterScrapeExtensions
{
    /// <summary>
    /// Writes scrape results to the provided logger at the specified log level,
    /// either as plain entries or as a compact ASCII table.
    /// </summary>
    /// <param name="logger">The logger to which to write the results.</param>
    /// <param name="scrape">The <see cref="MasterScrape"/> whose results should be written.</param>
    /// <param name="logLevel">The log level at which to log the results.</param>
    /// <param name="isAsciiTable">
    ///   If <c>true</c>, logs results in ASCII-table format; otherwise, logs each entry as a separate log call.
    /// </param>
    /// <param name="title">The title of the series for the ASCII-table header.</param>
    /// <param name="bookType">The book format for the ASCII-table header.</param>
    /// <param name="includeLinks">Whether to include website links after the results. Defaults to <c>true</c>.</param>
    public static void PrintResults(
        this ILogger logger,
        MasterScrape scrape,
        LogLevel logLevel,
        bool isAsciiTable = false,
        string title = "",
        BookType bookType = BookType.Manga,
        bool includeLinks = true)
    {
        if (!logger.IsEnabled(logLevel))
        {
            return;
        }

        EntryModel[] results = scrape.GetResults().AsValueEnumerable().ToArray();

        if (results.Length == 0)
        {
#pragma warning disable CA2254
            logger.Log(logLevel, "No MasterData Available");
#pragma warning restore CA2254
            return;
        }

        if (isAsciiTable)
        {
#pragma warning disable CA2254
            logger.Log(logLevel, scrape.GetResultsAsAsciiTable(title, bookType, includeLinks));
#pragma warning restore CA2254
            return;
        }

        foreach (EntryModel entry in results)
        {
#pragma warning disable CA2254
            logger.Log(logLevel, entry.ToString());
#pragma warning restore CA2254
        }

        if (includeLinks)
        {
            foreach (KeyValuePair<Website, string> url in scrape.GetResultUrls())
            {
#pragma warning disable CA2254
                logger.Log(logLevel, "[" + url.Key + "," + url.Value + "]");
#pragma warning restore CA2254
            }
        }
    }

    /// <summary>
    /// Writes scrape results to the console, either as plain lines or as a compact ASCII table.
    /// </summary>
    /// <param name="scrape">The <see cref="MasterScrape"/> whose results should be printed.</param>
    /// <param name="isAsciiTable">
    ///   If <c>true</c>, prints results in ASCII-table format; otherwise, prints each entry on its own line.
    /// </param>
    /// <param name="title">The title of the series used for the ASCII-table header.</param>
    /// <param name="bookType">The book format used for the ASCII-table header.</param>
    /// <param name="includeLinks">Whether to include website links after the results. Defaults to <c>true</c>.</param>
    public static void PrintResultsToConsole(
        this MasterScrape scrape,
        bool isAsciiTable = false,
        string title = "",
        BookType bookType = BookType.Manga,
        bool includeLinks = true)
    {
        EntryModel[] results = scrape.GetResults().AsValueEnumerable().ToArray();

        if (results.Length == 0)
        {
            Console.WriteLine("No MasterData Available");
            return;
        }

        if (isAsciiTable)
        {
            Console.WriteLine(scrape.GetResultsAsAsciiTable(title, bookType, includeLinks));
            return;
        }

        // Build the full payload in one StringBuilder, then a single WriteLine —
        // avoids taking the Console.Out lock once per row.
        StringBuilder sb = new(results.Length * 64);
        foreach (EntryModel entry in results)
        {
            sb.AppendLine(entry.ToString());
        }

        if (includeLinks)
        {
            foreach (KeyValuePair<Website, string> url in scrape.GetResultUrls())
            {
                sb.Append('[').Append(url.Key).Append(',').Append(url.Value).AppendLine("]");
            }
        }

        Console.Write(sb.ToString());
    }

    /// <summary>
    /// Writes scrape results to a file, either as plain lines or as a compact ASCII table.
    /// </summary>
    /// <param name="scrape">The <see cref="MasterScrape"/> whose results should be written.</param>
    /// <param name="file">Path to the output file.</param>
    /// <param name="isAsciiTable">
    ///   If <c>true</c>, writes results in ASCII-table format; otherwise, writes each entry on its own line.
    /// </param>
    /// <param name="title">The title of the series used for the ASCII-table header.</param>
    /// <param name="bookType">The book format used for the ASCII-table header.</param>
    /// <param name="includeLinks">Whether to include website links after the results. Defaults to <c>true</c>.</param>
    public static void PrintResultsToFile(
        this MasterScrape scrape,
        string file,
        bool isAsciiTable = false,
        string title = "",
        BookType bookType = BookType.Manga,
        bool includeLinks = true)
    {
        EntryModel[] results = scrape.GetResults().AsValueEnumerable().ToArray();

        if (isAsciiTable)
        {
            File.WriteAllText(file, scrape.GetResultsAsAsciiTable(title, bookType, includeLinks));
            return;
        }

        using StreamWriter writer = new(file);
        if (results.Length == 0)
        {
            writer.WriteLine("No MasterData Available");
            return;
        }

        foreach (EntryModel entry in results)
        {
            writer.WriteLine(entry.ToString());
        }

        if (includeLinks)
        {
            foreach (KeyValuePair<Website, string> website in scrape.GetResultUrls())
            {
                if (!string.IsNullOrWhiteSpace(website.Value))
                {
                    writer.WriteLine("[" + website.Key + "," + website.Value + "]");
                }
            }
        }
    }
}
