using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class BooksAMillion : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//div[@class='search-item-title']/a");
    private static readonly XPathExpression _descXPath = XPathExpression.Compile("//div[@id='pdpOverview']/div/div");
    private static readonly XPathExpression _bookQualityXPath = XPathExpression.Compile("//div[@class='productInfoText']");
    private static readonly XPathExpression _pricexPath = XPathExpression.Compile("//span[@class='our-price']");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//div[@class='availability_search_results']");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//ul[@class='search-page-list']//a[@title='Next']");

    [GeneratedRegex(@"V\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex MangaRemovalRegex();
    [GeneratedRegex(@"(?<=Box Set).*|:|\!|,|Includes.*|--The Manga|The Manga|\d+-\d+|\(Manga\) |(?<=Omnibus\s\d{1,3})[^\d.].*|\d{1,3}\s+\d{1,3}\s+\&\s+(\d{1,3})|\d{1,3},\s+\d{1,3}\s+\&\s+(\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex MangaFilterTitleRegex();
    [GeneratedRegex(@":|\!|,|Includes.*|\d+-\d+|\d+, \d+ \& \d+", RegexOptions.IgnoreCase)] private static partial Regex NovelFilterTitleRegex();
    [GeneratedRegex(@"(?<=Vol\s+\d+)[^\d\.].*|\(.*?\)$|\[.*?\]|Manga ", RegexOptions.IgnoreCase)] private static partial Regex CleanFilterTitleRegex();
    [GeneratedRegex(@"Box Set (\d+)", RegexOptions.IgnoreCase)] private static partial Regex BoxSetNumberRegex();
    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|3-In-1 V\d+|Vols\.|\d{1,3}-In-\d{1,3}|\d{1,3}-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"3-In-1 V(\d+)|\d{1,3}-In-\d{1,3}|(?:\d{1,3}-(\d{1,3}))$|\d{1,3},\s+\d{1,3}\s+\&\s+(\d{1,3})|\d{1,3}\s+\d{1,3}\s+\&\s+(\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex OmnibusMatchRegex();
    [GeneratedRegex(@"Vol\.|Volumes|Volume|Vols\.|Vols", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

    /// <inheritdoc />
    public const string TITLE = "Books-A-Million";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.booksamillion.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    private const decimal MEMBERSHIP_DISCOUNT = 0.1M;

    private static readonly FrozenSet<string> _mangaDescExcludeVals = [ "Novel", ];
    private static readonly FrozenSet<string> _mangaIncludeVals = [ "Vol", "Box Set", "BOXSET", "Comic", "Anniversary" ];
    private static readonly FrozenSet<string> _boxSetIncludeVals = ["Boxset", "Box Set"];
    private static readonly FrozenSet<string> _novelIncludeVals = [ "Light Novel", "Novel", ];
    private static readonly FrozenSet<string> _novelExcludeVals = [ "Manga", "Volumes", "Vol" ];

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships)
    {
        return Task.Run(async () =>
        {
            IPage page = await PlaywrightFactory.GetPageAsync(browser!, true);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page, memberships.IsBooksAMillionMember);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.BooksAMillion, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(string bookTitle, bool boxSetCheck, BookType bookType, int pageNum)
    {
        // Initialize a StringBuilder
        StringBuilder stringBuilder = new($"{BASE_URL}/search2?");

        bookTitle = InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"));
        if (bookType == BookType.LightNovel)
        {
            // https://www.booksamillion.com/search2?query=classroom+of+the+elite+light+novel&filters%5Bproduct_type%5D=Books
            // https://www.booksamillion.com/search2?query=Overlord+light+novel
            // https://www.booksamillion.com/search2?query=Overlord+light+novel;filters[product_type]=Books&page=1
            stringBuilder.Append($"query={bookTitle}");
            stringBuilder.Append($"{(boxSetCheck ? "+light+novel+box+set" : "+light+novel")};filters[product_type]=Books;page={pageNum}");
        }
        else
        {
            // https://www.booksamillion.com/search2?query=2.5+dimensional+seduction;filters[product_type]=Books&filters[content_lang]=English

            stringBuilder.Append($"query={bookTitle}{(boxSetCheck ? "manga+box+set" : "+manga")};filters[product_type]=Books&filters[content_lang]=English;page={pageNum}");
        }

        // Convert StringBuilder to string
        string url = stringBuilder.ToString();

        return url;
    }

    private static string CleanAndParseTitle(string entryTitle, BookType bookType, string bookTitle)
    {
        StringBuilder curTitle;

        if (bookType == BookType.LightNovel)
        {
            entryTitle = CleanFilterTitleRegex().Replace(NovelFilterTitleRegex().Replace(entryTitle, string.Empty), string.Empty);
            curTitle = new StringBuilder(entryTitle.Length)
                .Append(entryTitle)
                .Replace("(Novel)", "Novel")
                .Replace("(Light Novel)", "Novel");

            ReadOnlySpan<char> curSpan = curTitle.ToString();

            if (!curSpan.Contains("Novel", StringComparison.OrdinalIgnoreCase))
            {
                int volIndex = curSpan.IndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                int boxSetIndex = curSpan.IndexOf("Box Set", StringComparison.OrdinalIgnoreCase);

                if (volIndex != -1)
                    curTitle.Insert(volIndex, "Novel ");
                else if (boxSetIndex != -1)
                    curTitle.Insert(boxSetIndex, "Novel ");
                else
                    curTitle.Append(" Novel");
            }
        }
        else
        {
            ReadOnlySpan<char> rawSpan = entryTitle;

            if (rawSpan.Contains("Omnibus", StringComparison.CurrentCultureIgnoreCase) || 
                rawSpan.Contains("3-in-1", StringComparison.CurrentCultureIgnoreCase) || 
                rawSpan.Contains("2-in-1", StringComparison.CurrentCultureIgnoreCase))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
            }

            curTitle = new StringBuilder(CleanFilterTitleRegex().Replace(MangaFilterTitleRegex().Replace(entryTitle, string.Empty), string.Empty));
            string entryTitleCleaned = curTitle.ToString().Trim();
            ReadOnlySpan<char> cleanedSpan = entryTitleCleaned;

            if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace("Naruto Next Generations", string.Empty);
            }

            ReadOnlySpan<char> entrySpan = entryTitle;

            if (entrySpan.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || entrySpan.Contains("BOXSET", StringComparison.OrdinalIgnoreCase))
            {
                if (entryTitleCleaned.Contains("V01-V27", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("NARUTO BOXSET V01-V27", "Naruto Box Set 1");
                }
                else
                {
                    string boxSetNum = BoxSetNumberRegex().Match(entryTitle).Groups[1].Value;
                    if (!bookTitle.ContainsAny(["attack on titan"]))
                    {
                        curTitle.Append(' ');
                        curTitle.Append(!string.IsNullOrWhiteSpace(boxSetNum) ? boxSetNum : "1");
                    }
                }
            }
            else if (OmnibusMatchRegex().IsMatch(entryTitle))
            {
                var match = OmnibusMatchRegex().Match(entryTitle);
                GroupCollection groups = match.Groups;
                string firstOmniNum = groups[1].Value.TrimStart('0');
                string secondOmniNum = groups[2].Value;
                string thirdOmniNum = groups[3].Value;

                LOGGER.Debug("{} | {} | {} | {}", entryTitleCleaned, firstOmniNum, secondOmniNum, thirdOmniNum);

                if (!cleanedSpan.Contains(" Omnibus", StringComparison.OrdinalIgnoreCase))
                {
                    int volPos = cleanedSpan.IndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                    if (volPos != -1)
                        curTitle.Insert(volPos, "Omnibus ");
                }

                if (!cleanedSpan.Contains(" Vol", StringComparison.OrdinalIgnoreCase))
                    curTitle.Append("Vol ");

                ReadOnlySpan<char> trimmed = curTitle.ToString().Trim();
                if (!char.IsDigit(trimmed[^1]))
                {
                    if (!string.IsNullOrWhiteSpace(firstOmniNum))
                    {
                        curTitle.Append(firstOmniNum);
                    }
                    else if (!string.IsNullOrWhiteSpace(secondOmniNum))
                    {
                        curTitle.Append(Math.Ceiling(Convert.ToDecimal(secondOmniNum) / 3));
                    }
                    else if (!string.IsNullOrWhiteSpace(thirdOmniNum))
                    {
                        curTitle.Append(Math.Ceiling(Convert.ToDecimal(thirdOmniNum) / 3));
                    }
                }
            }
            else if (!cleanedSpan.Contains("Vol", StringComparison.OrdinalIgnoreCase) &&
                    !cleanedSpan.Contains("Box Set", StringComparison.OrdinalIgnoreCase) &&
                    MasterScrape.FindVolNumRegex().IsMatch(entryTitleCleaned))
            {
                Match match = MasterScrape.FindVolNumRegex().Match(entryTitleCleaned);
                curTitle.Insert(match.Index, "Vol ");
            }

            if (entrySpan.Contains("Stall", StringComparison.OrdinalIgnoreCase) && 
                !bookTitle.AsSpan().Contains("Stall", StringComparison.OrdinalIgnoreCase))
            {
                string temp = curTitle.ToString();
                int volIndex = temp.LastIndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                if (volIndex != -1)
                    curTitle.Remove(volIndex, curTitle.Length - volIndex);
            }
        }

        string final = curTitle.ToString();

        if (final.AsSpan().Contains("vols.", StringComparison.OrdinalIgnoreCase))
        {
            int index = final.IndexOf("vols.", StringComparison.OrdinalIgnoreCase);
            curTitle.Remove(index, curTitle.Length - index);
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString().Trim(), " ");
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            HtmlDocument doc = HtmlFactory.CreateDocument();
            HtmlDocument descDoc = HtmlFactory.CreateDocument();
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();

            bool boxSetCheck = false, boxsetValidation = false;
            bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            int pageNum = 1;
            string curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, pageNum);
            LOGGER.Info($"Initial Url {curUrl}");
            links.Add(curUrl);
            
            await page!.GotoAsync(curUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            // Check for promotion popup and clear them if it exist
            IReadOnlyList<IElementHandle> popupContainer = await page.QuerySelectorAllAsync(".ltkpopup-container");
            if (popupContainer.Count > 0)
            {
                await page.ClickAsync(".ltkpopup-close");
            }

            while (true)
            {
                await page.WaitForSelectorAsync(".search-item-title");

                // // Initialize the html doc for crawling
                doc.LoadHtml(await page.ContentAsync());

                // Get the page data from the HTML doc
                XPathNodeIterator titleData = nav.Select(_titleXPath);
                XPathNodeIterator bookQuality = nav.Select(_bookQualityXPath);
                XPathNodeIterator priceData = nav.Select(_pricexPath);
                XPathNodeIterator stockStatusData = nav.Select(_stockStatusXPath);
                XPathNavigator? pageCheck = nav.SelectSingleNode(_pageCheckXPath);

                if (titleData.Count == 0 || bookQuality.Count == 0 || priceData.Count == 0 || stockStatusData.Count == 0)
                {
                    LOGGER.Info("One of the helm node collections returned no data");
                    break;
                }

                while (titleData.MoveNext())
                {
                    bookQuality.MoveNext();
                    priceData.MoveNext();
                    stockStatusData.MoveNext();

                    XPathNavigator? curTitleVal = titleData.Current;
                    if (curTitleVal is null)
                    {
                        LOGGER.Debug("Entry Title = {} is null", curTitleVal);
                        continue;
                    }
                    string? entryTitle = WebUtility.HtmlDecode(curTitleVal.Value.Trim());

                    if (!boxsetValidation &&
                        entryTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) &&
                        entryTitle.ContainsAny(_boxSetIncludeVals) &&
                        (bookType == BookType.Manga ||
                            (
                                bookType == BookType.LightNovel &&
                                !entryTitle.ContainsAny(["Manga", "Volumes", "Vol"]) &&
                                !MangaRemovalRegex().IsMatch(entryTitle)
                            )
                        )
                    )
                    {
                        boxsetValidation = true;
                        continue;
                    }

                    LOGGER.Debug("{} | {} | {} | {} | {}", entryTitle, InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle), bookType == BookType.LightNovel && entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase), bookQuality.Current is null || !bookQuality.Current!.Value.Contains("Library Binding"), (
                                bookType == BookType.LightNovel &&
                                (entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) ||
                                !entryTitle.ContainsAny(_novelExcludeVals) &&
                                !MangaRemovalRegex().IsMatch(entryTitle))
                            ));

                    if (InternalHelpers.ShouldRemoveEntry(entryTitle) && !bookTitleRemovalCheck)
                    {
                        LOGGER.Debug("Removed {}", entryTitle);
                        continue;
                    }

                    if (
                        InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle) &&
                        (bookQuality.Current is null || !bookQuality.Current!.Value.Contains("Library Binding")) &&
                        (
                            titleData.Count == 1 && !boxSetCheck ||
                            (
                                bookType == BookType.Manga &&
                                (
                                    (entryTitle.Equals(bookTitle, StringComparison.OrdinalIgnoreCase) && pageNum == 1) ||
                                    entryTitle.ContainsAny(_mangaIncludeVals) ||
                                    (!entryTitle.ContainsAny(_mangaIncludeVals) &&
                                    (pageNum > 1 || entryTitle.Any(char.IsDigit) &&
                                    !bookTitle.Any(char.IsDigit)))
                                )
                                &&
                                (!(
                                    entryTitle.Contains("(Light Novel", StringComparison.OrdinalIgnoreCase) ||
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony") ||
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, ["harsh mistress"]) ||
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto", "Story", "Team 7 Character", "Dragon Rider")
                                )
                                ||
                                (
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Wanted")
                                ))
                            )
                            ||
                            (
                                bookType == BookType.LightNovel &&
                                (entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) ||
                                !entryTitle.ContainsAny(_novelExcludeVals) &&
                                !MangaRemovalRegex().IsMatch(entryTitle))
                            )
                        )
                    )
                    {
                        if (!entryTitle.ContainsAny(_boxSetIncludeVals) && MangaRemovalRegex().IsMatch(entryTitle))
                        {
                            LOGGER.Debug("Removed (2) {}", entryTitle);
                            continue;
                        }

                        entryTitle = CleanAndParseTitle(
                            FixVolumeRegex().Replace(WebUtility.HtmlDecode(entryTitle), "Vol "),
                            bookType,
                            bookTitle
                        );

                        string? price = priceData.Current?.Value.Trim();
                        if (price is not null && !string.IsNullOrWhiteSpace(price) && decimal.TryParse(price[1..], out decimal priceVal))
                        {
                            ReadOnlySpan<char> stockText = stockStatusData.Current is not null ? stockStatusData.Current!.Value.AsSpan() : string.Empty;
                            
                            StockStatus stockStatus = stockText.Contains("In Stock", StringComparison.OrdinalIgnoreCase) ? StockStatus.IS :
                                stockText.Contains("Preorder", StringComparison.OrdinalIgnoreCase) ? StockStatus.PO :
                                    stockText.Contains("On Order", StringComparison.OrdinalIgnoreCase) ? StockStatus.BO :
                                        StockStatus.OOS;

                            // Check desc to see if a series is a novel when looking for manga titles
                            if (bookType == BookType.Manga && !entryTitle.ContainsAny(_mangaIncludeVals))
                            {
                                string descLink = curTitleVal.GetAttribute("href", string.Empty);
                                LOGGER.Debug("Desc link = {}", descLink);
                                await page!.GotoAsync(descLink, new PageGotoOptions
                                {
                                    WaitUntil = WaitUntilState.DOMContentLoaded
                                });
                                await page.WaitForSelectorAsync("#pdpOverview");
                                
                                descDoc.LoadHtml(await page.ContentAsync());
                                await page!.GotoAsync(curUrl, new PageGotoOptions
                                {
                                    WaitUntil = WaitUntilState.DOMContentLoaded
                                });
                                HtmlNode? desc = descDoc.DocumentNode.SelectSingleNode(_descXPath);
                                if (desc is null || desc.InnerText.ContainsAny(_mangaDescExcludeVals))
                                {
                                    LOGGER.Debug("Removed (3) {}", entryTitle);
                                    continue;
                                }
                            }

                            data.Add(
                                new EntryModel
                                (
                                    entryTitle,
                                    $"${(isMember ? EntryModel.ApplyDiscount(priceVal, MEMBERSHIP_DISCOUNT) : priceVal.ToString())}",
                                    stockStatus,
                                    TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Debug("Removed (4) {}", entryTitle);
                        }
                    }
                    else
                    {
                        LOGGER.Debug("Removed (1) {}", entryTitle);
                    }
                }

                if (pageCheck != null)
                {
                    await page.ClickAsync("//a[@title='Next']");
                    await page.WaitForSelectorAsync("#content");
                    curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, ++pageNum);
                    links.Add(curUrl);
                    LOGGER.Info($"Next Page: {curUrl}");
                }
                else
                {
                    if (boxsetValidation && !boxSetCheck)
                    {
                        boxSetCheck = true;
                        pageNum = 1;
                        curUrl = GenerateWebsiteUrl(bookTitle, boxSetCheck, bookType, pageNum);
                        links.Add(curUrl);
                        LOGGER.Info("Box Set Url: {}", curUrl);
                        await page!.GotoAsync(curUrl, new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.DOMContentLoaded
                        });
                    }
                    else
                    {
                        break;
                    }
                }
            }

            data.TrimExcess();
            links.TrimExcess();
            data.Sort(EntryModel.VolumeSort);
            data.RemoveDuplicates(LOGGER);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        catch (Exception ex)
        {
            LOGGER.Error(ex, "{Title} ({BookType}) Error @ {TITLE}", bookTitle, bookType, TITLE);
            throw;
        }

        return (data, links);
    }
}