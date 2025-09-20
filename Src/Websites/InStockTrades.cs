using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class InStockTrades : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile(".//div[@class='title']/a/text()");
    private static readonly XPathExpression _detailsXPath = XPathExpression.Compile("//div[@class='detail clearfix']");
    private static readonly XPathExpression _priceXPath = XPathExpression.Compile("//div[@class='price']/text()");
    private static readonly XPathExpression PageCheckXPath= XPathExpression.Compile("//input[@id='currentpage']");

    private static readonly FrozenSet<string> _mangaFilters = [ " GN", "Vol", "Box Set", "Manga", "Special Ed", " HC" ];
    private static readonly FrozenSet<string> _novelFilters = [ "Novel", "Sc" ];
    private static readonly FrozenSet<string> _excludeMangaFilters = [ " Novel ", " Sc " ];
    private static readonly FrozenSet<string> _excludeNovelFilters = ["Manga", " GN", " Ed TP"];
    private static readonly FrozenSet<string> _oneShotCheckFilter = [ "Vol", "Box Set", "Manga" ];
    private static readonly FrozenSet<string> _novelMultiples = ["Light Novel", "Novel Sc", "L Novel"];
    private static readonly FrozenSet<string> _edVolMultiples = ["Ed Vol", "Ed HC Vol"];

    [GeneratedRegex(@" GN| TP| HC| Manga|(?<=Vol).*|(?<=Box Set).*|\(.*\)", RegexOptions.IgnoreCase)]  private static partial Regex TitleRegex();
    [GeneratedRegex(@" HC| TP|\(.*\)|(?<=Vol (?:\d{1,3}|\d{1,3}.\d{1,3}) ).*|(?<=Box Set (?:\d{1,3}|\d{1,3}.\d{1,3}) ).*", RegexOptions.IgnoreCase)]  private static partial Regex CleanTitleRegex();
    [GeneratedRegex(@"(?:(?<![\w-])(\d{1,3}(?:\.\d{1,3})?)(?=\s*(?:\(|$))|(?:(?:Vol|GN)|Box\s*Set(?:\s*Part)?)\s+(\d{1,3}(?:\.\d{1,3})?))", RegexOptions.IgnoreCase)] private static partial Regex VolNumberRegex();
    [GeneratedRegex(@"3In1 (?:Ed TP|TP|Ed)|3In1|Omnibus TP", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    
    /// <inheritdoc />
    public const string TITLE = "InStockTrades";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.instocktrades.com/";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships)
    {
        return Task.Run(async () =>
        {
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.InStockTrades, Links[0]);
        });
    }

    // https://www.instocktrades.com/search?term=world+trigger
    // https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
    // https://www.instocktrades.com/search?title=overlord+novel&publisher=&writer=&artist=&cover=&ps=true
    private static string GenerateWebsiteUrl(uint currPageNum, string bookTitle)
    {
        string url = $"{BASE_URL}/search?pg={currPageNum}&title={bookTitle.Replace(' ', '+')}&publisher=&writer=&artist=&cover=&ps=true";
        LOGGER.Info(url);
        return url;
    }

    private static string ParseAndCleanTitle(
        string   bookTitle,
        string   entryTitle,
        BookType bookType)
    {
        ReadOnlySpan<char> entrySpan = entryTitle.AsSpan();
        ReadOnlySpan<char> bookSpan = bookTitle.AsSpan();

        bool isBoxSet = entrySpan.IndexOf("Box Set".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasSeason = entrySpan.IndexOf("Season".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasSpecialEd = entrySpan.IndexOf("Special Ed".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasSpEd = entrySpan.IndexOf("Sp Ed".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasVol = entrySpan.IndexOf("Vol".AsSpan(), StringComparison.Ordinal) >= 0;
        char lastChar = entryTitle[^1];

        bool isOverlord = bookTitle.Equals("Overlord", StringComparison.OrdinalIgnoreCase);
        bool bookHasSpecial = bookSpan.IndexOf("Special Ed".AsSpan(),  StringComparison.OrdinalIgnoreCase) >= 0;

        StringBuilder curTitle = new(entryTitle.Length + 32);
        curTitle.Append(entryTitle);

        string volGroup;
        if (isBoxSet)
        {
            curTitle.Replace("Vol ", string.Empty);
            string snapshot = curTitle.ToString();
            Match match = VolNumberRegex().Match(snapshot);
            volGroup = !string.IsNullOrWhiteSpace(match.Groups[3].Value)
                ? match.Groups[3].Value
                : match.Groups[2].Value;

            if (hasSeason)
            {
                int idx = snapshot.IndexOf("Box Set", StringComparison.Ordinal);
                curTitle.Insert(idx, $"Part {volGroup.TrimStart('0')} ");
            }
        }
        else
        {
            if (isOverlord && entrySpan.IndexOf(" Og ".AsSpan(), StringComparison.Ordinal) >= 0)
            {
                curTitle.Replace("Og", "Oh");
            }

            bool needsVol = !hasVol
                && ((hasSpecialEd && !bookHasSpecial)
                    || char.IsDigit(lastChar)
                    || lastChar == ')');

            if (needsVol)
            {
                string snapshot = curTitle.ToString().Trim();
                Match vm = VolNumberRegex().Match(snapshot);

                if (!char.IsDigit(snapshot[^1]) && !hasSpecialEd)
                {
                    if (!string.IsNullOrWhiteSpace(vm.Groups[1].Value))
                    {
                        curTitle.Insert(vm.Index, $"Vol {vm.Groups[1].Value}");
                    }
                    else if (!string.IsNullOrWhiteSpace(vm.Groups[2].Value))
                    {
                        curTitle.Insert(vm.Index, $"Vol {vm.Groups[2].Value}");
                    }
                }
                else
                {
                    curTitle.Insert(vm.Index, "Vol ");
                }
            }

            if (bookType == BookType.LightNovel
                && entrySpan.IndexOf("Novel".AsSpan(), StringComparison.Ordinal) < 0)
            {
                curTitle.Append(" Novel");
            }
        }

        if (bookType == BookType.Manga)
        {
            InternalHelpers.ReplaceTextInEntryTitle(
                ref curTitle, bookTitle, "Vol GN", "Vol");
            InternalHelpers.ReplaceTextInEntryTitle(
                ref curTitle, bookTitle, " GN", string.Empty);
        }
        else
        {
            InternalHelpers.ReplaceMultipleTextInEntryTitle(
                ref curTitle, bookTitle, _novelMultiples, "Novel");
        }

        InternalHelpers.ReplaceTextInEntryTitle(
            ref curTitle, bookTitle, "One", "1");
        InternalHelpers.ReplaceTextInEntryTitle(
            ref curTitle, bookTitle, "Color HC Ed", "In Color");
        InternalHelpers.ReplaceMultipleTextInEntryTitle(
            ref curTitle, bookTitle, _edVolMultiples, "Edition Vol");
        InternalHelpers.ReplaceTextInEntryTitle(
            ref curTitle, bookTitle, "Colossal Ed", "Colossal Edition");
        InternalHelpers.ReplaceMultipleTextInEntryTitle(
            ref curTitle, bookTitle, new[] { " Annv Book", " Ann" }, " Anniversary Edition");
        InternalHelpers.ReplaceTextInEntryTitle(
            ref curTitle, bookTitle, "Deluxe Edition", "Deluxe");
        InternalHelpers.ReplaceTextInEntryTitle(
            ref curTitle, bookTitle, "Coll", "Collector");

        string mid = curTitle.ToString();
        if (!mid.Contains("Special Edition", StringComparison.Ordinal) 
            && (hasSpecialEd || hasSpEd))
        {
            int idx = mid.IndexOf("Vol", StringComparison.Ordinal);
            curTitle.Insert(idx, "Special Edition ");
        }

        mid = curTitle.ToString();
        if (mid.EndsWith(" Sc", StringComparison.Ordinal) 
            || mid.Contains(" Sc ", StringComparison.Ordinal))
        {
            int ri = mid.LastIndexOf(" Sc", StringComparison.Ordinal);
            curTitle.Remove(ri, 3);
        }

        mid = WebUtility.HtmlDecode(OmnibusRegex().Replace(curTitle.ToString(), "Omnibus"));
        string finalTitle = TitleRegex().Replace(mid, string.Empty).Trim();

        Match finalVol = VolNumberRegex().Match(curTitle.ToString());
        string volumeNum = finalVol.Groups[1].Success
            ? finalVol.Groups[1].Value
            : finalVol.Groups[2].Value;

        if (finalVol.Success && volumeNum.StartsWith('0'))
        {
            return $"{finalTitle} {volumeNum.TrimStart('0')}";
        }
        else if (entrySpan.IndexOf("Season".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0
                && bookSpan.IndexOf("Season".AsSpan(), StringComparison.OrdinalIgnoreCase) < 0)
        {
            return finalTitle;
        }
        else
        {
            return CleanTitleRegex().Replace(mid, string.Empty).Trim();
        }
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(
        string bookTitle,
        BookType bookType,
        IPage? page = null,
        bool isMember = false,
        Region curRegion = Region.America)
    {
        // Normalize ampersand
        if (bookTitle.Contains('&', StringComparison.Ordinal))
        {
            bookTitle = bookTitle.Replace("&", "and");
        }

        uint maxPages = 0;
        uint curPageNum = 1;
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            // First page: build URL, load, and get maxPages
            HtmlWeb html = HtmlFactory.CreateWeb();

            string url = GenerateWebsiteUrl(curPageNum, bookTitle);
            links.Add(url);

            HtmlDocument doc = await html.LoadFromWebAsync(url);
            doc.ConfigurePerf();

            HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
            if (pageCheck != null &&
                uint.TryParse(pageCheck.GetAttributeValue("data-max", "0"), out uint parsed))
            {
                maxPages = parsed;
                links.Capacity = (int)maxPages;
                data.EnsureCapacity((int)maxPages * 10);
            }
            
            bool replaceAdv = !bookTitle.Contains("Adv", StringComparison.Ordinal);
            bool isMangaType = bookType == BookType.Manga;
            bool isNovelType = bookType == BookType.LightNovel;
            bool titleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);

            // Loop through pages
            do
            {
                HtmlNodeCollection details = doc.DocumentNode.SelectNodes(_detailsXPath);
                HtmlNodeCollection prices = doc.DocumentNode.SelectNodes(_priceXPath);

                if (details == null || prices == null)
                {
                    break;
                }

                int count = details.Count;
                data.EnsureCapacity(data.Count + count);

                // Process each entry without LINQ allocations
                for (int i = 0; i < count; i++)
                {
                    HtmlNode titleNode = details[i].SelectSingleNode(_titleXPath);
                    if (titleNode == null)
                    {
                        continue;
                    }

                    string entryTitle = titleNode.InnerText;
                    if (replaceAdv)
                    {
                        entryTitle = entryTitle.Replace(" Adv ", " Adventure ");
                    }
                    LOGGER.Debug("{}", entryTitle);

                    // Precompute flags once
                    bool isOneShot = count == 1 && !entryTitle.ContainsAny(_oneShotCheckFilter);

                    // Book‐title validity
                    bool validBook = (titleRemovalCheck ||
                        InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle))
                        && !details[i].InnerText.Contains("Damaged")
                        && !InternalHelpers.ShouldRemoveEntry(entryTitle);

                    // Manga‐specific validity
                    bool validManga = isMangaType
                        && !entryTitle.ContainsAny(_excludeMangaFilters)
                        && (isOneShot ||
                            entryTitle.ContainsAny(_mangaFilters))
                        && !InternalHelpers.RemoveUnintendedVolumes(
                                bookTitle, "Berserk", entryTitle, "of Gluttony")
                        && !InternalHelpers.RemoveUnintendedVolumes(
                                bookTitle, "Naruto", entryTitle, "Boruto");

                    // Novel‐specific validity
                    bool validNovel = isNovelType
                        && entryTitle.ContainsAny(_novelFilters)
                        && !entryTitle.ContainsAny(_excludeNovelFilters)
                        && !InternalHelpers.RemoveUnintendedVolumes(
                                bookTitle, "overlord", entryTitle, "Joined Party");

                    if (validBook && (validManga || validNovel))
                    {
                        string cleanedTitle = ParseAndCleanTitle(bookTitle, entryTitle, bookType);
                        string trimmedPrice = prices[i].InnerText.Trim();
                        data.Add(new EntryModel(
                            cleanedTitle,
                            trimmedPrice,
                            StockStatus.IS,
                            TITLE)
                        );
                    }
                    else if (LOGGER.IsDebugEnabled)
                    {
                        LOGGER.Debug("Removed {0}", entryTitle);
                    }
                }

                // Next page?
                if (curPageNum < maxPages)
                {
                    curPageNum++;
                    url = GenerateWebsiteUrl(curPageNum, bookTitle);
                    links.Add(url);
                    doc = html.Load(url);
                }
                else
                {
                    break;
                }
            }
            while (true);
        }
        catch (Exception ex)
        {
            LOGGER.Error(ex, "{Title} ({BookType}) Error @ {TITLE}", bookTitle, bookType, TITLE);
        }
        finally
        {
            data.TrimExcess();
            links.TrimExcess();
            data.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }

        return (data, links);
    }
}