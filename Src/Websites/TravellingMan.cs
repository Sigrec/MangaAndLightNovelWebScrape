using System.Collections.Frozen;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class TravellingMan : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    /// <inheritdoc />
    public const string TITLE = "TravellingMan";
    /// <inheritdoc />
    public const string BASE_URL = "https://travellingman.com";
    /// <inheritdoc />
    public const Region REGION = Region.Britain;

    private static readonly List<string> _descRemovalStrings = ["novel", "figure", "sculpture", "collection of", "figurine", "statue", "miniature", "Figuarts"];

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/div/div[2]/div/span");
    private static readonly XPathExpression _priceXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/div/div[3]/dl/div[2]/dd[2]/span[1]");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//ul[@class='list--inline pagination']/li[3]/a");

    [GeneratedRegex(@"Volume|Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@",| The Manga| Manga|\(.*?\)", RegexOptions.IgnoreCase)] private static partial Regex CleanAndParseTitleRegex();
    [GeneratedRegex(@"\d{1,3}-in-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"(?<=Box Set \d{1,3})[^\d{1,3}.]+.*|(?:Box Set) Vol")] private static partial Regex BoxSetRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, null);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.SciFier, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(string bookTitle, BookType bookType, int curPage)
    {
        // https://travellingman.com/search?page=2&q=naruto+manga
        string url = $"{BASE_URL}/search?page={curPage}&q={InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"))}{(bookType == BookType.Manga ? "+manga" : "+novel")}";
        LOGGER.Info("Url {} => {}", curPage, url);
        return url;
    }

    private static string CleanAndParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (OmnibusRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
        }
        else
        {
            if (BoxSetRegex().IsMatch(entryTitle))
            {
                entryTitle = BoxSetRegex().Replace(entryTitle, "Box Set");

                // Trim trailing "... Box Set" if present (span-based)
                ReadOnlySpan<char> es = entryTitle.AsSpan();
                ReadOnlySpan<char> boxSet = "Box Set".AsSpan();
                if (es.EndsWith(boxSet, StringComparison.Ordinal))
                {
                    int last = es.LastIndexOf(boxSet, StringComparison.Ordinal);
                    if (last >= 0) entryTitle = entryTitle.Substring(0, last);
                }
            }
        }

        // Build mutable title once after regex cleanup
        StringBuilder curTitle = new(CleanAndParseTitleRegex().Replace(entryTitle, string.Empty));
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

        // Precompute span views for cheap checks
        ReadOnlySpan<char> entrySpan = entryTitle.AsSpan();
        ReadOnlySpan<char> bookSpan = bookTitle.AsSpan();

        // Remove " Hardcover"/" HC" only when present in entry but not in book (case-sensitive, matches original)
        if (entrySpan.Contains("Hardcover".AsSpan(), StringComparison.Ordinal) &&
            !bookSpan.Contains("Hardcover".AsSpan(), StringComparison.Ordinal))
        {
            curTitle.Replace(" Hardcover", string.Empty);
        }

        if (entrySpan.Contains("HC".AsSpan(), StringComparison.Ordinal) &&
            !bookSpan.Contains("HC".AsSpan(), StringComparison.Ordinal))
        {
            curTitle.Replace(" HC", string.Empty);
        }

        // AoT box-set numerals (keep original case behavior: Contains is CI, Replace is exact token)
        if (entrySpan.Contains("Box Set".AsSpan(), StringComparison.Ordinal) &&
            bookTitle.Equals("attack on titan", StringComparison.OrdinalIgnoreCase))
        {
            if (entrySpan.Contains("One".AsSpan(), StringComparison.OrdinalIgnoreCase))  curTitle.Replace("One", "1");
            else if (entrySpan.Contains("Two".AsSpan(), StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Two", "2");
            else if (entrySpan.Contains("Three".AsSpan(), StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Three", "3");
        }

        if (bookType == BookType.LightNovel)
        {
            curTitle.Replace("Light Novel", string.Empty);

            // Insert "Novel " before "Vol" if present, else append " Novel" (avoid ToString allocation)
            int volIdx = curTitle.IndexOfOrdinal("Vol");
            if (volIdx != -1) curTitle.Insert(volIdx, "Novel ");
            else curTitle.Insert(curTitle.Length, " Novel");
        }
        else if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace("Naruto Next Generations", string.Empty);
        }

        string result = curTitle.ToString();

        // Keep your custom ContainsAny extension untouched
        if (!result.ContainsAny(["Box Set", "Anniversary"]))
        {
            result = MasterScrape.FinalCleanRegex().Replace(result, string.Empty);
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(result, " ").Trim();
    }

    // TODO - Page source issue when a series has multiple pages, unsure why
    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            HtmlWeb web = HtmlFactory.CreateWeb();
            HtmlDocument doc = HtmlFactory.CreateDocument();

            int nextPage = 1;
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            for (int x = 0; x < _descRemovalStrings.Count; x++)
            {
                if (bookTitle.Contains(_descRemovalStrings[x])) _descRemovalStrings.RemoveAt(x);
            }
            if (bookType == BookType.LightNovel) _descRemovalStrings.Remove("novel");


            while (true)
            {
                // Initialize the html doc for crawling
                string url = GenerateWebsiteUrl(bookTitle, bookType, nextPage);
                links.Add(url);
                
                doc = await web.LoadFromWebAsync(url);

                // Get the page data from the HTML doc
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(_titleXPath);
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(_priceXPath);
                HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(_pageCheckXPath);
                if (priceData == null) { goto Stop; }

                for (int x = 0; x < priceData.Count; x++)
                {
                    string entryTitle = titleData[x].InnerText.Trim();
                    if (!entryTitle.ContainsAny(["Banpresto", "Nendoroid"])
                        && InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && (
                            (bookType == BookType.Manga
                            && (
                                !bookTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) &&
                                !entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                && !(
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                    )
                                )
                            )
                            ||
                            bookType == BookType.LightNovel &&
                            !entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase) &&
                            !bookTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase)
                            && !(
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Kharadron")
                                )
                            )
                        && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "Unimplemented")
                        )
                    {
                        bool descIsValid = true;

                        if (bookType == BookType.LightNovel || !entryTitle.ContainsAny(["Vol.", "Volume", "Box Set", "Comic"]))
                        {
                            HtmlNodeCollection descData = (await web.LoadFromWebAsync($"{BASE_URL}{doc.DocumentNode.SelectSingleNode($"(//li[@class='list-view-item']/div/a)[{x + 1}]").GetAttributeValue("href", string.Empty)}")).DocumentNode.SelectNodes("//div[@class='product-single__description rte'] | //div[@class='product-single__description rte']//p");
                            StringBuilder desc = new();
                            foreach (HtmlNode node in descData) { desc.AppendLine(node.InnerText); }
                            if (bookType == BookType.LightNovel)
                            {
                                descIsValid = entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) || desc.ToString().Contains("novel", StringComparison.OrdinalIgnoreCase);
                            }
                            else
                            {
                                descIsValid = !desc.ToString().ContainsAny(_descRemovalStrings);
                            }
                        }

                        if (descIsValid)
                            {
                                data.Add(
                                    new EntryModel
                                    (
                                        CleanAndParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                        priceData[x].InnerText.Trim(),
                                        StockStatus.IS,
                                        TITLE
                                    )
                                );
                            }
                            else { LOGGER.Info("Removed (2) {}", entryTitle); }
                    }
                    else { LOGGER.Info("Removed (1) {}", entryTitle); }
                }

            Stop:
                if (priceData != null && priceData.Count == titleData.Count && pageCheck != null)
                {
                    nextPage++;
                }
                else
                {
                    break;
                }
            }

            data.Sort(EntryModel.VolumeSort);
            data.RemoveDuplicates(LOGGER);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        catch (Exception ex)
        {
            LOGGER.Error("{} ({}) Error @ {} \n{}", bookTitle, bookType, TITLE, ex);
        }

        return (data, links);
    }
}