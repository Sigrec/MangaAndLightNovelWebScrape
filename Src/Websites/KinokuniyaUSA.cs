using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class KinokuniyaUSA : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
    
    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//span[@class='underline']");
    private static readonly XPathExpression _memberPriceXPath = XPathExpression.Compile("//li[@class='price'][2]/span");
    private static readonly XPathExpression _nonMemberPriceXPath = XPathExpression.Compile("//li[@class='price'][1]/span");
    private static readonly XPathExpression _descXPath = XPathExpression.Compile("//p[@class='description']");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//li[@class='status']");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//div[@class='categoryPager']/ul/li[last()]/a");
    
    [GeneratedRegex(@"\((?:Omnibus\s*|\d{1,3}\s*In\s*\d{1,3}\s*)Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"\(Light Novel\)|Light Novel|Novel", RegexOptions.IgnoreCase)] private static partial Regex NovelRegex();
    [GeneratedRegex(@"\((.*?)\)+", RegexOptions.IgnoreCase)] private static partial Regex TitleCaptureRegex();
    [GeneratedRegex(@"^[^\(]+", RegexOptions.IgnoreCase)] private static partial Regex CleanInFrontTitleRegex();
    [GeneratedRegex(@"(?<=\d{1,3})[^\d{1,3}].*", RegexOptions.IgnoreCase)] private static partial Regex CleanBehindTitleRegex();
    [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\({1,}.*?\){1,}|<.*?>|w/DVD|<|>|(?<=\d{1,3})\s+:.*", RegexOptions.IgnoreCase)] private static partial Regex MangaTitleFixRegex();
    [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\({1,}.*?\){1,}|(?<=\d{1,3})\s?:.*|<.*?[^\d+]>|w/DVD|<|>", RegexOptions.IgnoreCase)] private static partial Regex NovelTitleFixRegex();
    [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"\d{1,3}\.\d{1,3}|\d{1,3}")] private static partial Regex FindVolNumRegex();

    /// <inheritdoc />
    public const string TITLE = "Kinokuniya USA";

    /// <inheritdoc />
    public const string BASE_URL = "https://united-states.kinokuniya.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    private static readonly int STATUS_START_INDEX = "Availability Status : ".Length;
    private static readonly FrozenSet<string> _skipBookTitles = ["Attack on Titan"];
    private static readonly FrozenSet<string> _bookTypeKeyWords = ["Vol", "Box Set", "Anniversary"];

    // Manga English Search
    //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=world+trigger&taxon=2&x=39&y=4&page=1&per_page=100&form_taxon=109
    // https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=Skeleton+Knight+in+Another+World&taxon=2&x=39&y=11&page=1&per_page=100

    // Light Novel English Search
    //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=overlord+novel&taxon=&x=33&y=8&per_page=100&form_taxon=109
    //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=classroom+of+the+elite&taxon=&x=33&y=8&per_page=100&form_taxon=109

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships)
    {
        return Task.Run(async () =>
        {
            IPage page = await PlaywrightFactory.GetPageAsync(browser!, true);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page, memberships.IsKinokuniyaUSAMember);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.KinokuniyaUSA, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(string bookTitle, BookType bookType)
    {
        string url = $"{BASE_URL}/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords={bookTitle.Replace(" ", "+")}{(bookType == BookType.LightNovel ? "+novel" : string.Empty)}&taxon=2&x=39&y=11&page=1&per_page=100";
        LOGGER.Info($"Url = {url}");
        return url;
    }

    private static async Task WaitForPageLoad(IPage page, int timeoutMilliseconds = 30000)
    {
        // The locator for your loading element
        ILocator loadingElement = page.Locator("#loading");

        DateTime startTime = DateTime.Now;
        TimeSpan maxTime = TimeSpan.FromMilliseconds(timeoutMilliseconds);

        // Loop until the element is hidden or the timeout is reached
        while (true)
        {
            // Check if the element is hidden
            bool isHidden = await loadingElement.IsHiddenAsync();

            if (isHidden)
            {
                return; // The element is hidden, success!
            }

            // If it's not hidden, check if we've run out of time
            if (DateTime.Now - startTime > maxTime)
            {
                // Throw an exception if the element doesn't disappear in time
                throw new TimeoutException("The loading element did not disappear within the specified timeout.");
            }

            // Wait a short duration before checking again
            await Task.Delay(100);
        }
    }

    private static string ParseAndCleanTitle(string entryTitle, BookType bookType, string bookTitle, string entryDesc, bool oneShotCheck)
    {
        // 1) Cheap pre-computations / span-based checks
        bool entryTitleHasDigit = ContainsDigit(entryTitle);
        bool bookTitleHasDigit  = ContainsDigit(bookTitle);

        // Avoid doing Replace if not needed (micro); Replace returns original if not found, but we skip the call entirely
        if (bookTitle.IndexOf('-', StringComparison.Ordinal) < 0 && entryTitle.IndexOf('-', StringComparison.Ordinal) >= 0)
        {
            entryTitle = entryTitle.Replace("-", " ");
        }

        string output;

        // 2) Front cleanup / capture once
        string parseCheckTitle = TitleCaptureRegex().Match(entryTitle).Groups[1].Value;
        string checkBeforeText = CleanInFrontTitleRegex().Match(entryTitle).Value;

        if (!_skipBookTitles.Contains(bookTitle, StringComparer.OrdinalIgnoreCase)
            && parseCheckTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase)
            && !checkBeforeText.Contains(bookTitle, StringComparison.OrdinalIgnoreCase)
            && entryTitleHasDigit
            && !bookTitleHasDigit)
        {
            // Concat avoids building an intermediate then calling Insert on a new instance
            string cleaned = CleanInFrontTitleRegex().Replace(entryTitle, string.Empty);
            entryTitle = string.Concat(parseCheckTitle, " ", cleaned);
        }

        if (!oneShotCheck)
        {
            // 3) Regex transforms in a single pass per regex (avoid nesting; same work, clearer)
            string newEntryTitle = FixVolumeRegex().Replace(entryTitle, "Vol");
            newEntryTitle = OmnibusRegex().Replace(newEntryTitle, "Omnibus");

            if (bookType == BookType.LightNovel)
            {
                newEntryTitle = NovelRegex().Replace(newEntryTitle, "Novel");
                newEntryTitle = NovelTitleFixRegex().Replace(newEntryTitle, "$1");
            }
            else
            {
                newEntryTitle = MangaTitleFixRegex().Replace(newEntryTitle, "$1");
            }

            // 4) Work in StringBuilder, minimize ToString()
            StringBuilder curTitle = new(newEntryTitle.Length + 16);
            curTitle.Append(newEntryTitle);
            curTitle.Replace(",", string.Empty);
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

            // We need a string snapshot a few times; take it once, reuse, then invalidate only when needed
            string curSnapshot = curTitle.ToString().Trim();

            if (bookType == BookType.LightNovel)
            {
                // Insert book title at front if missing
                if (!curSnapshot.Contains(bookTitle, StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Insert(0, $"{char.ToUpperInvariant(bookTitle[0])}{bookTitle.AsSpan(1)} ");
                    curSnapshot = null!; // invalidate snapshot
                }

                string snapshot = curSnapshot ??= curTitle.ToString();
                bool containsNovel = snapshot.Contains("Novel", StringComparison.OrdinalIgnoreCase);
                bool containsVol   = snapshot.Contains("Vol",   StringComparison.OrdinalIgnoreCase);

                if (!containsNovel && !containsVol)
                {
                    // Reset builder with cleaned string to avoid constructing then discarding
                    string cleanedBehind = CleanBehindTitleRegex().Replace(snapshot, string.Empty);
                    curTitle.Clear().Append(cleanedBehind);
                    curSnapshot = null!;
                }
                else if (!containsNovel && containsVol)
                {
                    int volIdx = snapshot.IndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                    if (volIdx >= 0) curTitle.Insert(volIdx, "Novel ");
                    curSnapshot = null!;
                }

                snapshot = (curSnapshot ??= curTitle.ToString());
                if (!snapshot.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                    && !ContainsDigit(snapshot)
                    && !bookTitleHasDigit)
                {
                    curTitle.Append(" Novel");
                    curSnapshot = null!;
                }
            }
            else if (bookType == BookType.Manga && !newEntryTitle.ContainsAny(_bookTypeKeyWords))
            {
                // Only compute the vol match once
                Match volMatchForManga = FindVolNumRegex().Match(newEntryTitle);
                if (volMatchForManga.Success && !bookTitleHasDigit)
                {
                    curTitle.Insert(volMatchForManga.Index, "Vol ");
                    curSnapshot = null!;
                }
                else
                {
                    // avoid calling Contains twice on entryDesc
                    bool hasCollection = entryDesc.Contains("Collection", StringComparison.OrdinalIgnoreCase);
                    bool hasVolumes    = entryDesc.Contains("volumes",    StringComparison.OrdinalIgnoreCase);
                    if (hasCollection && hasVolumes)
                    {
                        curTitle.Append(" Box Set");
                        curSnapshot = null!;
                    }
                }
            }

            // Special series cleanup
            if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace("Naruto Next Generations", string.Empty);
                curSnapshot = null!;
            }

            // If entry title has digits but we don't already have "Vol" in the *current* string,
            // try to move detected vol number to the end as "Vol X"
            if (entryTitleHasDigit)
            {
                string snapshot = curSnapshot ?? (curSnapshot = curTitle.ToString());
                if (!snapshot.Contains("Vol", StringComparison.Ordinal) && !entryTitle.ContainsAny(new[] { "Box Set", "Anniversary" }))
                {
                    Match volNum = FindVolNumRegex().Match(snapshot);
                    if (volNum.Success)
                    {
                        curTitle.Remove(volNum.Index, volNum.Value.Length);
                        bool needsNovelLabel = bookType == BookType.LightNovel
                                            && snapshot.IndexOf("Novel", StringComparison.OrdinalIgnoreCase) < 0;

                        if (needsNovelLabel)
                        {
                            curTitle.Append(" Novel");
                        }

                        curTitle.Append(" Vol ").Append(volNum.Value);
                        curSnapshot = null!;
                    }
                }
            }

            // Noragami series tweak
            {
                string snapshot = curSnapshot ?? (curSnapshot = curTitle.ToString());
                if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase)
                    && snapshot.IndexOf("Omnibus",       StringComparison.OrdinalIgnoreCase) < 0
                    && snapshot.IndexOf("Stray Stories", StringComparison.OrdinalIgnoreCase) < 0
                    && snapshot.IndexOf("Stray God",     StringComparison.OrdinalIgnoreCase) < 0)
                {
                    int volIdx = snapshot.IndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                    if (volIdx >= 0)
                    {
                        curTitle.Insert(volIdx, "Stray God ");
                        curSnapshot = null!;
                    }
                }
            }

            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

            // Final squish + trim
            string squished = MasterScrape.MultipleWhiteSpaceRegex()
                .Replace(curTitle.Replace("Manga", string.Empty).ToString().Trim(), " ");
            output = squished;
        }
        else
        {
            // oneShotCheck path
            string cleaned = entryTitle.Replace("Manga", string.Empty).Replace(",", string.Empty);
            cleaned = FixVolumeRegex().Replace(cleaned, "Vol");

            if (bookType == BookType.Manga)
            {
                output = MasterScrape.MultipleWhiteSpaceRegex()
                    .Replace(MangaTitleFixRegex().Replace(cleaned, "$1").Trim(), " ");
            }
            else
            {
                cleaned = NovelTitleFixRegex().Replace(cleaned, "$1");
                output  = MasterScrape.MultipleWhiteSpaceRegex().Replace(cleaned.Trim(), " ");
            }
        }

        // Keep original semantics: insert "Special Edition " before "Vol" if the *entryTitle* has that phrase
        if (entryTitle.Contains("Special Edition", StringComparison.OrdinalIgnoreCase))
        {
            int volIdx = output.IndexOf("Vol", StringComparison.Ordinal);
            // original code assumes "Vol" exists; do the same (will throw if not found, matching prior behavior)
            output = output.Insert(volIdx, "Special Edition ");
        }

        static bool ContainsDigit(string s)
        {
            ReadOnlySpan<char> span = s.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (char.IsDigit(span[i])) return true;
            }
            return false;
        }

        return MasterScrape.FinalCleanRegex().Replace(output, string.Empty);
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            int maxPageCount = -1, curPageNum = 1;
            bool oneShotCheck = false;
            string entryTitle, entryDesc;
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);

            HtmlDocument doc = HtmlFactory.CreateDocument();
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();

            string url = GenerateWebsiteUrl(bookTitle, bookType);
            links.Add(url);
            await page!.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await WaitForPageLoad(page);

            // Click the list display mode so it shows stock status data with entry
            await page.Locator("li#detail-button a:has-text(\"List\")").ForceClickAsync();
            await WaitForPageLoad(page);
            LOGGER.Info("Clicked List Mode");

            if (bookType == BookType.Manga)
            {
                // Click the Manga
                await page.GetByText("Manga", new PageGetByTextOptions { Exact = true }).ForceClickAsync();
                await WaitForPageLoad(page);
                LOGGER.Info("Clicked Manga");
            }

            while (true)
            {
                doc.LoadHtml(await page.ContentAsync());

                // Get the page data from the HTML doc
                XPathNodeIterator titleData = nav.Select(_titleXPath);
                XPathNodeIterator priceData = nav.Select(isMember ? _memberPriceXPath : _nonMemberPriceXPath);
                XPathNodeIterator descData = nav.Select(_descXPath);
                XPathNodeIterator stockStatusData = nav.Select(_stockStatusXPath);
                if (maxPageCount == -1) { maxPageCount = Convert.ToInt32(doc.DocumentNode.SelectSingleNode(_pageCheckXPath).InnerText); }
                LOGGER.Info("Max Page Count = {Count}", maxPageCount);

                // Determine if the series is a one shot or not
                oneShotCheck = maxPageCount == 1 && titleData.Count == 1 && !titleData.Cast<XPathNavigator>().AsValueEnumerable().Any(title => title.Value.Contains("Vol", StringComparison.OrdinalIgnoreCase));

                    // Remove all of the novels from the list if user is searching for manga
                while (titleData.MoveNext())
                {
                    priceData.MoveNext();
                    descData.MoveNext();
                    stockStatusData.MoveNext();

                    XPathNavigator? curTitleData = titleData.Current;
                    if (curTitleData is null) continue;

                    entryTitle = WebUtility.HtmlDecode(curTitleData.Value);
                    entryDesc = descData.Current!.Value;

                    if (
                        InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && (
                                (
                                    bookType == BookType.Manga
                                    && (
                                        InternalHelpers.RemoveEntryTitleCheck(bookTitle, entryTitle, "Novel")
                                        || entryTitle.Contains("graphic novel", StringComparison.OrdinalIgnoreCase)
                                        )
                                    && !entryTitle.Contains("Chapter Book", StringComparison.OrdinalIgnoreCase)
                                    && (
                                        oneShotCheck ||
                                        FixVolumeRegex().IsMatch(entryTitle) ||
                                        entryDesc.ContainsAny(["Collection", "volumes", "color edition", "box set"]) ||
                                        (entryTitle.Any(char.IsDigit) && !bookTitle.Any(char.IsDigit)))
                                    && !(
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony") ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear Your Own World") ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, ["Itachi's Story", "Boruto"]) ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Attack on Titan", entryTitle, ["Kuklo", "end of the world"]) ||
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented")
                                    )
                                )
                                ||
                                (
                                    bookType == BookType.LightNovel
                                    && !entryTitle.Contains("graphic novel", StringComparison.OrdinalIgnoreCase)
                                    && entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                )
                            )
                        )
                    {
                        LOGGER.Debug("BEFORE = {Title}", entryTitle);
                        entryTitle = ParseAndCleanTitle(entryTitle, bookType, bookTitle, entryDesc, oneShotCheck);
                        LOGGER.Debug("AFTER = {Title}", entryTitle);

                        if (!data.AsValueEnumerable().Any(entry => entry.Entry.Equals(entryTitle, StringComparison.OrdinalIgnoreCase)))
                        {
                            data.Add(
                                new EntryModel(
                                    entryTitle,
                                    priceData.Current!.Value.Trim(),
                                    stockStatusData.Current!.Value.Trim().AsSpan(STATUS_START_INDEX) switch
                                    {
                                        "In stock at the Fulfilment Center." => StockStatus.IS,
                                        "Available for Pre Order" => StockStatus.PO,
                                        "Out of stock." => StockStatus.OOS,
                                        "Available for order from suppliers." => StockStatus.BO,
                                        _ => StockStatus.NA
                                    },
                                    TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Info("Removed (2) {}", entryTitle);
                        }
                    }
                    else
                    {
                        LOGGER.Info("Removed (1) {}", entryTitle);
                    }
                }

                if (curPageNum != maxPageCount)
                {
                    curPageNum++;
                    await page.Locator("p.pagerArrowR").ForceClickAsync();
                    await WaitForPageLoad(page);
                    LOGGER.Info("Page {} = {}", curPageNum, page.Url);
                }
                else
                {
                    break;
                }
            }

            data.TrimExcess();
            data.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        catch (Exception ex)
        {
            LOGGER.Error(ex, "{Title} ({BookType}) Error @ {TITLE}", bookTitle, bookType, TITLE);
        }

        return (data, links);
    }
}