using System.Buffers;
using System.Collections.Frozen;
using System.Runtime.InteropServices;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape;

internal static partial class InternalHelpers
{
    [GeneratedRegex(@"[^\w+]")] internal static partial Regex RemoveNonWordsRegex();
    [GeneratedRegex(@"Vol\s\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex VolRegex();

    private const int StackallocThreshold = 512;

    private static readonly FrozenSet<string> _entryRemovalTerms = new[]
    {
        "Bluray", "Blu-ray", "Choose Path", "Encyclopedia", "Anthology", "Official", "Character", "Guide",
        "Illustration", "Anime Profiles", "Choose Your Path", "Compendium",
        "Artbook", "Art Book", "Error", "Advertising", "(Osi)", "Ani-manga",
        "Anime", "Bilingual", "Game Book", "Theatrical", "Figure", "SEGA",
        "Poster", "Statue", "IMPORT", "Trace", "Bookmarks", "Music Book",
        "Retrospective", "Notebook", "Journal", "Art of", "the Anime",
        "Calendar", "Adventure Book", "Coloring Book", "Sketchbook", "PLUSH",
        "Pirate Recipes", "Exclusive", "Hobby", "Model Kit", "Funko POP", "Creator of the", "the Movie", "UniVersus"
    }
    .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal static bool NeedPlaywright(HashSet<Website> siteList)
    {
        return siteList.ContainsAny([ Website.KinokuniyaUSA, Website.BooksAMillion, Website.AmazonUSA, Website.MerryManga, Website.ForbiddenPlanet, Website.MangaMate, Website.MangaMart, Website.AmazonJapan ]);
    }

    /// <summary>
    /// Determines whether the given <paramref name="title"/> contains any of the
    /// predefined removal terms or any user‑supplied additional terms.
    /// </summary>
    /// <param name="title">
    ///   The string to inspect. If null, empty, or whitespace, returns <c>false</c>.
    /// </param>
    /// <param name="additionalTerms">
    ///   Optional extra substrings that, if found in <paramref name="title"/>, also trigger removal.
    /// </param>
    /// <returns>
    ///   <c>true</c> if any term (built‑in or additional) is found; otherwise <c>false</c>.
    /// </returns>
    internal static bool ShouldRemoveEntry(
        string title,
        IEnumerable<string>? additionalTerms = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        // Combine built‑in and additional terms
        IEnumerable<string> combinedTerms;
        if (additionalTerms != null)
        {
            combinedTerms = _entryRemovalTerms.Concat(additionalTerms);
        }
        else
        {
            combinedTerms = _entryRemovalTerms;
        }

        // Full substring scan (case‐insensitive)
        foreach (string term in combinedTerms)
        {
            if (title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Schedules scraping tasks for each <paramref name="site"/> in <paramref name="sites"/>,
    /// adding them to <paramref name="webTasks"/>. A fresh scraper instance is created per task,
    /// using the optional <paramref name="browser"/>. Scrape results are stored in <paramref name="masterBag"/>
    /// and <paramref name="masterDict"/>.
    /// </summary>
    /// <param name="webTasks">The list to which each scrape <see cref="Task"/> is added.</param>
    /// <param name="sites">The websites to scrape.</param>
    /// <param name="bookTitle">The series title to search for.</param>
    /// <param name="bookType">The book type (Manga or Light Novel).</param>
    /// <param name="masterBag">Thread-safe bag to collect each site's results.</param>
    /// <param name="masterDict">Thread-safe dictionary mapping each site to its scraped data.</param>
    /// <param name="memberships">
    ///   Tuple of membership flags for Books-A-Million & Kinokuniya USA; affects site-specific logic.
    /// </param>
    /// <param name="browser">Optional <see cref="Browser"/> instance to use for scraping, or null for default.</param>
    internal static void ScheduleScrapes(
        this List<Task> webTasks,
        IEnumerable<Website> sites,
        string bookTitle,
        BookType bookType,
        ConcurrentBag<List<EntryModel>> masterBag,
        ConcurrentDictionary<Website, string> masterDict,
        ConcurrentDictionary<Website, Exception> errors,
        IBrowser? browser,
        Region curRegion,
        ILoggerFactory loggerFactory,
        Membership memberships,
        CancellationToken cancellationToken)
    {
        foreach (Website site in sites)
        {
            IWebsite scraper = CreateScraper(site, loggerFactory);

            Task task = scraper.CreateTask(
                bookTitle,
                bookType,
                masterBag,
                masterDict,
                errors,
                browser,
                curRegion,
                memberships,
                cancellationToken
            );

            webTasks.Add(task);
        }
    }

    /// <summary>
    /// Rents a fresh Playwright <see cref="IPage"/>, runs <paramref name="scraper"/>'s
    /// <c>GetData</c>, registers results, and disposes the page + its owning context in
    /// a finally block. This is the shared lifecycle for every Playwright-backed site —
    /// each <c>CreateTask</c> reduces to a one-line delegation.
    ///
    /// Per-site failures are caught here, wrapped as <see cref="SiteScrapeException"/>, and
    /// stored in <paramref name="errors"/>. A site failure never aborts the surrounding
    /// orchestration — siblings keep running.
    /// </summary>
    /// <param name="useLastLink">
    /// If <c>true</c>, registers <c>Links.Last()</c> instead of <c>Links[0]</c>. Set for sites
    /// where the final scraped URL (not the first) is the canonical landing page.
    /// </param>
    internal static async Task RunPlaywrightScrapeAsync(
        IWebsite scraper,
        Website site,
        string bookTitle,
        BookType bookType,
        ConcurrentBag<List<EntryModel>> masterDataList,
        ConcurrentDictionary<Website, string> masterLinkList,
        ConcurrentDictionary<Website, Exception> errors,
        IBrowser browser,
        Region curRegion,
        CancellationToken cancellationToken,
        bool isMember = false,
        bool needsUserAgent = false,
        bool useLastLink = false)
    {
        IPage page = await PlaywrightFactory.GetPageAsync(browser, needsUserAgent);
        try
        {
            (List<EntryModel> Data, List<string> Links) = await scraper.GetData(bookTitle, bookType, page, isMember, curRegion, cancellationToken);
            masterDataList.Add(Data);
            if (Links.Count > 0)
            {
                masterLinkList.TryAdd(site, useLastLink ? Links.Last() : Links[0]);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            errors[site] = new SiteScrapeException(site, ex);
        }
        finally
        {
            await page.DisposeContextAsync();
        }
    }

    /// <summary>
    /// HtmlWeb-only version of <see cref="RunPlaywrightScrapeAsync"/>. Skips browser rental —
    /// the site's <c>GetData</c> handles the network directly via <c>HtmlWeb</c>. Same
    /// per-site failure handling: caught, wrapped, stored in <paramref name="errors"/>.
    /// </summary>
    internal static async Task RunHtmlScrapeAsync(
        IWebsite scraper,
        Website site,
        string bookTitle,
        BookType bookType,
        ConcurrentBag<List<EntryModel>> masterDataList,
        ConcurrentDictionary<Website, string> masterLinkList,
        ConcurrentDictionary<Website, Exception> errors,
        Region curRegion,
        CancellationToken cancellationToken,
        bool useLastLink = false)
    {
        try
        {
            (List<EntryModel> Data, List<string> Links) = await scraper.GetData(bookTitle, bookType, null, false, curRegion, cancellationToken);
            masterDataList.Add(Data);
            if (Links.Count > 0)
            {
                masterLinkList.TryAdd(site, useLastLink ? Links.Last() : Links[0]);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            errors[site] = new SiteScrapeException(site, ex);
        }
    }

    internal static IWebsite CreateScraper(Website site, ILoggerFactory loggerFactory)
    {
        return site switch
        {
            // America
            Website.AmazonUSA => new AmazonUSA(loggerFactory.CreateLogger<AmazonUSA>()),
            Website.BooksAMillion => new BooksAMillion(loggerFactory.CreateLogger<BooksAMillion>()),
            Website.Crunchyroll => new Crunchyroll(loggerFactory.CreateLogger<Crunchyroll>()),
            Website.InStockTrades => new InStockTrades(loggerFactory.CreateLogger<InStockTrades>()),
            Website.KinokuniyaUSA => new KinokuniyaUSA(loggerFactory.CreateLogger<KinokuniyaUSA>()),
            Website.MangaMart => new MangaMart(loggerFactory.CreateLogger<MangaMart>()),
            Website.MerryManga => new MerryManga(loggerFactory.CreateLogger<MerryManga>()),
            Website.RobertsAnimeCornerStore => new RobertsAnimeCornerStore(loggerFactory.CreateLogger<RobertsAnimeCornerStore>()),

            // Britain
            Website.ForbiddenPlanet => new ForbiddenPlanet(loggerFactory.CreateLogger<ForbiddenPlanet>()),
            Website.TravellingMan => new TravellingMan(loggerFactory.CreateLogger<TravellingMan>()),

            // Canada


            // Australia
            Website.MangaMate => new MangaMate(loggerFactory.CreateLogger<MangaMate>()),

            // Multi
            Website.SciFier => new SciFier(loggerFactory.CreateLogger<SciFier>()),
            _ => throw new ArgumentOutOfRangeException(nameof(site), site, "No scraper registered for this site")
        };
    }

    internal static void AddVolToString(this StringBuilder title)
    {
        string titleString = title.ToString();

        if (titleString.Contains("Vol", StringComparison.Ordinal) ||
            titleString.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Match volNumMatch = MasterScrape.FindVolNumRegex().Match(titleString);
        if (volNumMatch.Success)
        {
            title.Insert(volNumMatch.Index, "Vol ");
        }
    }

    internal static void RemoveAfterLastIfMultiple(ref StringBuilder input, char delimiter)
    {
        // Quick exit if delimiter not found
        int count = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == delimiter)
            {
                count++;
            }
        }

        if (count <= 1)
        {
            return; // Do nothing if there's 1 or fewer occurrences of the delimiter
        }

        // Find the last occurrence of the delimiter
        int lastIndex = -1;
        for (int i = input.Length - 1; i >= 0; i--)
        {
            if (input[i] == delimiter)
            {
                lastIndex = i;
                break;
            }
        }

        // Convert to string once for regex to extract the volume info
        string inputStr = input.ToString();

        // Look for "Vol ###" after the last delimiter
        Match match = VolRegex().Match(inputStr, lastIndex + 1);
        bool volAfterDelimiter = match.Success && match.Index > lastIndex;

        input.Length = lastIndex; // Trim everything after the last delimiter

        if (volAfterDelimiter)
        {
            input.Append(' ').Append(match.Value);
        }
    }

    /// <summary>
    /// Determines if the book title inputted by the user is contained within the current title scraped from the website
    /// </summary>
    /// <param name="bookTitle">The title inputed by the user to initialize the scrape</param>
    /// <param name="curTitle">The current title scraped from the website</param>
    internal static bool EntryTitleContainsBookTitle(string bookTitle, string curTitle)
    {
        return RemoveNonWordsRegex().Replace(curTitle, string.Empty).Contains(RemoveNonWordsRegex().Replace(bookTitle, string.Empty), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether this entry should be removed if it contains a certain text that is also not contained in the book title
    /// </summary>
    /// <param name="bookTitle"></param>
    /// <param name="entryTitle"></param>
    /// <param name="textToCheck"></param>
    /// <returns></returns>
    internal static bool RemoveEntryTitleCheck(string bookTitle, string entryTitle, string textToCheck)
    {
        return !entryTitle.Contains(textToCheck, StringComparison.OrdinalIgnoreCase) && !bookTitle.Contains(textToCheck, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool TitleStartsWithCheck(string bookTitle, string curTitle)
    {
        return RemoveNonWordsRegex().Replace(curTitle, string.Empty).StartsWith(RemoveNonWordsRegex().Replace(bookTitle, string.Empty), StringComparison.OrdinalIgnoreCase);
    }

    internal static void ReplaceMultipleTextInEntryTitle(ref StringBuilder curTitle, string bookTitle, IEnumerable<string> containsText, string replaceText)
    {
        foreach (string text in containsText)
        {
            if (!bookTitle.Contains(text, StringComparison.OrdinalIgnoreCase) && curTitle.ToString().Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(text, replaceText);
                break;
            }
        }
    }

    internal static void ReplaceTextInEntryTitle(ref StringBuilder curTitle, string bookTitle, string containsText, string replaceText)
    {
        if (!bookTitle.Contains(containsText, StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace(containsText, replaceText);
        }
    }

    internal static void ReplaceTextInEntryTitle(ref StringBuilder curTitle, string bookTitle, char containsText, char replaceText)
    {
        if (!bookTitle.Contains(containsText))
        {
            curTitle.Replace(containsText, replaceText);
        }
    }

    internal static void RemoveCharacterFromTitle(ref StringBuilder curTitle, string bookTitle, char charToRemove)
    {
        if (bookTitle.Contains(charToRemove))
        {
            return;
        }

        for (int i = curTitle.Length - 1; i >= 0; i--)
        {
            if (curTitle[i] == charToRemove)
            {
                curTitle.Remove(i, 1);
            }
        }
    }

    internal static void RemoveCharacterFromTitle(ref StringBuilder curTitle, string bookTitle, char charToRemove, string textToCheck)
    {
        if (bookTitle.Contains(charToRemove) || curTitle.ContainsOrdinal(textToCheck))
        {
            return;
        }

        for (int i = curTitle.Length - 1; i >= 0; i--)
        {
            if (curTitle[i] == charToRemove)
            {
                curTitle.Remove(i, 1);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bookTitle"></param>
    /// <param name="searchTitle"></param>
    /// <param name="curTitle"></param>
    /// <param name="removeText"></param>
    /// <returns>True if the curTitle should be removed</returns>
    internal static bool RemoveUnintendedVolumes(string bookTitle, string searchTitle, string curTitle, string removeText)
    {
        return bookTitle.Contains(searchTitle, StringComparison.OrdinalIgnoreCase) &&
            curTitle.Contains(removeText, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool RemoveUnintendedVolumes(string bookTitle, string searchTitle, string curTitle, params string[] removeText)
    {
        if (!bookTitle.Contains(searchTitle, StringComparison.OrdinalIgnoreCase)) return false;

        foreach (var text in removeText)
        {
            if (curTitle.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Percent-encodes any character in <paramref name="bookTitle"/> that is not an “unreserved” URI character:
    /// letters (A–Z, a–z), digits (0–9), hyphen (<c>-</c>), period (<c>.</c>), underscore (<c>_</c>), or tilde (<c>~</c>).
    /// Each disallowed character is replaced with its “%HH” two-digit uppercase hex code.
    /// </summary>
    /// <param name="bookTitle">The raw book title to filter/encode.</param>
    /// <returns>
    /// The percent-encoded title, suitable for use in a URL path or query.
    /// </returns>
    internal static string FilterBookTitle(string bookTitle)
    {
        if (string.IsNullOrEmpty(bookTitle))
        {
            return bookTitle;
        }

        // Estimate: most characters stay 1→1, escaped ones become 3 chars ("%HH")
        StringBuilder sb = new(bookTitle.Length * 2);

        foreach (char c in bookTitle)
        {
            // Unreserved per RFC3986: A–Z a–z 0–9 - . _ ~
            if ((c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                c == '-' ||
                c == '.' ||
                c == '_' ||
                c == '~')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('%');
                sb.Append(((int)c).ToString("X2"));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Trims trailing whitespace from the <see cref="StringBuilder"/>, plus any additional
    /// characters in <paramref name="pTrimChars"/>. Pass <c>[':']</c> etc. as a collection
    /// expression — it compiles to an inline span, no heap allocation.
    /// </summary>
    internal static StringBuilder TrimEnd(this StringBuilder pStringBuilder, ReadOnlySpan<char> pTrimChars = default)
    {
        if (pStringBuilder is null || pStringBuilder.Length == 0)
        {
            return pStringBuilder!;
        }

        int i = pStringBuilder.Length - 1;

        for (; i >= 0; i--)
        {
            char lChar = pStringBuilder[i];

            if (char.IsWhiteSpace(lChar))
            {
                continue;
            }

            if (pTrimChars.IsEmpty || pTrimChars.IndexOf(lChar) < 0)
            {
                break;
            }
        }

        if (i < pStringBuilder.Length - 1)
        {
            pStringBuilder.Length = i + 1;
        }

        return pStringBuilder;
    }

    internal static int IndexOfOrdinal(this StringBuilder sb, ReadOnlySpan<char> value)
    {
        int len = sb.Length;
        if (len == 0) return -1;

        char[]? rented = null;
        Span<char> buf = len <= StackallocThreshold
            ? stackalloc char[len]
            : (rented = ArrayPool<char>.Shared.Rent(len)).AsSpan(0, len);

        try
        {
            sb.CopyTo(0, buf, len);
            return buf.IndexOf(value, StringComparison.Ordinal);
        }
        finally
        {
            if (rented is not null) ArrayPool<char>.Shared.Return(rented);
        }
    }

    internal static int IndexOfOrdinal(this StringBuilder sb, string value)
        => sb.IndexOfOrdinal(value.AsSpan());

    internal static bool ContainsOrdinal(this StringBuilder sb, ReadOnlySpan<char> value)
        => sb.IndexOfOrdinal(value) >= 0;

    // Ignore-case variants if you need them:
    internal static int IndexOfIgnoreCase(this StringBuilder sb, ReadOnlySpan<char> value)
    {
        int len = sb.Length;
        if (len == 0) return -1;

        char[]? rented = null;
        Span<char> buf = len <= StackallocThreshold
            ? stackalloc char[len]
            : (rented = ArrayPool<char>.Shared.Rent(len)).AsSpan(0, len);

        try
        {
            sb.CopyTo(0, buf, len);
            return buf.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (rented is not null) ArrayPool<char>.Shared.Return(rented);
        }
    }

    internal static bool ContainsIgnoreCase(this StringBuilder sb, ReadOnlySpan<char> value)
        => sb.IndexOfIgnoreCase(value) >= 0;

    internal static void PrintWebsiteData(string website, string bookTitle, BookType bookType, IEnumerable<EntryModel> dataList, ILogger logger)
    {
        if (MasterScrape.IsDebugEnabled)
        {
            // Clean up website string once before using it for file path.
            string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            Directory.CreateDirectory(dataDir);
            string filePath = Path.Combine(dataDir, $"{website.Replace(" ", string.Empty)}Data.txt");

            using StreamWriter outputFile = new(filePath);
            if (dataList.Any())
            {
                foreach (EntryModel data in dataList)
                {
                    logger.WebsiteDataEntry(data);
                    outputFile.WriteLine(data);
                }
            }
            else
            {
                string message = $"{bookTitle} ({bookType}) Does Not Exist @ {website}";
                logger.WebsiteDataMissing(bookTitle, bookType, website);
                outputFile.WriteLine(message);
            }
        }
    }

    internal static bool ContainsAny(this string input, IEnumerable<string> values)
    {
        return values.AsValueEnumerable().Any(val => input.Contains(val, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool ContainsAny<T>(this HashSet<T> values, IEnumerable<T> input)
    {
        foreach (T element in input)
        {
            if (values.Contains(element))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sorts <paramref name="data"/> in place using the same logic as
    /// <see cref="EntryModel.VolumeSort"/>, but with per-entry sort keys precomputed once.
    /// <para>
    /// The naive <c>data.Sort(EntryModel.VolumeSort)</c> path runs 2–4 regex Replaces + 2
    /// <see cref="EntryModel.GetCurrentVolumeNum"/> calls per <c>Compare</c> — and <c>Compare</c>
    /// fires <c>N log N</c> times. This helper does the regex work once per entry (linear) and
    /// the in-sort comparisons become array reads against the precomputed key tables.
    /// </para>
    /// </summary>
    internal static void SortByVolume(this List<EntryModel> data)
    {
        int count = data.Count;
        if (count < 2) return;

        Span<EntryModel> span = CollectionsMarshal.AsSpan(data);

        // Per-entry sort keys, computed once.
        string[] filteredTexts = new string[count];   // fallback: ordinal compare on filtered title
        string[] filteredNames = new string[count];   // name with " Vol N" / " Box Set N" suffix stripped
        double[] volNums = new double[count];          // parsed vol number, -1 if absent / box set
        byte[] entryTypes = new byte[count];           // 0 = neither, 1 = Vol, 2 = Box Set

        for (int i = 0; i < count; i++)
        {
            string entry = span[i].Entry;

            // Box Set takes precedence — its vol always returns -1 via GetCurrentVolumeNum,
            // so we treat the two types as disjoint categories for the type-match check.
            bool isBoxSet = entry.Contains("Box Set");
            bool isVol = !isBoxSet && entry.Contains("Vol");
            entryTypes[i] = (byte)(isVol ? 1 : (isBoxSet ? 2 : 0));

            string filteredText = VolumeSort.FilterNameRegex().Replace(entry, " ");
            filteredTexts[i] = filteredText;
            filteredNames[i] = VolumeSort.ExtractNameRegex().Replace(filteredText, string.Empty);
            volNums[i] = EntryModel.GetCurrentVolumeNum(entry);
        }

        // Sort an index array against the precomputed keys, then materialize the result back
        // into `data`. Array.Sort with a Comparison delegate is in-place on the index array.
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;

        Array.Sort(indices, (a, b) =>
        {
            if (entryTypes[a] != 0 && entryTypes[a] == entryTypes[b]
                && volNums[a] != -1 && volNums[b] != -1)
            {
                if (string.Equals(filteredNames[a], filteredNames[b], StringComparison.OrdinalIgnoreCase) ||
                    Similar(filteredNames[a], filteredNames[b],
                        Math.Min(filteredNames[a].Length, filteredNames[b].Length) / 6) != -1)
                {
                    return volNums[a].CompareTo(volNums[b]);
                }
            }
            return string.Compare(filteredTexts[a], filteredTexts[b], StringComparison.OrdinalIgnoreCase);
        });

        // Apply the permutation. Two-pass with a temporary buffer is the simplest correct
        // approach; cycle-following would save the buffer but tangles the code.
        EntryModel[] reordered = new EntryModel[count];
        for (int i = 0; i < count; i++) reordered[i] = span[indices[i]];
        for (int i = 0; i < count; i++) span[i] = reordered[i];
    }

    /// <summary>
    /// Removes duplicate <see cref="EntryModel"/> rows in place, keeping the cheaper price when
    /// the same <c>Entry</c> appears more than once. Order-independent — does not require a
    /// pre-sort. Single pass, O(N), with parsed prices cached so each entry's
    /// <see cref="EntryModel.ParsePrice"/> runs at most once.
    /// </summary>
    internal static void RemoveDuplicates(this List<EntryModel> input, ILogger logger)
    {
        int count = input.Count;
        if (count < 2) return;

        Span<EntryModel> span = CollectionsMarshal.AsSpan(input);

        // Cache parsed prices — same-key collisions would otherwise re-parse each visit.
        decimal[] prices = new decimal[count];
        for (int i = 0; i < count; i++)
        {
            prices[i] = span[i].ParsePrice();
        }

        // Map Entry → index of the cheapest occurrence seen so far.
        Dictionary<string, int> bestSeen = new(count, StringComparer.OrdinalIgnoreCase);
        bool[] toRemove = new bool[count];

        for (int i = 0; i < count; i++)
        {
            string key = span[i].Entry;
            if (bestSeen.TryGetValue(key, out int existing))
            {
                logger.DuplicateInputPair(span[i], span[existing]);
                if (prices[i] < prices[existing])
                {
                    logger.RemovedDuplicate(span[existing]);
                    toRemove[existing] = true;
                    bestSeen[key] = i;
                }
                else
                {
                    logger.RemovedDuplicate(span[i]);
                    toRemove[i] = true;
                }
            }
            else
            {
                bestSeen[key] = i;
            }
        }

        // Compact in place — one pass, zero mid-list shifts.
        int write = 0;
        for (int read = 0; read < count; read++)
        {
            if (!toRemove[read])
            {
                if (write != read) input[write] = input[read];
                write++;
            }
        }
        input.RemoveRange(write, count - write);
    }

    /// <summary>
    /// Applies a coupon to the price by substracting the coupon amount
    /// </summary>
    /// <param name="initialPrice"></param>
    /// <param name="couponAmount"></param>
    /// <returns></returns>
    internal static string ApplyCoupon(decimal initialPrice, decimal couponAmount)
    {
        return decimal.Subtract(initialPrice, couponAmount).ToString("0.00");
    }
    
    /// <summary>
    ///  The Damerau–Levenshtein distance between two words is the minimum number of operations (consisting of insertions, deletions or substitutions of a single character, or transposition of two adjacent characters) required to change one word into the other (http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html)
    /// </summary>
    /// <returns>The distance, >= 0 representing the number of edits required to transform one string to the other, or -1 if the distance is greater than the specified maxDistance.</returns>
    internal static int Similar(string s, string t, int maxDistance)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return string.IsNullOrEmpty(t) || t.Length <= maxDistance ? t.Length : -1;
        }

        if (string.IsNullOrWhiteSpace(t))
        {
            return s.Length <= maxDistance ? s.Length : -1;
        }

        ReadOnlySpan<char> sSpan = s;
        ReadOnlySpan<char> tSpan = t;

        // Always operate on the shorter string
        if (sSpan.Length > tSpan.Length)
        {
            ReadOnlySpan<char> tmp = sSpan;
            sSpan = tSpan;
            tSpan = tmp;
        }

        int sLen = sSpan.Length;
        int tLen = tSpan.Length;

        if (tLen - sLen > maxDistance)
        {
            return -1;
        }

        Span<int> previousRow = stackalloc int[tLen + 1];
        Span<int> currentRow = stackalloc int[tLen + 1];

        // Lower-case both strings once up-front so the inner-loop comparison is a
        // single char==char rather than an N*M cascade of casing-table lookups.
        char[]? rentedSLower = null;
        char[]? rentedTLower = null;
        Span<char> sLower = sLen <= StackallocThreshold
            ? stackalloc char[sLen]
            : (rentedSLower = ArrayPool<char>.Shared.Rent(sLen)).AsSpan(0, sLen);
        Span<char> tLower = tLen <= StackallocThreshold
            ? stackalloc char[tLen]
            : (rentedTLower = ArrayPool<char>.Shared.Rent(tLen)).AsSpan(0, tLen);

        try
        {
            sSpan.ToLowerInvariant(sLower);
            tSpan.ToLowerInvariant(tLower);

            for (int j = 0; j <= tLen; j++)
            {
                previousRow[j] = j;
            }

            for (int i = 1; i <= sLen; i++)
            {
                currentRow[0] = i;
                int bestThisRow = currentRow[0];

                char sChar = sLower[i - 1];
                for (int j = 1; j <= tLen; j++)
                {
                    int cost = sChar == tLower[j - 1] ? 0 : 1;
                    int insert = currentRow[j - 1] + 1;
                    int delete = previousRow[j] + 1;
                    int replace = previousRow[j - 1] + cost;

                    currentRow[j] = Math.Min(Math.Min(insert, delete), replace);

                    bestThisRow = Math.Min(bestThisRow, currentRow[j]);
                }

                if (bestThisRow > maxDistance)
                {
                    return -1;
                }

                Span<int> temp = previousRow;
                previousRow = currentRow;
                currentRow = temp;
            }

            int result = previousRow[tLen];
            return result <= maxDistance ? result : -1;
        }
        finally
        {
            if (rentedSLower is not null) ArrayPool<char>.Shared.Return(rentedSLower);
            if (rentedTLower is not null) ArrayPool<char>.Shared.Return(rentedTLower);
        }
    }
}