using System.Collections.Frozen;

namespace MangaAndLightNovelWebScrape;

internal static partial class InternalHelpers
{
    private static readonly Logger LOGGER = LogManager.GetLogger("MasterScrape");
    [GeneratedRegex(@"[^\w+]")] internal static partial Regex RemoveNonWordsRegex();
    [GeneratedRegex(@"Vol\s\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex VolRegex();

    private static readonly FrozenSet<string> _entryRemovalTerms = new[]
    {
        "Bluray", "Blu-ray", "Choose Path", "Encyclopedia", "Anthology", "Official", "Character", "Guide",
        "Illustration", "Anime Profiles", "Choose Your Path", "Compendium",
        "Artbook", "Art Book", "Error", "Advertising", "(Osi)", "Ani-manga",
        "Anime", "Bilingual", "Game Book", "Theatrical", "Figure", "SEGA",
        "Poster", "Statue", "IMPORT", "Trace", "Bookmarks", "Music Book",
        "Retrospective", "Notebook", "Journal", "Art of", "the Anime",
        "Calendar", "Adventure Book", "Coloring Book", "Sketchbook", "PLUSH",
        "Pirate Recipes", "Exclusive", "Hobby", "Model Kit", "Funko POP", "Creator of the", "the Movie"
    }
    .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

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
    ///   Tuple of membership flags for Books-A-Million, Kinokuniya USA, and Indigo; affects site-specific logic.
    /// </param>
    /// <param name="browser">Optional <see cref="Browser"/> instance to use for scraping, or null for default.</param>
    internal static void ScheduleScrapes(
        this List<Task> webTasks,
        IEnumerable<Website> sites,
        string bookTitle,
        BookType bookType,
        ConcurrentBag<List<EntryModel>> masterBag,
        ConcurrentDictionary<Website, string> masterDict,
        Browser browser,
        Region curRegion,
        (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships)
    {
        foreach (Website site in sites)
        {
            IWebsite scraper = CreateScraper(site);

            Task task = scraper.CreateTask(
                bookTitle,
                bookType,
                masterBag,
                masterDict,
                browser,
                curRegion,
                memberships
            );

            webTasks.Add(task);
        }
    }

    internal static IWebsite CreateScraper(Website site)
    {
        return site switch
        {
            Website.Crunchyroll => new Crunchyroll(),
            Website.RobertsAnimeCornerStore => new RobertsAnimeCornerStore(),
            Website.InStockTrades => new InStockTrades(),
            Website.MangaMart => new MangaMart(),
            Website.AmazonUSA => new AmazonUSA(),
            Website.BooksAMillion => new BooksAMillion(),
            Website.ForbiddenPlanet => new ForbiddenPlanet(),
            Website.Indigo => new Indigo(),
            Website.KinokuniyaUSA => new KinokuniyaUSA(),
            Website.MangaMate => new MangaMate(),
            Website.MerryManga => new MerryManga(),
            Website.SciFier => new SciFier(),
            _ => throw new ArgumentOutOfRangeException(nameof(site), site, "No scraper registered for this site")
        };
    }

    internal static List<EntryModel> RemoveDuplicateEntries(List<EntryModel> entries)
    {
        List<EntryModel> output = [];
        foreach (EntryModel entry in entries)
        {
            if (!output.Contains(entry))
            {
                output.Add(entry);
            }
        }
        return output;
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

    internal static void ReplaceMultipleTextInEntryTitle (ref StringBuilder curTitle, string bookTitle, IEnumerable<string> containsText, string replaceText)
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

    internal static void ReplaceTextInEntryTitle (ref StringBuilder curTitle, string bookTitle, string containsText, string replaceText)
    {
        if (!bookTitle.Contains(containsText, StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace(containsText, replaceText);
        }
    }

    internal static void ReplaceTextInEntryTitle (ref StringBuilder curTitle, string bookTitle, char containsText, char replaceText)
    {
        if (!bookTitle.Contains(containsText))
        {
            curTitle.Replace(containsText, replaceText);
        }
    }

    internal static void RemoveCharacterFromTitle(ref StringBuilder curTitle, string bookTitle, char charToRemove)
    {
        // Check if charToRemove exists in bookTitle
        if (!bookTitle.Contains(charToRemove) && curTitle.ToString().Contains(charToRemove))
        {
            for (int i = 0; i < curTitle.Length; i++)
            {
                if (curTitle[i] == charToRemove)
                {
                    curTitle.Remove(i, 1);
                    i--; // Adjust the index to re-check the current position after removal
                }
            }
        }
    }

    internal static void RemoveCharacterFromTitle(ref StringBuilder curTitle, string bookTitle, char charToRemove, string textToCheck)
    {
        string title = curTitle.ToString();
        if (!bookTitle.Contains(charToRemove) && !title.Contains(textToCheck))
        {
            int index = 0;
            while (index < curTitle.Length)
            {
                if (curTitle[index] == charToRemove)
                {
                    curTitle.Remove(index, 1); // Remove character at index
                }
                else
                {
                    index++; // Only increment if no removal occurred
                }
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
        return bookTitle.IndexOf(searchTitle, StringComparison.OrdinalIgnoreCase) >= 0 &&
            curTitle.IndexOf(removeText, StringComparison.OrdinalIgnoreCase) >= 0;
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
    public static string FilterBookTitle(string bookTitle)
    {
        if (string.IsNullOrEmpty(bookTitle))
        {
            return bookTitle;
        }

        // Estimate: most characters stay 1→1, escaped ones become 3 chars ("%HH")
        StringBuilder sb = new StringBuilder(bookTitle.Length * 2);

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
    /// Trims the end of the StingBuilder Content. On Default only the white space char is truncated.
    /// </summary>
    /// <param name="pTrimChars">Array of additional chars to be truncated. A little bit more efficient than using char[]</param>
    /// <returns></returns>
    internal static StringBuilder TrimEnd(this StringBuilder pStringBuilder, HashSet<char>? pTrimChars = null)
    {
        if (pStringBuilder == null || pStringBuilder.Length == 0)
        {
            return pStringBuilder!;
        }

        int i = pStringBuilder.Length - 1;

        for (; i >= 0; i--)
        {
            var lChar = pStringBuilder[i];

            if (pTrimChars == null)
            {
                if (char.IsWhiteSpace(lChar) == false)
                {
                    break;
                }
            }
            else if ((char.IsWhiteSpace(lChar) == false) && (pTrimChars.Contains(lChar) == false))
            {
                break;
            }
        }

        if (i < pStringBuilder.Length - 1)
            pStringBuilder.Length = i + 1;

        return pStringBuilder;
    }

    internal static void PrintWebsiteData(string website, string bookTitle, BookType bookType, IEnumerable<EntryModel> dataList, Logger LOGGER)
    {
        if (MasterScrape.IsDebugEnabled)
        {
            // Clean up website string once before using it for file path.
            string filePath = $@"Data\{website.Replace(" ", string.Empty)}Data.txt";

            using (StreamWriter outputFile = new(filePath))
            {
                if (dataList.Count() > 0)
                {
                    // If we have data, write it to both the logger and the output file.
                    foreach (EntryModel data in dataList)
                    {
                        LOGGER.Info(data);  // Log the data entry
                        outputFile.WriteLine(data);  // Write to the file
                    }
                }
                else
                {
                    string message = $"{bookTitle} ({bookType}) Does Not Exist @ {website}";
                    LOGGER.Error(message);  // Log the error message
                    outputFile.WriteLine(message);  // Write the error to the file
                }
            }
        }
    }

    internal static bool ContainsAny(this string input, IEnumerable<string> values)
    {
        return values.AsValueEnumerable().Any(val => input.Contains(val, StringComparison.OrdinalIgnoreCase));
    }

    internal static void RemoveDuplicates(this List<EntryModel> input, Logger LOGGER)
    {
        for (int x = input.Count - 1; x > 0; x--)
        {
            if (input[x].Entry.Equals(input[x - 1].Entry, StringComparison.OrdinalIgnoreCase))
            {
                LOGGER.Debug("INPUT 1 {}", input[x]);
                LOGGER.Debug("INPUT 2 {}", input[x - 1]);
                if (input[x].ParsePrice() >= input[x - 1].ParsePrice())
                {
                    LOGGER.Info($"Removed Duplicate {input[x]}");
                    input.RemoveAt(x);  // Remove the current entry
                }
                else
                {
                    LOGGER.Info($"Removed Duplicate {input[x - 1]}");
                    input.RemoveAt(x - 1);  // Remove the previous entry
                }
            }
        }
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
}