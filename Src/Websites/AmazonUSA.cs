using System.Collections.Frozen;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class AmazonUSA : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
    public const string WEBSITE_LINK = "https://www.amazon.com/Manga-Comics-Graphic-Novels-Books/b?ie=UTF8&node=4367";

    /// <inheritdoc />
    public const string TITLE = "Amazon USA";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.amazon.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    private static readonly FrozenSet<string> _titleRemovalStrings
        = new[]
        {
            "Kindle", "Manga Set", "Book Set", "Books Set",
            "Collection Set", "Novels Set", "Series Set",
            "books Collection", "Novel Set", "ESPINAS",
            "nº", "BOOKS COVER", "Free Comic Book",
            "n.", "v. ", "CN:", "Reedición",
            "Català", "SHONEN JUMP", "Adventure", "Collection"
        }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> _validPriceStrings
        = new[]
        {
            "Paperback",
            "Hardcover"
        }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@role='listitem']//div[@data-cy='title-recipe']/a//span"); 
    private static readonly XPathExpression EntryInfoXPath = XPathExpression.Compile("//div[@role='listitem']//div[@data-cy='price-recipe']/parent::div");
    private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//span[@class='s-pagination-strip']/ul/span[last()] | //span[@class='s-pagination-strip']/ul/li[(last() - 1)]/span/a");

    [GeneratedRegex(@"\[.*\]|\((?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,3}\s\d{4}\)|\(\s+\d{4}\s+\)|\d{4}-\d{1,2}-\d{1,2}|(?:Korean|German|Japanese|Spanish|French|Italian) Edition")] internal static partial Regex EntryTitleCheckRegex();
    [GeneratedRegex(@"\(.*?\)$|(?<=Vol\s+\d{1,3}.).*|―The Manga|The Manga| Manga", RegexOptions.IgnoreCase)] internal static partial Regex FormatMangaTitleRegex();
    [GeneratedRegex(@"\(Light Novel\)|\(Novel\)|\(.* Novel(?:\)|s\))|(?<=Vol\s+\d{1,3}):.*", RegexOptions.IgnoreCase)] internal static partial Regex FormatNovelTitleRegex();
    [GeneratedRegex(@"(?:Paperback|Hardcover)[\D]*(\$\d{1,3}\.\d{1,2})", RegexOptions.IgnoreCase)] internal static partial Regex GetPriceRegex();
    [GeneratedRegex(@"Save\s(\$\d{1,3}\.\d{1,2})", RegexOptions.IgnoreCase)] internal static partial Regex GetCouponRegex();
    [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] internal static partial Regex FormatVolumeRegex();
    [GeneratedRegex(@"\d{1,3}-\d{1,3}-(\d{1,3})|\d{1,3}-(\d{1,3})")] private static partial Regex OmnibusCheckRegex();
    [GeneratedRegex(@"\((?:Omnibus|\d{1}-in-\d{1}) Edition\)|\d{1}-in-\d{1} Edition|\d{1}-in-\d{1}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
    [GeneratedRegex(@":.*")] private static partial Regex ColonFixRegex();
    [GeneratedRegex(@"\((.*)\)|:(.*)", RegexOptions.IgnoreCase)] private static partial Regex ExtractTextRegex();
    [GeneratedRegex(@"(\d{1,3}) Special Edition.*", RegexOptions.IgnoreCase)] private static partial Regex SpecialEditionRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            IPage page = await PlaywrightFactory.GetPageAsync(browser!);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.AmazonUSA, Links[0]);
        });
    }

    private string GenerateWebsiteUrl(BookType bookType, uint curPage, string bookTitle)
    {
        string url = string.Empty;
        if (bookType == BookType.Manga)
        {
            url = $"{BASE_URL}/s?k={bookTitle.Replace(" ", "+")}&i=stripbooks&rh=n%3A4367%2Cp_n_feature_twenty-five_browse-bin%3A3291437011&dc&qid=1737329343&rnid=3291435011&xpid=Ef0ZD0_P2z6bY&ref=sr_nr_p_n_feature_twenty-five_browse-bin_1&ds=v1%3AYfGmdgzyqhb9%2B1eRHQnAhWhv0FWR5vsvtbRzuVaB6b0&page={curPage}";
        }
        else if (bookType == BookType.LightNovel)
        {
            // url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}+%28light+novel%29&i=stripbooks&rh=n%3A283155%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page={curPage}&crid=37IOUUTCBCGQW&qid=1719069790&rnid=1294421011&ref=sr_pg_{curPage}";

            url = $"{BASE_URL}/s?k={bookTitle.Replace(" ", "+")}+%28light+novel%29&i=stripbooks&rh=n%3A283155%2Cp_n_feature_twenty-five_browse-bin%3A3291437011&dc&ds=v1%3AFRPeeHRJgdHaBhqfCUtrUe8N2WmARxSv1FZdQ%2FAbsIc&crid=21DLI0M90JJLE&qid=1737329715&rnid=3291435011&sprefix={bookTitle.Replace(" ", "+")}+light+novel+%2Cstripbooks%2C153&ref=sr_nr_p_n_feature_twenty-five_browse-bin_1";
        }

        LOGGER.Info(url);
        return url;
    }

    private static string CleanAndParseTitle(string entryTitle, BookType bookType, string bookTitle)
    {
        string insertString = string.Empty;
        if (OmnibusFixRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusFixRegex().Replace(entryTitle, "Omnibus");
        }
        else if (!entryTitle.Contains("Box Set") && !entryTitle.Contains("Omnibus"))
        {
            Match omniCheck = OmnibusCheckRegex().Match(entryTitle);
            if (omniCheck.Success)
            {
                GroupCollection omniVol = omniCheck.Groups;
                int volNum = -1;
                for(int i = 1; i < omniVol.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(omniVol[i].Value))
                    {
                        volNum = Convert.ToInt32(omniVol[i].Value) / 3;
                        break;
                    }
                }
                insertString = $" Omnibus Vol {volNum}";
                if (entryTitle.Contains(':'))
                {
                    entryTitle = entryTitle.Insert(entryTitle.IndexOf(':'), insertString);
                }
                else
                {
                    entryTitle = entryTitle[..entryTitle.IndexOf("Vol")] + insertString;
                }
            }
        }

        if (bookType == BookType.Manga)
        {
            if (entryTitle.Contains("Naruto Chapter Book"))
            {
                entryTitle = ExtractTextRegex().Match(entryTitle).Groups[1].Value;
            }
            else if (entryTitle.Contains(':') && char.IsDigit(entryTitle[^1]) && !entryTitle.Contains("Includes") && !entryTitle[..entryTitle.IndexOf(':')].Contains("Vol"))
            {
                entryTitle = entryTitle.Insert(entryTitle.LastIndexOf(':'), ExtractTextRegex().Match(entryTitle).Groups[2].Value);
            }
            entryTitle = FormatMangaTitleRegex().Replace(entryTitle, string.Empty);
        }
        else if (bookType == BookType.LightNovel)
        {
            entryTitle = FormatNovelTitleRegex().Replace(entryTitle, "Novel");
        }

        int colonIndex = entryTitle.IndexOf(':');
        if (!bookTitle.Contains(':') && entryTitle.Contains(':') && !entryTitle.Contains("Box Set") && (!entryTitle[colonIndex..].Contains("Vol ") || entryTitle[..colonIndex].Contains("Vol ")) && entryTitle.Any(char.IsDigit))
        {
            entryTitle = ColonFixRegex().Replace(entryTitle, string.Empty);
        }

        if (entryTitle.Contains("Special Edition"))
        {
            entryTitle = SpecialEditionRegex().Replace(entryTitle, "Vol $1 Special Edition");
        }

        StringBuilder parsedTitle = new StringBuilder(entryTitle).Replace(",", "");
        InternalHelpers.ReplaceTextInEntryTitle(ref parsedTitle, bookTitle, "Books", "Book");
        InternalHelpers.RemoveCharacterFromTitle(ref parsedTitle, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref parsedTitle, bookTitle, "―", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref parsedTitle, bookTitle, "-", " ");
        parsedTitle.TrimEnd();

        if (bookType == BookType.LightNovel)
        {
            if (entryTitle.Contains("Vol") && parsedTitle.ToString().EndsWith("Novel"))
            {
                parsedTitle.Remove(parsedTitle.Length - 6, 6);
            }

            if (!parsedTitle.ToString().Contains("novel", StringComparison.OrdinalIgnoreCase))
            {
                Match match = MasterScrape.FindVolWithNumRegex().Match(parsedTitle.ToString());
                if (match.Success)
                {
                    int index = match.Index;
                    if (index > 0)
                    {
                        parsedTitle.Insert(index - 1, " Novel ");
                    }
                    else if (index == 0)
                    {
                        parsedTitle.Append(" Novel");
                    }
                }
            }
        }

        if (entryTitle.Contains("Deluxe") && !entryTitle.Contains("Deluxe Edition") && !bookTitle.Contains("Deluxe"))
        {
            parsedTitle.Replace("Deluxe", "Deluxe Edition");
        }

        if (bookTitle.Contains("Boruto", StringComparison.CurrentCultureIgnoreCase))
        {
            parsedTitle.Replace(" Naruto Next Generations", string.Empty);
        }

        if (char.IsDigit(parsedTitle[^1]) && !parsedTitle.ToString().Contains("Vol") && !parsedTitle.ToString().Contains("Box Set"))
        {
            parsedTitle = parsedTitle.Insert(MasterScrape.FindVolNumRegex().Match(parsedTitle.ToString()).Index, "Vol ");
        }

        parsedTitle.TrimEnd();
        if (entryTitle.Contains("Box Set") && !entryTitle.Contains("Season") && !entryTitle.Contains("Part") && !entryTitle.Contains("Compelte Box Set") && !char.IsDigit(parsedTitle[parsedTitle.Length - 1]))
        {
            parsedTitle.Append(" 1");
        }

        if (bookTitle.Contains("Adventure of Dai"))
        {
            parsedTitle.Replace(" Disciples of Avan", string.Empty);
        }
        parsedTitle.TrimEnd([ ':' ]);

        // LOGGER.Debug("AFTER = {}", MasterScrape.MultipleWhiteSpaceRegex().Replace(parsedTitle.ToString(), " ").Trim());
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(parsedTitle.ToString(), " ").Trim();
    }

    // TODO - Need to finish checking tests and cleaning
    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            uint curPage = 1;
            string url = GenerateWebsiteUrl(bookType, curPage, bookTitle);
            links.Add(url);

            HtmlDocument doc = HtmlFactory.CreateDocument();

            HtmlNodeCollection pageNums = doc.DocumentNode.SelectNodes(PageCheckXPath);
            uint maxPage = pageNums != null ? Convert.ToUInt32(pageNums.Last().InnerText.Trim()) : 0;
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle, _titleRemovalStrings);

            while (true)
            {
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                HtmlNodeCollection entryInfoData = doc.DocumentNode.SelectNodes(EntryInfoXPath);
                if (titleData is null || entryInfoData is null)
                {
                    break;
                }

                HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);

                LOGGER.Debug("{} | {}", titleData != null ? titleData.Count : "null", entryInfoData != null ? entryInfoData.Count : "null");

                int entryCount = Math.Min(titleData!.Count, entryInfoData!.Count);
                for (int x = 0; x < entryCount; x++)
                {
                    string entryTitle = titleData[x].InnerText.Trim();
                    string entryInfo = entryInfoData[x].InnerText.Trim();
                    LOGGER.Debug("BEFORE ({}) = {} | {}", x, entryTitle, entryInfo);

                    if (InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle, _titleRemovalStrings) || BookTitleRemovalCheck)
                        && !EntryTitleCheckRegex().IsMatch(entryTitle)
                        && entryInfo.ContainsAny(_validPriceStrings)
                        && (
                            (
                                bookType == BookType.Manga
                                && !(
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "naruto", entryTitle, ["boruto", "Dragon Rider", "Turtle"])
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "one piece", entryTitle, "joy boy")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "bleach", entryTitle, "maximum bleach")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "starfall")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "berserk", entryTitle, ["gluttony", "flame dragon knight", "darkness ink"])
                                )
                            )
                            ||
                            (
                                bookType == BookType.LightNovel
                                && !entryTitle.ContainsAny(["(Manga)", "Graphic Novel"])
                            // && (entryTitle.Contains("(Novel)", StringComparison.CurrentCultureIgnoreCase) || entryTitle.Contains("Light Novel", StringComparison.CurrentCultureIgnoreCase) || entryTitle.Contains("Novels", StringComparison.CurrentCultureIgnoreCase))
                            )
                        )
                    )
                    {
                        string price = string.Empty;
                        if (entryInfo.Contains('$') && (entryInfo.Contains("Paperback", StringComparison.OrdinalIgnoreCase) || entryInfo.Contains("Hardcover", StringComparison.OrdinalIgnoreCase)))
                        {
                            price = GetPriceRegex().Match(entryInfo).Groups[1].Value.Trim();
                        }
                        else
                        {
                            LOGGER.Error("No Valid Price, Entry Info for {} = {}", entryTitle, entryInfo);
                            continue;
                        }

                        if (entryInfo.Contains("Save $", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal coupon = Convert.ToDecimal(GetCouponRegex().Match(entryInfo).Groups[1].Value.TrimStart('$'));
                            LOGGER.Info("Applying Coupon {} to {} for {}", coupon, entryTitle, price.TrimStart('$'));
                            price = $"${InternalHelpers.ApplyCoupon(Convert.ToDecimal(price.TrimStart('$')), coupon)}";
                        }

                        if (!string.IsNullOrWhiteSpace(price))
                        {
                            data.Add(
                                new EntryModel
                                    (
                                        CleanAndParseTitle(FormatVolumeRegex().Replace(entryTitle, "Vol"), bookType, bookTitle),
                                        price.Trim(),
                                        entryInfo switch
                                        {
                                            string curStatus when curStatus.Contains("Temporarily out of stock", StringComparison.OrdinalIgnoreCase) => StockStatus.OOS,
                                            string curStatus when curStatus.Contains("Pre-order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                            _ => StockStatus.IS,
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

                if (curPage < maxPage)
                {
                    url = GenerateWebsiteUrl(bookType, ++curPage, bookTitle);
                    links.Add(url);
                    // driver.Navigate().GoToUrl(url);
                    // wait.Until(driver => driver.FindElement(By.XPath("//div[@class='s-main-slot s-result-list s-search-results sg-row']/div[last()]")));
                    // doc.LoadHtml(driver.PageSource);
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
            data.RemoveDuplicates(LOGGER);

            if (bookType == BookType.Manga && data.Any(entry => entry.Entry.ContainsAny(["Vol", "Box Set"])))
            {
                data.RemoveAll(entry => entry.Entry.Equals(bookTitle, StringComparison.OrdinalIgnoreCase));
            }
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }

        return (data, links);
    }
}