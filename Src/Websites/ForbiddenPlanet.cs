using System.Collections.Frozen;
using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class ForbiddenPlanet : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
    
    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//div[@class='full']/ul/li/section/header/div[2]/h3/a/text()");
    private static readonly XPathExpression _priceXPath = XPathExpression.Compile("//span[@class='clr-price']/text()");
    private static readonly XPathExpression _minorPriceXPath = XPathExpression.Compile("(//div[@class='full']/ul/li/section/header/div[2])/p/span[2]");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//div[@class='full']/ul/li/section/header/div/ul");
    private static readonly XPathExpression _bookFormatXPath = XPathExpression.Compile("(//div[@class='full']/ul/li/section/header/div[1])/p[1]");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("(//a[@class='product-list__pagination__next'])[1]");

    [GeneratedRegex(@"The Manga|\(Hardcover\)|:(?:.*):|\(.*\)", RegexOptions.IgnoreCase)] internal static partial Regex CleanAndParseTitleRegex();
    [GeneratedRegex(@"\(Hardcover\)|The Manga", RegexOptions.IgnoreCase)] internal static partial Regex ColorCleanAndParseTitleRegex();
    [GeneratedRegex(@":.*(?:(?:3|2)-In-1|(?:3|2) In 1) Edition\s{0,3}:|:.*(?:(?:3|2)-In-1|(?:3|2) In 1)\s{0,3}:|(?:(?:3|2)-In-1|(?:3|2) In 1)\s{0,3} Edition:|\(Omnibus Edition\)|Omnibus\s{0,}(\d{1,3}|\d{1,3}.\d{1})(?:\s{0,}|:\s{0,})(?:\(.*\)|Vol \d{1,3}-\d{1,3})|:.*Omnibus:\s+(\d{1,3}).*|:.*:\s+Vol\s+(\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
    [GeneratedRegex(@"Box Set:|:\s+Box Set|Box Set (\d{1,3}):|:\s+(\d{1,3}) \(Box Set\)|\(Box Set\)|Box Set Part", RegexOptions.IgnoreCase)] private static partial Regex BoxSetFixRegex();
    [GeneratedRegex(@"(\d{1,3})-\d{1,3}")] private static partial Regex BoxSetVolFindRegex();
    [GeneratedRegex(@"(?<=(?:Vol|Box Set)\s+(?:\d{1,3}|\d{1,3}.\d{1}))[^\d{1,3}.]+.*")] private static partial Regex RemoveAfterVolNumRegex();
    [GeneratedRegex(@"\((.*?Anniversary.*?)\)")] private static partial Regex AnniversaryMatchRegex();
    [GeneratedRegex(@"Volumes|Volume|Vol\.|Volumr", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"(?:\(Deluxe(?::|\s*)|Deluxe\s*Edition:).*", RegexOptions.IgnoreCase)] internal static partial Regex DeluxeEditionRegex();

    /// <inheritdoc />
    public const string TITLE = "Forbidden Planet";

    /// <inheritdoc />
    public const string BASE_URL = "https://forbiddenplanet.com";

    /// <inheritdoc />
    public const Region REGION = Region.Britain;
    
    private static readonly FrozenSet<string> DescRemovalStrings = ["novel", "original stories", "collecting issues"];

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships = default)
    {
        return Task.Run(async () =>
        {            
            IPage page = await PlaywrightFactory.GetPageAsync(browser!, true);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.ForbiddenPlanet, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(BookType bookType, string entryTitle, bool isSecondCategory)
    {
        // https://forbiddenplanet.com/catalog/?q=Naruto&show_out_of_stock=on&sort=release-date-asc&page=1
        string url = $"{BASE_URL}/catalog/{(!isSecondCategory ? "manga" : "comics-and-graphic-novels")}/?q={(bookType == BookType.Manga ? InternalHelpers.FilterBookTitle(entryTitle) : $"{InternalHelpers.FilterBookTitle(entryTitle)}%20light%20novel")}&show_out_of_stock=on&sort=release-date-asc&page=1";
        LOGGER.Info($"Url = {url}");
        return url;
    }

    private static string CleanAndParseTitle(string bookTitle, string entryTitle, BookType bookType)
    {
        entryTitle = FixVolumeRegex().Replace(entryTitle.Trim(), " Vol");
        StringBuilder curTitle;
        if (entryTitle.EndsWith("(Colour Edition Hardcover)"))
        {
            entryTitle = entryTitle.Replace("(Colour Edition Hardcover)", string.Empty).Trim();
            entryTitle = entryTitle.Insert(entryTitle.IndexOf("Vol"), "In Color ");
        }

        if (!entryTitle.Contains("Anniversary") && entryTitle.ContainsAny(["3-In-1", "3 In 1", "Omnibus"]))
        {
            // LOGGER.Debug("(0) {}", entryTitle);
            entryTitle = OmnibusFixRegex().Replace(entryTitle, $" Omnibus $1$2$3");
            curTitle = new StringBuilder(entryTitle);

            if (!entryTitle.Contains("Omnibus"))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
            }

            if (!entryTitle.Contains("Vol"))
            {
                Match volMatch = MasterScrape.FindVolNumRegex().Match(entryTitle);
                if (volMatch.Success)
                {
                    curTitle.Insert(volMatch.Index, "Vol ");
                };
            }
            curTitle.TrimEnd();
            // LOGGER.Debug("(1) {}", curTitle.ToString());

            if (!char.IsDigit(curTitle.ToString()[^1]))
            {
                curTitle.Replace("Omnibus", string.Empty);
                curTitle.TrimEnd();
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Omnibus ");
            }
            // LOGGER.Debug("(2) {}", curTitle.ToString());
        }
        else if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            // entryTitle = entryTitle.Replace("Part", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            if (BoxSetVolFindRegex().IsMatch(entryTitle))
            {
                curTitle = new StringBuilder(BoxSetVolFindRegex().Replace(BoxSetFixRegex().Replace(entryTitle, " Box Set $1"), string.Empty));
                if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || entryTitle.IndexOf("Vol") < entryTitle.IndexOf("Box Set"))
                {
                    curTitle.Append($" {BoxSetVolFindRegex().Match(entryTitle).Groups[1].Value}");
                }
            }
            else
            {
                curTitle = new StringBuilder(BoxSetFixRegex().Replace(entryTitle, " Box Set $2"));
                if (!char.IsDigit(entryTitle[^1]))
                {
                    curTitle.Replace("Box Set", string.Empty);
                    curTitle.TrimEnd();
                    if (!char.IsDigit(curTitle.ToString()[^1])) curTitle.Append(" 1");
                    
                    if (entryTitle.Contains("Part") && !bookTitle.Contains("Part"))
                    {
                        curTitle.Insert(curTitle.Length, " Box Set");
                    }
                    else curTitle.Insert(curTitle.Length - 1, " Box Set ");
                }
            }
            curTitle.Replace("Vol", string.Empty);
            if (!bookTitle.Contains("One", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("One", "1");
            if (!bookTitle.Contains("Two", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Two", "2");
            if (!bookTitle.Contains("Three", StringComparison.OrdinalIgnoreCase)) curTitle.Replace("Three", "3");

            if (InternalHelpers.RemoveNonWordsRegex().Replace(bookTitle, string.Empty).Contains("Attackontitan", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("The Final Season") && entryTitle.Contains("Final Season"))
            {   
                curTitle.Insert("Attack On Titan".Length, " The");
            }
        }
        else
        {
            curTitle = new StringBuilder(entryTitle);
        }

        if (entryTitle.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
        {
            entryTitle = DeluxeEditionRegex().Replace(entryTitle, "Deluxe Edition");
            ReadOnlySpan<char> entryTitleSpan = entryTitle.AsSpan();
            if (!entryTitleSpan.Contains("Deluxe Edition Vol", StringComparison.OrdinalIgnoreCase))
            {
                int index = entryTitleSpan.IndexOf("Vol");
                if (index != -1)
                {
                    curTitle.Insert(index, "Deluxe Edition ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Deluxe Edition");
                }
            }
        }

        curTitle.Replace(",", string.Empty);
        if (!bookTitle.Contains(':') && !entryTitle.ContainsAny(["Year", "Oh", "Edition", "Boruto"]))
        {
            entryTitle = MasterScrape.MultipleWhiteSpaceRegex().Replace(CleanAndParseTitleRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
        }
        else
        {
            entryTitle = MasterScrape.MultipleWhiteSpaceRegex().Replace(ColorCleanAndParseTitleRegex().Replace(RemoveAfterVolNumRegex().Replace(curTitle.ToString(), string.Empty), string.Empty), " ").Trim();
        }
        curTitle = new StringBuilder(entryTitle);
        curTitle.Replace(":", string.Empty);

        if (bookType == BookType.LightNovel)
        {
            curTitle.Replace(" (Light Novel)", string.Empty).Replace(" (Light Novel Hardcover)", string.Empty);
            string snapshot = curTitle.ToString();
            LOGGER.Debug("Snapshot = {}", snapshot);
            if (!snapshot.Contains("Novel"))
            {
                int index = snapshot.AsSpan().IndexOf("Vol");
                if (index != -1)
                {
                    curTitle.Insert(index, "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }
        }
        else
        {
            if (bookTitle.Contains("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(":", string.Empty);
                curTitle.Replace("Naruto Next Generations", string.Empty);
            }
        }

        entryTitle = curTitle.ToString();
        if (entryTitle.Contains("Anniversary") && entryTitle.Contains("Vol"))
        {
            curTitle.Insert(entryTitle.AsSpan().IndexOf("Vol"), $"{AnniversaryMatchRegex().Match(entryTitle).Groups[1].Value} ");
        }
        else if (entryTitle.Contains("Special") && entryTitle.Contains("Edition"))
        {
            curTitle.Insert(entryTitle.AsSpan().IndexOf("Vol"), "Special Edition ");
        }
        // LOGGER.Debug("(3) {}", curTitle.ToString());

        if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Omnibus") && !entryTitle.Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
        {
            curTitle.Insert(curTitle.ToString().Trim().AsSpan().IndexOf("Vol"), "Stray God ");
        }

        if (!entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            Match volMatch = MasterScrape.FindVolNumRegex().Match(entryTitle);
            if (volMatch.Success)
            {
                curTitle.Insert(volMatch.Index, "Vol ");
            };
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    private static async Task BypassCookiesAsync(IPage page)
    {
        try
        {
            // Wait for up to 5 seconds for the button to become visible
            await page.WaitForSelectorAsync("button.button--brand.button--lg.brad.mql.mt", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // Now that we know it's visible, get a locator and click it
            await page.ClickAsync("button.button--brand.button--lg.brad.mql.mt");
        }
        catch (PlaywrightException) { }
    }

    private static async Task LoadAllEntries(IPage page)
    {
        ILocator? loadAllButton = page.Locator("button.load-all.button--brand.brad--sm");
        while (await loadAllButton.IsVisibleAsync() && await loadAllButton.IsEnabledAsync())
        {
            LOGGER.Info("Loading all entries...");
            await loadAllButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.ScrollToBottomUntilStableAsync(
                "//li[@class='support-links product-list__list__separator owl-off bg-white phl phr pht pb brdr--top brdr--top--thin brdr--top--dotted']", // <- something that appears for each item
                maxScrolls: 60,
                stabilityMs: 900,
                stepPx: 1400
            );
        }

        int clickCount = 0;
        ILocator? loadMoreButton = page.Locator("button.load-more.button--brand.brad--sm");
        ILocator? separatorLiLocator = page.Locator("//li[@class='support-links product-list__list__separator owl-off bg-white phl phr pht pb brdr--top brdr--top--thin brdr--top--dotted']");
        while (await loadMoreButton.IsVisibleAsync() && await loadMoreButton.IsEnabledAsync())
        {
            LOGGER.Info("Loading more entries...");
            await loadMoreButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            clickCount++;
            await page.ScrollToBottomUntilStableAsync(
                "//li[@class='support-links product-list__list__separator owl-off bg-white phl phr pht pb brdr--top brdr--top--thin brdr--top--dotted']", // <- something that appears for each item
                maxScrolls: 60,
                stabilityMs: 900,
                stepPx: 1400
            );

            int currentLiCount = await separatorLiLocator.CountAsync();
            int expectedLiCount = 1 + clickCount;

            if (currentLiCount != expectedLiCount) break;
        }
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            HtmlDocument doc = HtmlFactory.CreateDocument();
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();
            HtmlWeb html = HtmlFactory.CreateWeb();
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            bool isSecondCategory = false;

            string url = GenerateWebsiteUrl(bookType, bookTitle, isSecondCategory);
            links.Add(url);

            await page!.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await BypassCookiesAsync(page);
            await LoadAllEntries(page);
            await page.WaitForSelectorAsync("div.full");

        Restart:
            doc.LoadHtml(await page.ContentAsync());
            XPathNodeIterator titleData = nav.Select(_titleXPath);
            XPathNodeIterator priceData = nav.Select(_priceXPath);
            XPathNodeIterator minorPriceData = nav.Select(_minorPriceXPath);
            XPathNodeIterator bookFormatData = nav.Select(_bookFormatXPath);
            XPathNodeIterator stockStatusData = nav.Select(_stockStatusXPath);
            XPathNavigator? pageCheck = nav.SelectSingleNode(_pageCheckXPath);
            LOGGER.Debug("{} | {} | {} | {} | {}", titleData.Count, priceData.Count, minorPriceData.Count, bookFormatData.Count, stockStatusData.Count);

            while (titleData.MoveNext())
            {
                priceData.MoveNext();
                minorPriceData.MoveNext();
                bookFormatData.MoveNext();
                stockStatusData.MoveNext();

                string? bookFormat = bookFormatData.Current?.Value;
                string? titleVal = titleData.Current?.Value;
                string? priceDataVal = priceData.Current?.Value;
                string? stockStatusDataVal = stockStatusData.Current?.Value;
                string? minorPriceDataVal = minorPriceData.Current?.Value;
                if (titleVal is null || bookFormat is null || priceDataVal is null || stockStatusDataVal is null || minorPriceDataVal is null)
                {
                    LOGGER.Debug("Some input value is null for {Title} Skipping", titleVal);
                    continue;
                }

                string entryTitle = WebUtility.HtmlDecode(titleVal);
                // LOGGER.Debug("(-1) {} | {} | {}", entryTitle, bookFormat, minorPriceData.Current?.Value);

                if (
                    InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                    && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                    && (
                            (
                            bookType == BookType.Manga
                            && (
                                    !entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                    && (bookFormat.Equals("Manga") || bookFormat.Equals("Graphic Novel"))
                                    && !(
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony", "Berserker", "Operation")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto", "Itachi", "Family Day", "Naruto: Shikamaru's Story", "Naruto: Kakashi's Story")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "fullmetal alchemist", entryTitle, "Under The Faraway Sky")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "FLY")
                                    )
                                )
                            )
                            || bookType == BookType.LightNovel
                        )
                    && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "Unimplemented")
                    )
                {
                    bool descIsValid = true;
                    if (bookType == BookType.Manga && (entryTitle.Contains("Hardcover") || !entryTitle.ContainsAny(["Vol", "Box Set", "Comic"])))
                    {
                        string? urlPath = doc.DocumentNode.SelectSingleNode($"(//a[@class='block one-whole clearfix dfbx dfbx--fdc link-banner link--black'])[{titleData.CurrentPosition}]")?.GetAttributeValue("href", string.Empty);
                        if (string.IsNullOrWhiteSpace(urlPath))
                        {
                            LOGGER.Debug("Unable to retrieve url path for entry desc at pos {Pos}", titleData.CurrentPosition);
                            continue;
                        }

                        HtmlNodeCollection descData = (await html.LoadFromWebAsync($"https://forbiddenplanet.com{urlPath}")).DocumentNode.SelectNodes("//div[@id='product-description']/p");

                        StringBuilder desc = new();
                        foreach (HtmlNode node in descData) { desc.AppendLine(node.InnerText); }
                        LOGGER.Debug("Checking Desc {} => {}", entryTitle, desc.ToString());
                        descIsValid = !desc.ToString().ContainsAny(DescRemovalStrings);
                    }

                    if (descIsValid)
                    {
                        string finalTitle = CleanAndParseTitle(bookTitle, entryTitle, bookType);
                        LOGGER.Debug("Final Title = {}", finalTitle);
                        data.Add(
                            new EntryModel(
                                finalTitle,
                                $"{priceDataVal}{minorPriceDataVal}",
                                stockStatusDataVal switch
                                {
                                    "Pre-Order" => StockStatus.PO,
                                    "Currently Unavailable" => StockStatus.OOS,
                                    _ => StockStatus.IS
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

            if (!isSecondCategory)
            {
                isSecondCategory = true;
                LOGGER.Info("Checking Comics & Graphic Novel Cateogry");
                url = GenerateWebsiteUrl(bookType, bookTitle, isSecondCategory);
                links.Add(url);

                await page!.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });
                await LoadAllEntries(page);
                goto Restart;
            }

            data.TrimExcess();
            data.Sort(EntryModel.VolumeSort);
            data.RemoveDuplicates(LOGGER);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        catch (Exception ex)
        {
            LOGGER.Error(ex, "{Title} ({BookType}) Error @ {TITLE}", bookTitle, bookType, TITLE);
        }

        return (data, links);
    }
}