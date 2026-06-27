using MangaAndLightNovelWebScrape.Models;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Top-level entry point for the library. Holds scrape configuration
/// (<see cref="Region"/>, <see cref="Browser"/>, stock filter, memberships) and exposes
/// <see cref="InitializeScrapeAsync"/> to fan out per-site scrapes, merge results, and
/// dedup the cheapest copy of each volume across every retailer.
/// <para>
/// Designed to be constructed once and reused — every call to
/// <see cref="InitializeScrapeAsync"/> resets all per-run state. A single instance is NOT
/// safe to drive two scrapes concurrently.
/// </para>
/// </summary>
public sealed partial class MasterScrape
{
    private readonly ConcurrentBag<List<EntryModel>> _masterDataList = [];
    private readonly ConcurrentDictionary<Website, string> _masterLinkDict = [];
    private readonly ConcurrentDictionary<Website, Exception> _errors = [];
    private readonly List<Task> _webTasks = [with(15)];
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private List<EntryModel>? _finalResults;

    // Static delegate avoids the per-Sort allocation of a fresh Comparison<T> in the merge loop.
    private static readonly Comparison<List<EntryModel>> ByCount = static (a, b) => a.Count.CompareTo(b.Count);

    /// <summary>
    /// Geographic region the scrape runs against. Only the sites listed for this region by
    /// <see cref="Helpers.GetRegionWebsiteList(Region)"/> may be passed to
    /// <see cref="InitializeScrapeAsync"/>. Multi-region values (combined flags) are rejected.
    /// </summary>
    public Region Region { get; set; }
    /// <summary>
    /// Playwright browser channel used by sites that require JS rendering. HTML-only sites
    /// ignore this setting. Defaults to <see cref="Browser.Edge"/>.
    /// </summary>
    public Browser Browser { get; set; }
    /// <summary>
    /// Stock statuses to drop from the merged result set. Use one of the pre-built arrays
    /// on <see cref="MangaAndLightNovelWebScrape.Models.StockStatusFilter"/>, or supply a
    /// custom <c>StockStatus[]</c>. The default
    /// (<see cref="MangaAndLightNovelWebScrape.Models.StockStatusFilter.EXCLUDE_NONE_FILTER"/>)
    /// keeps everything except <see cref="StockStatus.NA"/>, which is always implicitly removed.
    /// </summary>
    public StockStatus[] Filter { get; set; } = StockStatusFilter.EXCLUDE_NONE_FILTER;
    /// <summary>
    /// Which member-only price columns the scraper should prefer when reading prices.
    /// Combine flags: <c>Membership.BooksAMillion | Membership.KinokuniyaUSA</c>.
    /// </summary>
    public Membership Memberships { get; set; }
    /// <summary>
    /// When <c>true</c>, <see cref="InitializeScrapeAsync"/> first probes every requested
    /// site via <see cref="IsSiteAvailableAsync"/> in parallel and silently drops any that
    /// don't respond. The omitted sites are recorded in <see cref="Errors"/> as
    /// <see cref="SiteUnavailableException"/> so the caller can still see what was skipped.
    /// Defaults to <c>false</c> — every site in the input list is dispatched and a
    /// down site costs its full scrape timeout.
    /// </summary>
    public bool SkipUnavailableSites { get; set; }
    /// <summary>
    /// Determines whether debug mode is enabled (Disabled by default)
    /// </summary>
    internal static bool IsDebugEnabled { get; set; } = false;

    [GeneratedRegex(@"\d{1,3}(?:\.\d{1})?$")] internal static partial Regex FindVolNumRegex();
    [GeneratedRegex(@"Vol \d{1,3}(?:\.\d{1})?$")] internal static partial Regex FindVolWithNumRegex();
    [GeneratedRegex(@"\s{2,}|(?:--|\u2014)\s*| - ")] internal static partial Regex MultipleWhiteSpaceRegex();
    [GeneratedRegex(@"(?<=\b(?:Vol|Novel)\.?\s+(?:\d{1,3}|\d{1,3}\.\d{1}))[^\d.].*")] internal static partial Regex FinalCleanRegex();

    /// <summary>
    /// Creates a new <see cref="MasterScrape"/> with the specified configuration.
    /// </summary>
    /// <param name="Filter">Stock statuses to drop from the merged result. Use one of the
    /// presets on <see cref="MangaAndLightNovelWebScrape.Models.StockStatusFilter"/>.</param>
    /// <param name="Region">Region the scrape runs against. Only sites supported by this
    /// region may be passed to <see cref="InitializeScrapeAsync"/>. Defaults to
    /// <see cref="Region.America"/>.</param>
    /// <param name="Browser">Playwright browser channel for JS-requiring sites. Defaults to
    /// <see cref="Browser.Edge"/>.</param>
    /// <param name="Memberships">Bit-flags marking sites where the caller has paid
    /// membership pricing. Combine with <c>|</c>. Defaults to <see cref="Membership.None"/>.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>. When null, all
    /// library logging is dropped (NullLogger). Provider-agnostic: works with Serilog,
    /// NLog, console, or any other Microsoft.Extensions.Logging-compatible provider.</param>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="Region"/> has
    /// more than one flag set.</exception>
    public MasterScrape(
        StockStatus[] Filter,
        Region Region = Region.America,
        Browser Browser = Browser.Edge,
        Membership Memberships = Membership.None,
        ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<MasterScrape>();

        this.Filter = Filter;
        this.Region = Region;
        if (this.Region.IsMultiRegion())
        {
            _logger.MultiRegionRejected();
            throw new NotSupportedException("Multi Region Scrape is not Supported");
        }
        this.Browser = Browser;
        this.Memberships = Memberships;
    }

    /// <summary>
    /// Disables debug mode, preventing debug outputs to files.
    /// </summary>
    /// <returns>The current <see cref="MasterScrape"/> instance.</returns>
    public MasterScrape DisableDebugMode()
    {
        IsDebugEnabled = false;
        return this;
    }

    /// <summary>
    /// Enables debug mode, allowing debug outputs to "Data" and "Logs" directories.
    /// Creates the directories if they do not already exist.
    /// </summary>
    /// <returns>The current <see cref="MasterScrape"/> instance.</returns>
    public MasterScrape EnableDebugMode()
    {
        IsDebugEnabled = true;

        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string dataPath = Path.Combine(basePath, "Data");
        string logsPath = Path.Combine(basePath, "Logs");

        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        if (!Directory.Exists(logsPath))
        {
            Directory.CreateDirectory(logsPath);
        }

        return this;
    }

    /// <summary>
    /// Returns the final merged results from the most recent scrape, or an empty list if no
    /// scrape has completed. The returned reference is read-only and supports indexed access
    /// (<c>results.Count</c>, <c>results[i]</c>) without a forced enumeration.
    /// </summary>
    public IReadOnlyList<EntryModel> GetResults() =>
        _finalResults is { } results ? results : Array.Empty<EntryModel>();

    /// <summary>
    /// Per-site failures from the most recent <see cref="InitializeScrapeAsync"/> call. Empty if
    /// every site completed successfully. A single site failure is recorded here as a
    /// <see cref="SiteScrapeException"/> and does <em>not</em> abort the rest of the scrape;
    /// catastrophic failures (browser launch, cancellation) propagate as thrown exceptions instead.
    /// </summary>
    /// <example>
    /// <code>
    /// await scrape.InitializeScrapeAsync(title, bookType, sites);
    /// foreach ((Website site, Exception ex) in scrape.Errors)
    /// {
    ///     Console.WriteLine($"{site} failed: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    public IReadOnlyDictionary<Website, Exception> Errors => _errors;

    /// <summary>
    /// Reachability probe for a single <see cref="Website"/>. Returns <c>true</c> when
    /// the site's host resolves and the server answers — including auth-gated (4xx) and
    /// CDN-challenged (Cloudflare) responses. DNS failures, connection refusals, timeouts,
    /// and 5xx responses return <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Useful as a pre-flight before <see cref="InitializeScrapeAsync"/> when you want to
    /// skip a known-down site instead of waiting on the scrape's per-site timeout.
    /// </remarks>
    public Task<bool> IsSiteAvailableAsync(Website site, CancellationToken cancellationToken = default)
        => Services.SiteHealth.IsReachableAsync(Helpers.GetWebsiteLink(site, this.Region), cancellationToken);

    /// <summary>
    /// Batched reachability probe for the supplied sites. Runs each check in parallel
    /// and returns a snapshot of which sites are currently reachable.
    /// </summary>
    public async Task<IReadOnlyDictionary<Website, bool>> CheckSitesAvailableAsync(
        IEnumerable<Website> sites,
        CancellationToken cancellationToken = default)
    {
        Website[] siteArray = [.. sites];
        Task<bool>[] checks = new Task<bool>[siteArray.Length];
        for (int i = 0; i < siteArray.Length; i++)
        {
            checks[i] = IsSiteAvailableAsync(siteArray[i], cancellationToken);
        }
        bool[] results = await Task.WhenAll(checks);
        Dictionary<Website, bool> map = new(siteArray.Length);
        for (int i = 0; i < siteArray.Length; i++)
        {
            map[siteArray[i]] = results[i];
        }
        return map;
    }

    /// <summary>
    /// Returns the per-site URLs the most recent scrape visited.
    /// </summary>
    /// <remarks>
    /// The returned reference is live — subsequent <see cref="InitializeScrapeAsync"/> calls
    /// clear and repopulate it. Copy via
    /// <c>new Dictionary&lt;Website, string&gt;(scrape.GetResultUrls())</c> if you need a stable
    /// snapshot across scrapes.
    /// </remarks>
    public IReadOnlyDictionary<Website, string> GetResultUrls() => _masterLinkDict;

    /// <summary>
    /// Gets the titles of the currently selected membership sites as a read-only list.
    /// </summary>
    /// <returns>
    /// A <see cref="ReadOnlyCollection{T}"/> of <see cref="string"/> containing the title of each active membership site
    /// (e.g. <c>Books-A-Million</c>, <c>Kinokuniya USA</c>).
    /// </returns>
    public ReadOnlyCollection<string> GetMembershipsAsString()
    {
        List<string> memberships = new(2);

        if (Memberships.HasFlag(Membership.BooksAMillion))
        {
            memberships.Add(BooksAMillion.TITLE);
        }

        if (Memberships.HasFlag(Membership.KinokuniyaUSA))
        {
            memberships.Add(KinokuniyaUSA.TITLE);
        }

        return memberships.AsReadOnly();
    }

    /// <summary>
    /// Gets the set of currently selected membership site enums as a read-only list.
    /// </summary>
    /// <returns>
    /// A <see cref="ReadOnlyCollection{Website}"/> containing each active membership enum
    /// (Website.BooksAMillion, Website.KinokuniyaUSA).
    /// </returns>
    public ReadOnlyCollection<Website> GetMembershipsAsEnum()
    {
        List<Website> memberships = new(2);

        if (Memberships.HasFlag(Membership.BooksAMillion))
        {
            memberships.Add(Website.BooksAMillion);
        }

        if (Memberships.HasFlag(Membership.KinokuniyaUSA))
        {
            memberships.Add(Website.KinokuniyaUSA);
        }

        return memberships.AsReadOnly();
    }

    /// <summary>
    /// Gets the results of a scrape and formats them as an ASCII table.
    /// The table includes columns for Title, Price, Status, and Website, and optionally appends the links at the bottom.
    /// </summary>
    /// <param name="bookTitle">The title used for this scrape. Must be non-empty.</param>
    /// <param name="bookType">The format used for this scrape (e.g., Manga or Light Novel).</param>
    /// <param name="includeLinks">Whether to append the scraped URLs after the table.</param>
    /// <returns>A string containing the formatted ASCII table, including header info and optional links.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="bookTitle"/> is null, empty, or whitespace.</exception>
    public string GetResultsAsAsciiTable(string bookTitle, BookType bookType, bool includeLinks = true)
    {
        if (string.IsNullOrWhiteSpace(bookTitle))
        {
            throw new ArgumentException("Title can't be empty.", nameof(bookTitle));
        }

        EntryModel[] results = GetResults().AsValueEnumerable().ToArray();

        // Single pass: measure widths AND cache StockStatus name strings. The enum-name
        // lookup allocates a string per call, so without caching we'd do it 2x per entry
        // (once for width, once for render). Cache once, use twice.
        int longestTitle = "Title".Length;
        int longestPrice = "Price".Length;
        int longestStatus = "Status".Length;
        int longestWebsite = "Website".Length;
        string[] statusNames = new string[results.Length];

        for (int i = 0; i < results.Length; i++)
        {
            EntryModel entry = results[i];
            string statusName = entry.StockStatus.ToString();
            statusNames[i] = statusName;
            if (entry.Entry.Length > longestTitle) longestTitle = entry.Entry.Length;
            if (entry.Price.Length > longestPrice) longestPrice = entry.Price.Length;
            if (statusName.Length > longestStatus) longestStatus = statusName.Length;
            if (entry.Website.Length > longestWebsite) longestWebsite = entry.Website.Length;
        }

        // Borders are one-shot — plain string ops are fine here.
        string titlePad = new('━', longestTitle + 2);
        string pricePad = new('━', longestPrice + 2);
        string statusPad = new('━', longestStatus + 2);
        string websitePad = new('━', longestWebsite + 2);

        string topBorder = $"┏{titlePad}┳{pricePad}┳{statusPad}┳{websitePad}┓";
        string midBorder = $"┣{titlePad}╋{pricePad}╋{statusPad}╋{websitePad}┫";
        string bottomBorder = $"┗{titlePad}┻{pricePad}┻{statusPad}┻{websitePad}┛";

        int estimatedRowLen = titlePad.Length + pricePad.Length + statusPad.Length + websitePad.Length + 10;
        StringBuilder sb = new(results.Length * estimatedRowLen + 200);

        sb.AppendLine()
            .Append("Title: \"").Append(bookTitle).AppendLine("\"")
            .Append("BookType: ").Append(bookType.ToString()).AppendLine()
            .Append("Region: ").Append(Region.ToString()).AppendLine();

        ReadOnlyCollection<string> membershipList = GetMembershipsAsString();
        if (membershipList.Count > 0)
        {
            sb.Append("Memberships: ")
                .Append(string.Join(", ", membershipList))
                .AppendLine();
        }

        sb.AppendLine(topBorder);

        // Header row — build with Append + fill rather than PadRight (no extra alloc per cell).
        AppendCell(sb, "Title", longestTitle);
        AppendCell(sb, "Price", longestPrice);
        AppendCell(sb, "Status", longestStatus);
        AppendCell(sb, "Website", longestWebsite, last: true);

        sb.AppendLine(midBorder);

        for (int i = 0; i < results.Length; i++)
        {
            EntryModel entry = results[i];
            AppendCell(sb, entry.Entry, longestTitle);
            AppendCell(sb, entry.Price, longestPrice);
            AppendCell(sb, statusNames[i], longestStatus);
            AppendCell(sb, entry.Website, longestWebsite, last: true);
        }

        sb.Append(bottomBorder);

        if (includeLinks && !_masterLinkDict.IsEmpty)
        {
            sb.AppendLine().AppendLine("Links:");
            foreach (KeyValuePair<Website, string> url in _masterLinkDict)
            {
                sb.Append(url.Key.ToString())
                    .Append(" => ")
                    .AppendLine(url.Value);
            }
        }

        return sb.ToString();

        // PadRight allocates a fresh padded string per call (~4 per row × N rows). This local
        // appends the value then pads with raw spaces straight into the SB instead.
        static void AppendCell(StringBuilder sb, string value, int width, bool last = false)
        {
            sb.Append("┃ ").Append(value).Append(' ', width - value.Length);
            if (last) sb.AppendLine(" ┃");
            else sb.Append(' ');
        }
    }

    /// <summary>
    /// <br>Compares the prices of all the volumes that the two websites both have, and outputs the resulting list containing </br>
    /// <br>the lowest prices for each available volume between the websites. If one website does not have a volume that the other</br>
    /// <br>does then that volumes data set defaults to the "smallest" and is added to the list.</br>
    /// </summary>
    /// <param name="smallerList">The smaller list of data sets between the two websites</param>
    /// <param name="biggerList">The bigger list of data sets between the two websites</param>
    /// <param name="bookTitle">The initial title inputted by the user used to determine if the titles in the lists "match"</param>
    /// <returns>The final list of data containing all available lowest price volumes between the two websites</returns>
    /// <summary>
    /// Merges two per-site result lists by volume number, keeping the cheaper price when both
    /// sites carry the same volume. Volumes only one side carries are passed through.
    /// </summary>
    /// <param name="sortResult">
    /// When <c>true</c> (default), the returned list is sorted by <see cref="EntryModel.VolumeSort"/>.
    /// Internal callers chaining multiple merge rounds pass <c>false</c> to skip the intermediate
    /// sort — the final caller does one sort at the end instead of O(log N) sorts of growing lists.
    /// </param>
    /// <remarks>
    /// The merge itself is order-independent — keyed by parsed volume number via a dictionary
    /// — so a tweak to the sort doesn't silently change the merge outcome.
    ///
    /// Complexity: <c>O(N + M)</c> average over the smaller-list build and the bigger-list scan.
    /// Worst case <c>O(N · M)</c> only when the volume key is unparseable (returns -1) or is a
    /// Box Set (also -1) for every entry — that bucket degenerates to a linear scan.
    /// </remarks>
    internal static List<EntryModel> PriceComparison(List<EntryModel> smallerList, List<EntryModel> biggerList, bool sortResult = true)
    {
        int smallerCount = smallerList.Count;
        int biggerCount = biggerList.Count;
        List<EntryModel> finalData = new(biggerCount + smallerCount);

        // Fast-path: nothing to compare against — just emit biggerList as-is.
        if (smallerCount == 0)
        {
            foreach (EntryModel entry in CollectionsMarshal.AsSpan(biggerList))
            {
                finalData.Add(entry);
            }
            if (sortResult) finalData.SortByVolume();
            return finalData;
        }

        Span<EntryModel> smallerSpan = CollectionsMarshal.AsSpan(smallerList);

        // Cache regex/parse outputs for the smaller list once.
        bool[] consumed = new bool[smallerCount];
        decimal[] smallerPrices = new decimal[smallerCount];
        bool[] smallerHasImperfect = new bool[smallerCount];

        // Index from vol-number → smaller-list index of the *first* entry with that vol.
        // For the rare case of multiple entries sharing a vol (Box Sets all hash to -1, and so
        // do unparseable titles), `nextSameVol[i]` links to the next entry sharing the bucket
        // (or -1 if none) — i.e. a flat-array chained hashmap instead of a Dictionary-of-List.
        // The win: most volumes are unique, so we skip allocating a List<int> per bucket.
        Dictionary<double, int> volHead = new(smallerCount);
        int[] nextSameVol = new int[smallerCount];

        // Walk in reverse so the chain ends up in ascending-index order — matches the old
        // positional iteration semantics (first inserted wins when multiple match).
        for (int i = smallerCount - 1; i >= 0; i--)
        {
            double vol = EntryModel.GetCurrentVolumeNum(smallerSpan[i].Entry);
            smallerPrices[i] = smallerSpan[i].ParsePrice();
            smallerHasImperfect[i] = smallerSpan[i].Entry.Contains("Imperfect");

            nextSameVol[i] = volHead.TryGetValue(vol, out int currentHead) ? currentHead : -1;
            volHead[vol] = i;
        }

        foreach (EntryModel biggerEntry in CollectionsMarshal.AsSpan(biggerList))
        {
            double biggerVol = EntryModel.GetCurrentVolumeNum(biggerEntry.Entry);
            decimal biggerPrice = biggerEntry.ParsePrice();
            bool matched = false;

            if (volHead.TryGetValue(biggerVol, out int candidateIdx))
            {
                while (candidateIdx != -1)
                {
                    if (!consumed[candidateIdx] && !smallerHasImperfect[candidateIdx])
                    {
                        EntryModel smaller = smallerSpan[candidateIdx];
                        int similarityThreshold = smaller.Entry.Length > biggerEntry.Entry.Length
                            ? biggerEntry.Entry.Length / 6
                            : smaller.Entry.Length / 6;

                        if (smaller.Entry.Equals(biggerEntry.Entry, StringComparison.OrdinalIgnoreCase) ||
                            InternalHelpers.Similar(smaller.Entry, biggerEntry.Entry, similarityThreshold) != -1)
                        {
                            finalData.Add(biggerPrice > smallerPrices[candidateIdx] ? smaller : biggerEntry);
                            consumed[candidateIdx] = true;
                            matched = true;
                            break;
                        }
                    }

                    candidateIdx = nextSameVol[candidateIdx];
                }
            }

            if (!matched)
            {
                finalData.Add(biggerEntry);
            }
        }

        // Append the smaller-list entries we never matched. These are volumes the bigger list
        // doesn't carry (or were filtered by the "Imperfect" gate).
        for (int x = 0; x < smallerCount; x++)
        {
            if (!consumed[x])
            {
                finalData.Add(smallerSpan[x]);
            }
        }

        if (sortResult) finalData.SortByVolume();
        return finalData;
    }

    /// <summary>
    /// Generates a set of <see cref="Website"/> enums based on provided title strings
    /// and a target region. Unrecognized or out‑of‑region titles are ignored.
    /// </summary>
    /// <param name="input">
    ///   An enumerable of website title strings to map. May be null or empty.
    /// </param>
    /// <param name="curRegion">
    ///   The region for which to validate and map websites.
    /// </param>
    /// <returns>
    ///   A <see cref="HashSet{Website}"/> containing all matched and region‑valid
    ///   <see cref="Website"/> values, or an empty set if <paramref name="input"/>
    ///   is null, empty, or contains no valid titles.
    /// </returns>
    public static HashSet<Website> GenerateWebsiteList(IEnumerable<string> input, Region curRegion)
    {
        if (input == null)
        {
            return [];
        }

        if (!Helpers.WebsitesByRegion.TryGetValue(curRegion, out Website[]? validWebsites))
        {
            return [];
        }

        HashSet<Website> websiteList = [];
        foreach (string raw in input)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            string key = raw.Trim();
            if (Helpers.WebsiteTitleMap.TryGetValue(key, out Website site)
                && Array.IndexOf(validWebsites, site) >= 0)
            {
                websiteList.Add(site);
            }
        }

        return websiteList;
    }

    /// <summary>
    /// Initializes and runs the web‐scrape, populating the master data and URL dictionaries.
    /// Applies optional stock filters, compares prices across sites, and prepares the final results.
    /// </summary>
    /// <param name="title">
    ///   The series title to search for. Must not be null, empty, or whitespace.
    /// </param>
    /// <param name="bookType">
    ///   The type of book (e.g. Manga or Light Novel) to scrape.
    /// </param>
    /// <param name="webScrapeList">
    ///   A set of <see cref="Website"/> enums indicating which sites to query.
    ///   All sites must be valid for the current <see cref="Region"/>.
    /// </param>
    /// <returns>A <see cref="Task"/> that completes when scraping finishes.</returns>
    /// <exception cref="ArgumentException">
    ///   Thrown if any requested site does not support the current region,
    ///   or if <paramref name="title"/> is null/empty when used in ASCII output.
    /// </exception>
    /// <summary>
    /// Runs the scrape against the supplied sites and stores the results internally — read them
    /// via <see cref="GetResults"/> and <see cref="GetResultUrls"/>, and inspect per-site failures
    /// via <see cref="Errors"/>.
    /// </summary>
    /// <param name="title">Series title to search for. Must not be null/whitespace.</param>
    /// <param name="bookType">Manga or LightNovel.</param>
    /// <param name="cancellationToken">
    /// Cancels the scrape between major checkpoints (site fan-out, merge rounds). Note: an
    /// already-in-flight network call inside a site cannot be interrupted mid-request because
    /// HtmlWeb / older Playwright .NET don't accept cancellation tokens for the underlying I/O.
    /// </param>
    /// <param name="siteList">Sites to scrape. All must be valid for the current Region.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="title"/> is empty/whitespace, or one or more sites are invalid for the
    /// current <see cref="Region"/>.
    /// </exception>
    /// <exception cref="ScrapeBrowserLaunchException">
    /// Thrown when any requested site needs Playwright and the browser fails to launch — the
    /// underlying Playwright error is preserved as <c>InnerException</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled.
    /// </exception>
    public async Task InitializeScrapeAsync(
        string title,
        BookType bookType,
        HashSet<Website> siteList,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or whitespace.", nameof(title));
        }

        if (!Helpers.IsWebsiteListValid(this.Region, siteList))
        {
            string siteListString = string.Join(", ", siteList);
            string plural = siteList.Count > 1 ? "s" : string.Empty;
            throw new ArgumentException(
                $"Website{plural} [{siteListString}] not supported in region \"{this.Region}\".");
        }

        cancellationToken.ThrowIfCancellationRequested();
        _errors.Clear();

        // Pre-flight reachability check. Skip down sites instead of waiting on the
        // scrape's per-site timeout. Skipped sites are recorded in Errors with a
        // SiteUnavailableException so the caller can still tell what happened.
        if (this.SkipUnavailableSites && siteList.Count > 0)
        {
            IReadOnlyDictionary<Website, bool> reach = await CheckSitesAvailableAsync(siteList, cancellationToken);
            HashSet<Website> reachable = [];
            foreach ((Website site, bool isUp) in reach)
            {
                if (isUp)
                {
                    reachable.Add(site);
                }
                else
                {
                    _errors[site] = new SiteUnavailableException(site);
                    _logger.SiteSkippedUnavailable(site);
                }
            }

            if (reachable.Count == 0)
            {
                _masterDataList.Clear();
                _masterLinkDict.Clear();
                _finalResults = null;
                return;
            }
            siteList = reachable;
            cancellationToken.ThrowIfCancellationRequested();
        }

        PlaywrightSession? session = null;
        if (InternalHelpers.NeedPlaywright(siteList))
        {
            try
            {
                session = await PlaywrightFactory.SetupPlaywrightBrowserAsync(this.Browser);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new ScrapeBrowserLaunchException(ex);
            }
        }

        try
        {
            _logger.ScrapeStarting(title, bookType, string.Join(',', siteList));
            _logger.RegionSet(this.Region);
            _logger.BrowserSet(this.Browser);

            // 1) Clear prior URLs / cached results
            _masterLinkDict.Clear();
            _masterDataList.Clear();
            _finalResults = null;

            // 2) Kick off the individual scraping tasks
            _webTasks.ScheduleScrapes(
                siteList,
                title,
                bookType,
                _masterDataList,
                _masterLinkDict,
                _errors,
                session?.Browser,
                this.Region,
                _loggerFactory,
                this.Memberships,
                cancellationToken
            );
            // .WaitAsync forwards cancellation without leaving the underlying scrape tasks
            // unobserved — they continue running but we stop waiting on them.
            await Task.WhenAll(_webTasks).WaitAsync(cancellationToken);
            _webTasks.Clear();

            cancellationToken.ThrowIfCancellationRequested();

            // 3) Snapshot concurrent bag into a List<T>
            List<List<EntryModel>> currentLists = [.. _masterDataList];

            // 4) Remove any empty result‐sets
            currentLists.RemoveAll(inner => inner.Count == 0);

            // 5) Apply stock‑status filters
            if (this.Filter != StockStatusFilter.EXCLUDE_NONE_FILTER
                && currentLists.Count > 0)
            {
                _logger.ApplyingStockFilters();
                // Pack the filter set + the always-excluded NA into a single uint bitmask.
                // Per-entry check becomes one shift-and instead of a linear Array.IndexOf scan.
                // (StockStatus has 6 values fitting in 0-5, well inside uint's 32 bits.)
                uint filterMask = 1u << (int)StockStatus.NA;
                foreach (StockStatus status in this.Filter)
                {
                    filterMask |= 1u << (int)status;
                }

                foreach (List<EntryModel> entryList in currentLists)
                {
                    entryList.RemoveAll(e => (filterMask & (1u << (int)e.StockStatus)) != 0);
                }
            }

            // 6) Price comparisons
            if (currentLists.Count > 1) // While there is still 2 or more lists of data to compare prices continue
            {
                _logger.StartingPriceComparisons();
                int initialMasterDataListCount;
                List<Task<List<EntryModel>>> comparisonTasks = new(currentLists.Count / 2);
                while (currentLists.Count > 1)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentLists.Sort(ByCount);
                    initialMasterDataListCount = currentLists.Count;
                    for (int i = 0; i < currentLists.Count - 1; i += 2)
                    {
                        List<EntryModel> smaller = currentLists[i];
                        List<EntryModel> larger = currentLists[i + 1];
                        // Intermediate merges skip their internal sort — the dict-based merge
                        // doesn't need sorted inputs, and we sort the final result once below.
                        comparisonTasks.Add(Task.Run(() => PriceComparison(smaller, larger, sortResult: false), cancellationToken));
                    }

                    currentLists.AddRange(await Task.WhenAll(comparisonTasks).WaitAsync(cancellationToken));
                    currentLists.RemoveRange(0, initialMasterDataListCount % 2 == 0 ? initialMasterDataListCount : initialMasterDataListCount - 1);
                    comparisonTasks.Clear();
                }
            }

            // The per-site collectors are no longer needed — stash the merged result.
            _masterDataList.Clear();
            _finalResults = currentLists.Count > 0 ? currentLists[0] : null;
            _finalResults?.SortByVolume();
            currentLists.Clear();

            // 7) Optional debug dump
            if (IsDebugEnabled)
            {
                _logger.PrintResults(
                    this,
                    LogLevel.Information,
                    true,
                    title,
                    bookType);
            }
        }
        catch (Exception ex) when (LogAndRethrow(ex))
        {
            // Unreachable — LogAndRethrow always returns false. The when-filter lets us log
            // without swallowing: exception continues propagating to the caller.
            throw;
        }
        finally
        {
            if (session is not null) await session.DisposeAsync();
        }

        bool LogAndRethrow(Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                _logger.ScrapeExecutionFailed(ex);
            }
            return false;
        }
    }

}