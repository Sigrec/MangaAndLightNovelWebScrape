using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class SciFier : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//ul[@class='productGrid']//h3[@class='card-title']/a");
    private static readonly XPathExpression _priceXPath = XPathExpression.Compile("//div[@class='card-body']//span[contains(@class, 'price price--withTax price--main')]");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//a[@aria-label='Next']");
    private static readonly XPathExpression _summaryXPath = XPathExpression.Compile("//div[@class='card-text card-text--summary']");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//div[@class='card-buttons']");
    private static readonly XPathExpression _entryDescXPath = XPathExpression.Compile("(//div[@class='productView-description-tabContent is-open'])[1]");

    [GeneratedRegex(@"(?<=Vol\s+(?:\d{1,3}|\d{1,3}\.\d{1}))[^\d.].+|(?<=Box Set \d{1,3}).*|\(Manga\)|The Manga|Manga", RegexOptions.IgnoreCase)] private static partial Regex TitleFixRegex();
    [GeneratedRegex(@"\s{1}[a-zA-Z]+\s{1}[a-zA-Z]+\s{1}\d{13}|,|\s+by\s+.*$")] private static partial Regex RemoveAuthorAndIdRegex();
    [GeneratedRegex(@"\s{1}[a-zA-Z]+\s{1}\d{13}|,|\s+by\s+.*$")] private static partial Regex RemoveAuthorAndIdSingleRegex();
    [GeneratedRegex(@"(?:Vol|Box Set) \d{1,3}\s{1}([a-zA-Z]+)\s{1}\d{13}", RegexOptions.IgnoreCase)] private static partial Regex GetAuthorAndIdRegex();
    [GeneratedRegex(@"\s{1}([a-zA-Z]+)\s{1}\d{13}", RegexOptions.IgnoreCase)] private static partial Regex GetAuthorAndIdNoVolRegex();
    [GeneratedRegex(@"\d{1,3}(?!\d*th)")]private static partial Regex GetVolNumRegex();
    [GeneratedRegex(@"\((?:\d{1}-in-\d{1}|Omnibus) Edition\)|:[\w\s]+\d{1,3}-\d{1,3}-\d{1,3}|Omnibus (\d{1,3})")]private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"[$£€]\d{1,3}\.\d{1,2} - ([$£€]\d{1,3}\.\d{1,2})")] private static partial Regex PriceRangeRegex();
    [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"V\d{1,3}")] private static partial Regex TitleRemoveRegex();
    
    /// <inheritdoc />
    public const string TITLE = "SciFier";

    /// <inheritdoc />
    public const string BASE_URL = "https://scifier.com";

    /// <inheritdoc />
    public const Region REGION = Region.America | Region.Europe | Region.Britain | Region.Canada | Region.Australia;

    private static readonly FrozenDictionary<Region, ushort> CURRENCY_DICTIONARY = new Dictionary<Region, ushort>
    {
        {Region.Britain, 1},
        {Region.America, 2},
        {Region.Australia, 3},
        {Region.Europe, 5},
        {Region.Canada, 6}
    }.ToFrozenDictionary();

    private static readonly FrozenSet<string> _checkDescStrings = ["Vol", "Box Set"];

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, null, curRegion: curRegion);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.SciFier, Links[0]);
        });
    }

    // Has issues where the search is not very strict unforunate
    private static string GenerateWebsiteUrl(string bookTitle, BookType bookType, Region curRegion, bool letterIsFrontHalf)
    {
        // https://scifier.com/search.php?setCurrencyId=4&section=product&search_query_adv=jujutsu+kaisen&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2060&section=product

        // https://scifier.com/search.php?setCurrencyId=6&section=product&search_query_adv=classroom+of+the+elite&searchsubs=ON&brand=&price_from=&price_to=&category=2060&limit=100&sort=alphaasc&mode=6

        // https://scifier.com/search.php?search_query_adv=overlord+novel&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2175&section=product&sort=alphadesc&limit=100&mode=6\
        
        string url;
        if (bookType == BookType.Manga)
        {
            url = $"{BASE_URL}/search.php?setCurrencyId={CURRENCY_DICTIONARY[curRegion]}&section=product&search_query_adv={bookTitle.Replace(' ', '+')}&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2060&section=product&limit=100&sort=alpha{(letterIsFrontHalf ? "asc" : "desc")}&mode=6";
        }
        else
        {
            url = $"{BASE_URL}/search.php?setCurrencyId={CURRENCY_DICTIONARY[curRegion]}&search_query_adv={bookTitle.Replace(' ', '+')}+light+novels&searchsubs=ON&brand=&price_from=&price_to=&section=product";
        }

        LOGGER.Info($"Url = {url}");
        return url;
    }

    private static string CleanAndParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        Match omniCheck = OmnibusRegex().Match(entryTitle);
        if (omniCheck.Success)
        {
            if (!string.IsNullOrWhiteSpace(omniCheck.Groups[1].Value))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus Vol $1");
            }
            else
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, " Omnibus");
            }
        }
        if (bookType == BookType.Manga && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Vol"))
        {
            entryTitle = entryTitle.Insert(GetVolNumRegex().Match(entryTitle).Index, "Vol ");
        }

        StringBuilder curTitle = new(TitleFixRegex().Replace(entryTitle, string.Empty));
        string volNum = TitleFixRegex().Match(entryTitle).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(volNum) && !curTitle.ToString().Contains("Vol"))
        {
            curTitle.AppendFormat(" Vol {0}", volNum);
        }
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, '-', ' ');
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Complete ", string.Empty);
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Color Edition", "In Color");
        
        if (entryTitle.Contains("Special Edition"))
        {
            curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index - 4, "Special Edition ");
        }
        if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
        {
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Naruto Next Generations", string.Empty);
        }
        if (entryTitle.StartsWith("Vol "))
        {
            curTitle.Remove(0, 4);
        }
        if (bookTitle.Equals("Bleach", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Vol") && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace("Bleach", "Bleach Vol 40");
        }
        if (bookType == BookType.LightNovel)
        {
            curTitle.Replace("(Light Novel)", "Novel");
        }

        if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Omnibus")  && !curTitle.ToString().Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
        {
            int index = curTitle.ToString().IndexOf("Vol");
            if (index != -1)
            {
                curTitle.Insert(index, "Stray God ");
            }
        }

        if (bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel"))
        {
            int index = curTitle.ToString().IndexOf("Vol");
            if (index != -1)
            {
                curTitle.Insert(index, "Novel ");
            }
            else
            {
                curTitle.Insert(curTitle.Length, " Novel");
            }
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];
        
        try
        {
            HtmlWeb html = HtmlFactory.CreateWeb();

            bool letterIsFrontHalf = char.IsDigit(bookTitle[0]) || (bookTitle[0] & 0b11111) <= 13;
            bool ShouldEndEarly = false, IsSingleName = true;
            string url = GenerateWebsiteUrl(bookTitle, bookType, curRegion, letterIsFrontHalf);
            links.Add(url);

            HtmlDocument doc = await html.LoadFromWebAsync(url);
            HtmlDocument descDoc = HtmlFactory.CreateDocument();
            doc.ConfigurePerf();
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();
            
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);

            while (true)
            {
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(_titleXPath);
                char firstEntryFirstChar = char.ToLowerInvariant(titleData[0].InnerText.TrimStart()[0]);
                char lastEntryFirstChar = char.ToLowerInvariant(titleData[^1].InnerText.TrimStart()[0]);
                char bookTitleFirstChar = char.ToLowerInvariant(bookTitle.TrimStart()[0]);

                HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(_pageCheckXPath);

                if ((letterIsFrontHalf && (firstEntryFirstChar > bookTitleFirstChar)) || (!letterIsFrontHalf && (firstEntryFirstChar < bookTitleFirstChar)))
                {
                    LOGGER.Info($"Ending Scrape Early -> '{lastEntryFirstChar}' {(letterIsFrontHalf ? '>' : '<')} '{firstEntryFirstChar}'");
                    ShouldEndEarly = true;
                    goto EndEarly;
                }
                else if ((letterIsFrontHalf && firstEntryFirstChar < bookTitleFirstChar && lastEntryFirstChar < bookTitleFirstChar) || (!letterIsFrontHalf && firstEntryFirstChar > bookTitleFirstChar && lastEntryFirstChar > bookTitleFirstChar))
                {
                    LOGGER.Debug($"Skipping Page -> '{firstEntryFirstChar}' {(letterIsFrontHalf ? '<' : '>')} '{bookTitleFirstChar}' && '{lastEntryFirstChar}' {(letterIsFrontHalf ? '<' : '>')} '{bookTitleFirstChar}'");
                    goto EndEarly;
                }

                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(_priceXPath);
                HtmlNodeCollection summaryData = doc.DocumentNode.SelectNodes(_summaryXPath);
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(_stockStatusXPath);

                int foundCheck = 0;
                for (int x = 0; x < titleData.Count; x++)
                {
                    // priceData.MoveNext();
                    // summaryData.MoveNext();
                    // stockStatusData.MoveNext();

                    string entryTitle = WebUtility.HtmlDecode(FixVolumeRegex().Replace(titleData[x].InnerText.Trim(), "Vol"));
                    if (
                        (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && !entryTitle.Contains("USED COPY", StringComparison.OrdinalIgnoreCase)
                        && !TitleRemoveRegex().IsMatch(entryTitle)
                        && (
                                (
                                    bookType == BookType.Manga
                                    && !entryTitle.Contains("Novel)", StringComparison.OrdinalIgnoreCase)
                                    && (!summaryData[x].InnerText.Contains("novel", StringComparison.OrdinalIgnoreCase) || entryTitle.Contains("(Manga)") || entryTitle.Contains("Box Set"))
                                    && !(
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Funny Sports")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, ["Overlord Kugane Maruyama 9781975374785", "Unimplemented"])
                                    )
                                )
                                ||
                                (
                                    bookType == BookType.LightNovel
                                    && !entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase)
                                )
                            )
                        )
                    {
                        if (bookType == BookType.LightNovel && !entryTitle.Contains("novel", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!(await html.LoadFromWebAsync(titleData[x].GetAttributeValue("href", "ERROR"))).DocumentNode.SelectSingleNode(_entryDescXPath).InnerText.ContainsAny(["novel series", "series of prose novels"]))
                            {
                                LOGGER.Info("Removed (3) {}", entryTitle);
                                continue;
                            }
                        }

                        foundCheck++;
                        if (foundCheck == 1)
                        {
                            if (entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase))
                            {
                                IsSingleName = !string.IsNullOrWhiteSpace(GetAuthorAndIdRegex().Match(entryTitle).Groups[1].Value);
                            }
                            else
                            {
                                string author = GetAuthorAndIdNoVolRegex().Match(entryTitle).Groups[1].Value;
                                IsSingleName = !string.IsNullOrWhiteSpace(author) && author.Count(char.IsUpper) == 2;
                            }
                            // LOGGER.Debug("IsSingleName = {} | {}", IsSingleName, entryTitle);
                        }
                        entryTitle = !IsSingleName ? RemoveAuthorAndIdRegex().Replace(entryTitle, string.Empty) : RemoveAuthorAndIdSingleRegex().Replace(entryTitle, string.Empty);
                        if (InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle) || InternalHelpers.Similar(bookTitle, entryTitle, bookTitle.Length > entryTitle.Length ? entryTitle.Length / 6 : bookTitle.Length / 6) != -1)
                        {
                            string? price = priceData[x].InnerText.Trim();
                            if (price is null) continue;
                            string priceCheck = PriceRangeRegex().Match(price).Groups[1].Value;

                            string? stockStatus = stockStatusData[x].InnerText.Trim();
                            if (stockStatus is null) continue;

                            // LOGGER.Debug("Before = {} | {} | {}", entryTitle, price, stockStatus);

                            entryTitle = CleanAndParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType);

                            // If it doesn't contain a valid type identifier check its desc for it
                            if (!entryTitle.ContainsAny(_checkDescStrings))
                            {
                                LOGGER.Info("Checking desc for {Title}", entryTitle);
                                HtmlNode descNode = (await html.LoadFromWebAsync(titleData[x].GetAttributeValue<string>("href", "ERROR"))).DocumentNode.SelectSingleNode(_entryDescXPath);
                                if (descNode is not null)
                                {
                                    string desc = descNode.InnerText;
                                    if (desc.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
                                    {
                                        entryTitle += " Box Set";
                                    }
                                }
                            }

                            // LOGGER.Debug("After = {} | {} | {}", entryTitle, price, stockStatus);

                            links.Add(url);
                            data.Add(
                                new EntryModel
                                (
                                    entryTitle,
                                    string.IsNullOrWhiteSpace(priceCheck) ? price : priceCheck,
                                    stockStatus switch
                                    {
                                        string status when status.Contains("Pre-Order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                        string status when status.Contains("Add to Cart", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                        _ => StockStatus.OOS
                                    },
                                    TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Debug("Removed (2) {}", entryTitle);
                        }
                    }
                    else
                    {
                        LOGGER.Debug("Removed (1) {}", entryTitle);
                    }
                }

            EndEarly:
                if (!ShouldEndEarly && pageCheck != null)
                {
                    url = $"https://scifier.com{WebUtility.HtmlDecode(pageCheck.GetAttributeValue("href", "Url Error"))}";
                    doc = await html.LoadFromWebAsync(url);
                    LOGGER.Info($"Next Page => {url}");
                }
                else
                {
                    break;
                }
            }
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
            if (bookType == BookType.Manga && data.Any(entry => entry.Entry.Contains("Vol")))
            {
                data.RemoveAll(entry => !entry.Entry.ContainsAny(["Vol", "Box Set", "Color", "Comic", "Anniversary"]) && !(entry.ParsePrice() > 50));
            }
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }

        return (data, links);
    }
}