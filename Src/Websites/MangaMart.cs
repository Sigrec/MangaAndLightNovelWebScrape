using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class MangaMart : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='product-item__title text--strong link']");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price' or @class='price price--highlight']/text()[2]");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[contains(@class, 'product-item product-item--vertical')]");
    private static readonly XPathExpression DeepStockStatusXPath = XPathExpression.Compile(".//span[@class='bss_pl_text_hover_text bss_pl_text_hover_link_disable']/div/strong");
    private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//a[@class='pagination__nav-item link'])[last()]");
    private static readonly XPathExpression EntryTitleDesc = XPathExpression.Compile("//div[@class='rte text--pull']");

    [GeneratedRegex(@"\b(?:Vols?|Volume)\b\.?", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"(?>,|\([^)]*\)|\s*Includes.*|(?<=Omnibus,\s*Vol\s*\d{1,3}\.).*|(?<=Vol\s*\d{1,3}(?!\d)).*|-The Manga|The Manga|Manga)", RegexOptions.IgnoreCase)] private static partial Regex ParseAndCleanTitleRegex();
    [GeneratedRegex(@"(?<=Box\s*Set\s*\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex BoxSetTitleCleanRegex();
    [GeneratedRegex(@"\((?:Omnibus|\d{1,3}-in-\d{1,3}) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusTitleCleanRegex();

    /// <inheritdoc />
    public const string TITLE = "MangaMart";

    /// <inheritdoc />
    public const string BASE_URL = "https://mangamart.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            IPage page = await PlaywrightFactory.GetPageAsync(browser!);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.MangaMart, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(BookType bookType, string bookTitle, uint curPageNum)
    {
        // https://mangamart.com/search?type=product&q=jujutsu+kaisen&page=2
        // https://mangamart.com/search?type=product&q=overlord+novel
        string url = $"{BASE_URL}/search?type=product&q={Uri.EscapeDataString(bookTitle)}{(bookType == BookType.Manga ? string.Empty : "+novel")}&page={curPageNum}";
        LOGGER.Info("URL #{} -> {}", curPageNum, url);
        return url;
    }

    private static string ParseAndCleanTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
        {
            entryTitle = BoxSetTitleCleanRegex().Replace(entryTitle, string.Empty);
        }
        else if (OmnibusTitleCleanRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusTitleCleanRegex().Replace(entryTitle, "Omnibus");
        }

        StringBuilder curTitle = new(ParseAndCleanTitleRegex().Replace(entryTitle, string.Empty));

        if (bookType == BookType.LightNovel)
        {
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "(Light Novel)", string.Empty);
        }
        else
        {
            if (bookTitle.Contains("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(" Naruto Next Generations", string.Empty);
            }
        }

        if (bookTitle.Contains(':'))
        {
            InternalHelpers.RemoveAfterLastIfMultiple(ref curTitle, ':');
        }
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        
        curTitle.TrimEnd().AddVolToString();
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    private static async Task WaitForStableLastItemAsync(
        IPage page,
        string selector = "price.price--highlight",
        int timeoutMs = 30000,
        int pollMs = 250)
    {
        ILocator items = page.Locator(selector);

        int previousCount = -1;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            int count = await items.CountAsync();

            // no items yet → keep waiting
            if (count == 0)
            {
                previousCount = 0;
                await Task.Delay(pollMs);
                continue;
            }

            // if the page is still appending more, update previousCount and keep waiting
            if (count != previousCount)
            {
                previousCount = count;
                await Task.Delay(pollMs);
                continue;
            }

            // finally, once the count is stable, return when the last is visible
            ILocator last = items.Nth(count - 1);
            if (await last.IsVisibleAsync())
                return;

            // not visible yet → keep polling
            await Task.Delay(pollMs);
        }

        throw new TimeoutException($"WaitForStableLastItemAsync timed out after {timeoutMs} ms for selector: {selector}");
    }

    private static async Task WaitForStablePageLoadAsync(IPage page)
    {
        try
        {
            await page.WaitForSelectorAsync(
                "span.bss_pl_text_hover_text div strong",
                new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 5_000
                }
            );

            await page.WaitForSelectorAsync(
                "span.price.price--highlight",
                new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 5_000
                }
            );
        }
        catch (TimeoutException) { }
        finally
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.ScrollToBottomUntilStableAsync(
                "span.price.price--highlight",
                maxScrolls: 60,
                stabilityMs: 900,
                stepPx: 1400
            );
        }
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            HtmlWeb html = HtmlFactory.CreateWeb();
            HtmlDocument doc = HtmlFactory.CreateDocument();

            // Load the document once after preparation.
            uint curPageNum = 1;
            string url = GenerateWebsiteUrl(bookType, bookTitle, curPageNum);
            bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);

            links.Add(url);

            await page!.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await WaitForStablePageLoadAsync(page);

            doc.LoadHtml(await page.ContentAsync());

            XPathNavigator nav = doc.DocumentNode.CreateNavigator();
            XPathNavigator? pageNode = nav.SelectSingleNode(PageCheckXPath);
            int maxPageNum = pageNode is not null ? pageNode.ValueAsInt : 1;
            LOGGER.Debug("Max Pages = {}", maxPageNum);

            while (curPageNum <= maxPageNum)
            {
                XPathNodeIterator titleData = nav.Select(TitleXPath);
                XPathNodeIterator priceData = nav.Select(PriceXPath);
                XPathNodeIterator stockStatusData = nav.Select(StockStatusXPath);

                while (titleData.MoveNext())
                {
                    priceData.MoveNext();
                    stockStatusData.MoveNext();
                    string? entryTitle = WebUtility.HtmlDecode(titleData.Current?.Value)?.Trim();
                    if (entryTitle is null)
                    {
                        continue;
                    }

                    bool shouldRemoveEntry = !InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle) || (!bookTitleRemovalCheck && InternalHelpers.ShouldRemoveEntry(entryTitle));

                    if (bookType == BookType.Manga)
                    {
                        shouldRemoveEntry = shouldRemoveEntry ||
                            (!bookTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase)) ||
                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, ["Pirate Recipes"]) ||
                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, ["of Gluttony"]) ||
                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, ["Boruto"]) ||
                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, ["Unimplemented"]);

                        if (!shouldRemoveEntry && !entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase))
                        {
                            LOGGER.Debug("Checking {} for Novel", entryTitle);
                            string urlPath = titleData.Current!.GetAttribute("href", string.Empty);
                            if (!string.IsNullOrWhiteSpace(urlPath))
                            {
                                HtmlNode? descNode = (await html.LoadFromWebAsync($"https://mangamart.com/{urlPath}")).DocumentNode.SelectSingleNode(EntryTitleDesc);
                                string? innerText = descNode?.InnerText;
                                if (descNode is not null && innerText is not null && (innerText.Contains("Light Novel", StringComparison.OrdinalIgnoreCase) || innerText.Contains("novels", StringComparison.OrdinalIgnoreCase)))
                                {
                                    LOGGER.Debug("Found Novel entry in Manga Scrape");
                                    shouldRemoveEntry = true;
                                }
                            }
                        }
                    }
                    else if (bookType == BookType.LightNovel)
                    {
                        shouldRemoveEntry = shouldRemoveEntry || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented");
                    }

                    if (!shouldRemoveEntry)
                    {
                        string stockStatusNode = stockStatusData.Current?.SelectSingleNode(DeepStockStatusXPath)?.Value.Trim() ?? string.Empty;
                        StockStatus stockStatus = stockStatusNode switch
                        {
                            "PRE-ORDER" => StockStatus.PO,
                            "BACK-ORDER" => StockStatus.BO,
                            _ => StockStatus.IS,
                        };
                        LOGGER.Debug("{} | {} | {}", entryTitle, string.IsNullOrWhiteSpace(stockStatusNode), stockStatusNode);

                        data.Add(
                            new EntryModel
                            (
                                ParseAndCleanTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                priceData.Current!.Value,
                                stockStatus,
                                TITLE
                            )
                        );
                    }
                    else
                    {

                        LOGGER.Debug("Removed {}", entryTitle);
                    }
                }

                curPageNum++;
                if (curPageNum <= maxPageNum)
                {
                    url = GenerateWebsiteUrl(bookType, bookTitle, curPageNum);
                    links.Add(url);
                    await page!.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });
                    await WaitForStablePageLoadAsync(page);
                    doc.LoadHtml(await page.ContentAsync());
                }
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