using MangaAndLightNovelWebScrape.Models;
using System.Diagnostics;
using System.Collections.ObjectModel;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape;

/// <summary>
/// Debug mode is disabled by default
/// </summary>
public sealed partial class MasterScrape
{ 
    private readonly ConcurrentBag<List<EntryModel>> _masterDataList = [];
    private readonly ConcurrentDictionary<Website, string> _masterLinkDict = [];
    private readonly List<Task> _webTasks = new(15);

    // private WebDriver? PersistentWebDriver = null;

    /// <summary>
    /// The current region of the Scrape
    /// </summary>
    public Region Region { get; set; }
    /// <summary>
    /// The current browser of the Scrape
    /// </summary>
    public Browser Browser { get; set; }
    /// <summary>
    /// The current stock filter of the scrape
    /// </summary>
    public StockStatus[] Filter { get; set; } = StockStatusFilter.EXCLUDE_NONE_FILTER;
    public bool IsBooksAMillionMember { get; set; }
    public bool IsKinokuniyaUSAMember { get; set; }
    internal static bool IsWebDriverPersistent { get; set; } = false;
    /// <summary>
    /// Determines whether debug mode is enabled (Disabled by default)
    /// </summary>
    internal static bool IsDebugEnabled { get; set; } = false;

    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
    
    [GeneratedRegex(@"\d{1,3}(?:\.\d{1})?$")] internal static partial Regex FindVolNumRegex();
    [GeneratedRegex(@"Vol \d{1,3}(?:\.\d{1})?$")] internal static partial Regex FindVolWithNumRegex();
    [GeneratedRegex(@"\s{2,}|(?:--|\u2014)\s*| - ")] internal static partial Regex MultipleWhiteSpaceRegex();
    [GeneratedRegex(@"(?<=\b(?:Vol|Novel)\.?\s+(?:\d{1,3}|\d{1,3}\.\d{1}))[^\d.].*")] internal static partial Regex FinalCleanRegex();

    public MasterScrape(StockStatus[] Filter, Region Region = Region.America, Browser Browser = Browser.Edge, bool IsBooksAMillionMember = false, bool IsKinokuniyaUSAMember = false)
    {
        this.Filter = Filter;
        this.Region = Region;
        if (this.Region.IsMultiRegion())
        {
            LOGGER.Fatal("User inputted a multi region instead of a singular region");
            throw new NotSupportedException("Multi Region Scrape is not Supported");
        }
        this.Browser = Browser;
        this.IsBooksAMillionMember = IsBooksAMillionMember;
        this.IsKinokuniyaUSAMember = IsKinokuniyaUSAMember;
    }

    // /// <summary>
    // /// Enables a persistent WebDriver instance for subsequent scrapes.
    // /// If no driver exists, initializes one.
    // /// </summary>
    // /// <returns>The current <see cref="MasterScrape"/> instance.</returns>
    // public MasterScrape EnablePersistentWebDriver()
    // {
    //     LOGGER.Info("Enabling persistent WebDriver");
    //     IsWebDriverPersistent = true;

    //     PersistentWebDriver ??= SetupBrowserDriver(this.Browser, true);

    //     return this;
    // }

    // /// <summary>
    // /// Disables the persistent WebDriver so the driver will dispose/quit after every run.
    // /// </summary>
    // /// <returns>The current <see cref="MasterScrape"/> instance.</returns>
    // public MasterScrape DisablePersistentWebDriver()
    // {
    //     LOGGER.Info("Disabling persistent WebDriver");
    //     IsWebDriverPersistent = false;
    //     PersistentWebDriver?.Quit();
    //     PersistentWebDriver = null;

    //     return this;
    // }

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
    /// Returns the first set of scraped entries, or an empty sequence if no data exists.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{EntryModel}"/> of results.</returns>
    public IEnumerable<EntryModel> GetResults()
    {
        if (!_masterDataList.IsEmpty)
        {
            return _masterDataList.ElementAt(0);
        }

        return Enumerable.Empty<EntryModel>();
    }

    /// <summary>
    /// Gets the dictionary of final result URLs by site.
    /// </summary>
    /// <returns>
    /// A <see cref="Dictionary{Website,String}"/> mapping website names to their URLs.
    /// </returns>
    public Dictionary<Website, string> GetResultUrls()
    {
        return new Dictionary<Website, string>(_masterLinkDict);
    }

    /// <summary>
    /// Gets the titles of the currently selected membership sites as a read-only list.
    /// </summary>
    /// <returns>
    /// A <see cref="ReadOnlyCollection{T}"/> of <see cref="string"/> containing the title of each active membership site
    /// (e.g. <c>Books-A-Million</c>, <c>Kinokuniya USA</c>).
    /// </returns>
    public ReadOnlyCollection<string> GetMembershipsAsString()
    {
        List<string> memberships = new(3);

        if (IsBooksAMillionMember)
        {
            memberships.Add(BooksAMillion.TITLE);
        }

        if (IsKinokuniyaUSAMember)
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
        List<Website> memberships = new(3);

        if (IsBooksAMillionMember)
        {
            memberships.Add(Website.BooksAMillion);
        }

        if (IsKinokuniyaUSAMember)
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
        int longestTitle = "Title".Length;
        int longestPrice = "Price".Length;
        int longestStatus = "Status".Length;
        int longestWebsite = "Website".Length;

        foreach (EntryModel entry in results)
        {
            if (entry.Entry.Length > longestTitle) longestTitle = entry.Entry.Length;
            if (entry.Price.Length > longestPrice) longestPrice = entry.Price.Length;
            int statusLen = entry.StockStatus.ToString().Length;
            if (statusLen > longestStatus) longestStatus = statusLen;
            if (entry.Website.Length > longestWebsite) longestWebsite = entry.Website.Length;
        }

        string titlePad = "━".PadRight(longestTitle   + 2, '━');
        string pricePad = "━".PadRight(longestPrice   + 2, '━');
        string statusPad = "━".PadRight(longestStatus  + 2, '━');
        string websitePad = "━".PadRight(longestWebsite + 2, '━');

        string topBorder = $"┏{titlePad}┳{pricePad}┳{statusPad}┳{websitePad}┓";
        string headerRow = $"┃ {"Title".PadRight(longestTitle)} ┃ {"Price".PadRight(longestPrice)} ┃ {"Status".PadRight(longestStatus)} ┃ {"Website".PadRight(longestWebsite)} ┃";
        string midBorder = $"┣{titlePad}╋{pricePad}╋{statusPad}╋{websitePad}┫";
        string bottomBorder = $"┗{titlePad}┻{pricePad}┻{statusPad}┻{websitePad}┛";

        int estimatedRowLen = titlePad.Length + pricePad.Length + statusPad.Length + websitePad.Length + 10;
        StringBuilder sb = new StringBuilder(results.Length * estimatedRowLen + 200);

        sb.AppendLine()
        .Append("Title: \"")
        .Append(bookTitle)
        .AppendLine("\"")
        .Append("BookType: ")
        .Append(bookType.ToString())
        .AppendLine()
        .Append("Region: ")
        .Append(Region.ToString())
        .AppendLine();

        ReadOnlyCollection<string> membershipList = GetMembershipsAsString();
        if (membershipList.Count > 0)
        {
            sb.Append("Memberships: ")
            .Append(string.Join(", ", membershipList))
            .AppendLine();
        }

        sb.AppendLine(topBorder)
        .AppendLine(headerRow)
        .AppendLine(midBorder);

        foreach (EntryModel entry in results)
        {
            sb.Append("┃ ")
            .Append(entry.Entry.PadRight(longestTitle))
            .Append(" ┃ ")
            .Append(entry.Price.PadRight(longestPrice))
            .Append(" ┃ ")
            .Append(entry.StockStatus.ToString().PadRight(longestStatus))
            .Append(" ┃ ")
            .Append(entry.Website.PadRight(longestWebsite))
            .AppendLine(" ┃");
        }

        sb.Append(bottomBorder);

        if (includeLinks && !_masterLinkDict.IsEmpty)
        {
            sb.AppendLine()
            .AppendLine("Links:");
            foreach (KeyValuePair<Website, string> url in _masterLinkDict)
            {
                sb.Append(url.Key)
                .Append(" => ")
                .AppendLine(url.Value);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Writes scrape results to the console, either as plain lines or as a compact ASCII table.
    /// </summary>
    /// <param name="isAsciiTable">
    ///   If <c>true</c>, prints results in ASCII‑table format; otherwise, prints each entry on its own line.
    /// </param>
    /// <param name="title">The title of the series used for the ASCII‑table header.</param>
    /// <param name="bookType">The book format used for the ASCII‑table header.</param>
    /// <param name="includeLinks">
    ///   Whether to include website links after the results. Defaults to <c>true</c>.
    /// </param>
    /// <exception cref="ArgumentException">
    ///   Thrown if <paramref name="title"/> is null, empty, or whitespace when <paramref name="isAsciiTable"/> is <c>true</c>.
    /// </exception>
    public void PrintResultsToConsole(
        bool isAsciiTable = false,
        string title = "",
        BookType bookType = BookType.Manga,
        bool includeLinks = true)
    {
        EntryModel[] results = GetResults().AsValueEnumerable().ToArray();

        if (results.Length == 0)
        {
            Console.WriteLine("No MasterData Available");
            return;
        }

        if (isAsciiTable)
        {
            Console.WriteLine(GetResultsAsAsciiTable(title, bookType, includeLinks));
            return;
        }

        // Plain output
        foreach (EntryModel entry in results)
        {
            Console.WriteLine(entry.ToString());
        }

        if (includeLinks)
        {
            foreach (KeyValuePair<Website, string> url in GetResultUrls())
            {
                // compact "[key,value]" format
                Console.WriteLine("[" + url.Key + "," + url.Value + "]");
            }
        }
    }

    /// <summary>
    /// Writes scrape results to a file, either as plain lines or as a compact ASCII table.
    /// </summary>
    /// <param name="file">Path to the output file.</param>
    /// <param name="isAsciiTable">
    ///   If <c>true</c>, writes results in ASCII‑table format; otherwise, writes each entry on its own line.
    /// </param>
    /// <param name="title">The title of the series used for the ASCII‑table header.</param>
    /// <param name="bookType">The book format used for the ASCII‑table header.</param>
    /// <param name="includeLinks">
    ///   Whether to include website links after the results. Defaults to <c>true</c>.
    /// </param>
    /// <exception cref="ArgumentException">
    ///   Thrown if <paramref name="title"/> is null, empty, or whitespace when <paramref name="isAsciiTable"/> is <c>true</c>.
    /// </exception>
    public void PrintResultsToFile(
        string file,
        bool isAsciiTable = false,
        string title = "",
        BookType bookType = BookType.Manga,
        bool includeLinks = true)
    {
        EntryModel[] results = GetResults().AsValueEnumerable().ToArray();

        if (isAsciiTable)
        {
            // Entire table at once
            File.WriteAllText(file, GetResultsAsAsciiTable(title, bookType, includeLinks));
            return;
        }

        using StreamWriter writer = new(file);
        if (results.Length == 0)
        {
            writer.WriteLine("No MasterData Available");
            return;
        }

        // Plain output
        foreach (EntryModel entry in results)
        {
            writer.WriteLine(entry.ToString());
        }

        if (includeLinks)
        {
            foreach (KeyValuePair<Website, string> website in _masterLinkDict)
            {
                if (!string.IsNullOrWhiteSpace(website.Value))
                {
                    writer.WriteLine("[" + website.Key + "," + website.Value + "]");
                }
            }
        }
    }

    /// <summary>
    /// Writes scrape results to the provided logger at the specified log level,
    /// either as plain entries or as a compact ASCII table.
    /// </summary>
    /// <param name="UserLogger">The logger to which to write the results.</param>
    /// <param name="logLevel">The log level at which to log the results.</param>
    /// <param name="isAsciiTable">
    ///   If <c>true</c>, logs results in ASCII‑table format; otherwise, logs each entry as a separate log call.
    /// </param>
    /// <param name="title">The title of the series for the ASCII‑table header.</param>
    /// <param name="bookType">The book format for the ASCII‑table header.</param>
    /// <param name="includeLinks">
    ///   Whether to include website links after the results. Defaults to <c>true</c>.
    /// </param>
    public void PrintResultsToLogger(
        Logger UserLogger,
        LogLevel logLevel,
        bool isAsciiTable = false,
        string title = "",
        BookType bookType = BookType.Manga,
        bool includeLinks = true)
    {
        EntryModel[] results = GetResults().AsValueEnumerable().ToArray();

        Action<string> logAction = logLevel.Ordinal switch
        {
            0 => UserLogger.Trace,
            1 => UserLogger.Debug,
            2 => UserLogger.Info,
            3 => UserLogger.Warn,
            4 => UserLogger.Error,
            5 => UserLogger.Fatal,
            _ => UserLogger.Info
        };

        if (results.Length == 0)
        {
            logAction("No MasterData Available");
            return;
        }

        if (isAsciiTable)
        {
            logAction(GetResultsAsAsciiTable(title, bookType, includeLinks));
            return;
        }

        foreach (EntryModel entry in results)
        {
            logAction(entry.ToString());
        }

        if (includeLinks)
        {
            foreach (KeyValuePair<Website,string> url in _masterLinkDict)
            {
                logAction("[" + url.Key + "," + url.Value + "]");
            }
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
    private static List<EntryModel> PriceComparison(List<EntryModel> smallerList, List<EntryModel> biggerList)
    {
        List<EntryModel> finalData = []; // The final list of data containing all available volumes for the series from the website with the lowest price
        bool sameVolumeCheck; // Determines whether a match has been found where the 2 volumes are the same to compare prices for
        int nextVolPos = 0; // The position of the next volume and then proceeding volumes to check if there is a volume to compare
        double biggerListCurrentVolNum; // The current vol number from the website with the bigger list of volumes that is being checked
        // LOGGER.Debug($"Smaller -> {smallerList[0].Website} | Bigger -> {biggerList[0].Website}");

        foreach (EntryModel biggerListData in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(biggerList))
        {
            sameVolumeCheck = false; // Reset the check to determine if two volumes with the same number has been found to false
            biggerListCurrentVolNum = EntryModel.GetCurrentVolumeNum(biggerListData.Entry);

            if (nextVolPos != smallerList.Count) // Only need to check for a comparison if there are still volumes to compare in the "smallerList"
            {
                for (int y = nextVolPos; y < smallerList.Count; y++) // Check every volume in the smaller list, skipping over volumes that have already been checked
                {
                    // Check to see if the titles are not the same and they are not similar enough, or it is not new then go to the next volume
                    if (smallerList[y].Entry.Contains("Imperfect") || (!smallerList[y].Entry.Equals(biggerListData.Entry, StringComparison.OrdinalIgnoreCase) && InternalHelpers.Similar(smallerList[y].Entry, biggerListData.Entry, smallerList[y].Entry.Length > biggerListData.Entry.Length ? biggerListData.Entry.Length / 6 : smallerList[y].Entry.Length / 6) == -1))
                    {
                        // LOGGER.Debug($"Not The Same ({smallerList[y].Entry} | {biggerListData.Entry} | {!smallerList[y].Entry.Equals(biggerListData.Entry)} | {(InternalHelpers.Similar(smallerList[y].Entry, biggerListData.Entry, smallerList[y].Entry.Length > biggerListData.Entry.Length ? biggerListData.Entry.Length / 6 : smallerList[y].Entry.Length / 6) == -1)} | {smallerList[y].Entry.Contains("Imperfect")})");
                        continue;
                    }

                    // LOGGER.Debug($"MATCH? ({biggerListCurrentVolNum} = {(biggerListData.Entry.Contains("Box Set") ? EntryModel.GetCurrentVolumeNum(smallerList[y].Entry) : EntryModel.GetCurrentVolumeNum(smallerList[y].Entry))}) -> {biggerListCurrentVolNum == EntryModel.GetCurrentVolumeNum(smallerList[y].Entry)}");
                    // If the vol numbers are the same and the titles are similar or the same from the if check above, add the lowest price volume to the list
                    if (biggerListCurrentVolNum == EntryModel.GetCurrentVolumeNum(smallerList[y].Entry))
                    {
                        // LOGGER.Debug($"Found Match for {biggerListData} | {smallerList[y]}");
                        // LOGGER.Debug($"PRICE COMPARISON ({biggerListData.ParsePrice()} > {smallerList[y].ParsePrice()}) -> {biggerListData.ParsePrice() > smallerList[y].ParsePrice()}");
                        // Get the lowest price between the two then add the lowest dataset
                        if (biggerListData.ParsePrice() > smallerList[y].ParsePrice())
                        {
                            // LOGGER.Debug($"Add Match SmallerList {smallerList[y]}");
                            finalData.Add(smallerList[y]);
                        }
                        else
                        {
                            // LOGGER.Debug($"Add Match BiggerList {biggerListData}");
                            finalData.Add(biggerListData);
                        }
                        smallerList.RemoveAt(y);

                        nextVolPos = y; // Shift the position in which the next volumes to compare from the smaller list starts essentially "shrinking" the number of comparisons needed whenever a valid comparison is found by 1

                        sameVolumeCheck = true;
                        break;
                    }
                }
            }

            if (!sameVolumeCheck) // If the current volume number in the bigger list has no match in the smaller list (same volume number and name) then add it
            {
                // LOGGER.Debug($"Add No Match {biggerListData}");
                finalData.Add(biggerListData);
            }
        }

        // LOGGER.Debug("SmallerList Size = " + smallerList.Count);
        // Smaller list has volumes that are not present in the bigger list and are volumes that have a volume # greater than the greatest volume # in the bigger lis
        for (int x = 0; x < smallerList.Count; x++)
        {
            // LOGGER.Debug($"Add SmallerList Leftovers {smallerList[x]}");
            finalData.Add(smallerList[x]);
        }
        // finalData.ForEach(data => LOGGER.Info($"Final -> {data}"));
        finalData.Sort(EntryModel.VolumeSort);
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
    public async Task InitializeScrapeAsync(
        string title,
        BookType bookType,
        params HashSet<Website> siteList)
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

        IBrowser? browser = null;
        if (InternalHelpers.NeedPlaywright(siteList))
        {
            browser = await PlaywrightFactory.SetupPlaywrightBrowserAsync(this.Browser, false);
        }

        try
        {
            LOGGER.Info("Starting scrape for {Title} ({BookType}), against website(s) [{Websites}]", title, bookType, string.Join(',', siteList));
            LOGGER.Info("Region set to {0}", this.Region);
            LOGGER.Info("Running on {0} browser", this.Browser);

            // 1) Clear prior URLs
            _masterLinkDict.Clear();
            _masterDataList.Clear();

            // 2) Kick off the individual scraping tasks
            _webTasks.ScheduleScrapes(
                siteList,
                title,
                bookType,
                _masterDataList,
                _masterLinkDict,
                browser,
                this.Region,
                (this.IsBooksAMillionMember, this.IsKinokuniyaUSAMember)
            );
            await Task.WhenAll(_webTasks);
            _webTasks.Clear();

            // 3) Snapshot concurrent bag into a List<T>
            List<List<EntryModel>> currentLists = [.. _masterDataList];

            // 4) Remove any empty result‐sets
            currentLists.RemoveAll(inner => inner.Count == 0);

            // 5) Apply stock‑status filters
            if (this.Filter != StockStatusFilter.EXCLUDE_NONE_FILTER
                && currentLists.Count > 0)
            {
                LOGGER.Info("Applying stock filters");
                foreach (List<EntryModel> entryList in currentLists)
                {
                    entryList.RemoveAll(e =>
                        this.Filter.Contains(e.StockStatus) ||
                        e.StockStatus == StockStatus.NA);
                }
            }

            // 6) Price comparisons
            if (currentLists.Count > 1) // While there is still 2 or more lists of data to compare prices continue
            {
                LOGGER.Info("Starting price comparisons");
                int initialMasterDataListCount;
                List<Task<List<EntryModel>>> comparisonTasks = new(currentLists.Count / 2 + currentLists.Count);
                while (currentLists.Count > 1)
                {
                    currentLists.Sort((a, b) => a.Count.CompareTo(b.Count));
                    initialMasterDataListCount = currentLists.Count;
                    for (int i = 0; i < currentLists.Count - 1; i += 2)
                    {
                        List<EntryModel> smaller = currentLists[i];
                        List<EntryModel> larger = currentLists[i + 1];
                        comparisonTasks.Add(Task.Run(() => PriceComparison(smaller, larger)));
                    }

                    currentLists.AddRange(await Task.WhenAll(comparisonTasks));
                    currentLists.RemoveRange(0, initialMasterDataListCount % 2 == 0 ? initialMasterDataListCount : initialMasterDataListCount - 1);
                    comparisonTasks.Clear();
                }
            }

            _masterDataList.Clear();
            if (currentLists.Count > 0)
            {
                _masterDataList.Add(currentLists[0]);
            }
            currentLists.Clear();

            // 7) Optional debug dump
            if (IsDebugEnabled)
            {
                PrintResultsToLogger(
                    LOGGER,
                    LogLevel.Info,
                    true,
                    title,
                    bookType);
            }
        }
        catch (Exception ex)
        {
            LOGGER.Error(ex, "Unknown error thrown during scrape execution");
        }
        finally
        {
            if (browser is not null) await browser.CloseAsync();
        }
    }

    internal static async Task Main()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        MasterScrape scrape = new MasterScrape(
            Filter: StockStatusFilter.EXCLUDE_NONE_FILTER,
            Region: Region.Canada,
            Browser: Browser.Edge,
            IsBooksAMillionMember: false,
            IsKinokuniyaUSAMember: false)
        .EnableDebugMode();

        string title = "jujutsu kaisen";
        BookType bookType = BookType.Manga;

        await scrape.InitializeScrapeAsync(
            title: title,
            bookType: bookType,
            siteList: [Website.SciFier]);

        stopwatch.Stop();

        scrape.PrintResultsToConsole(
            isAsciiTable: true,
            title: title,
            bookType: bookType);

        LOGGER.Info(
            $"Elapsed time: {stopwatch.Elapsed.TotalSeconds:F3} seconds");
        Console.WriteLine(
            $"Elapsed time: {stopwatch.Elapsed.TotalSeconds:F3} seconds");
    }

    // private static void Main()
    // {
        
    // }
}