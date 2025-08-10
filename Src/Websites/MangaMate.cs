using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class MangaMate : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='grid-product__title grid-product__title--body']");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//div[@class='grid-product__price']/text()[3]");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='grid-product__content']/div[1]");
    private static readonly XPathExpression StockStatusXPath2 = XPathExpression.Compile("//div[@class='grid-product__image-mask']/div[1]");
    private static readonly XPathExpression EntryLinkXPath = XPathExpression.Compile("//div[@class='grid__item-image-wrapper']/a");
    private static readonly XPathExpression EntryTypeXPath = XPathExpression.Compile("//div[@class='product-block'][4]/div/span/table//tr[4]/td[2]");

    [GeneratedRegex(@"The Manga|\(.*\)|Manga", RegexOptions.IgnoreCase)] private static partial Regex TitleParseRegex();
    [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

    /// <inheritdoc />
    public const string TITLE = "MangaMate";

    /// <inheritdoc />
    public const string BASE_URL = "https://mangamate.shop";

    /// <inheritdoc />
    public const Region REGION = Region.Australia;

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            IPage page = await PlaywrightFactory.GetPageAsync(browser!);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.MangaMate, Links[0]);
        });
    }

    private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum)
    {
        // https://mangamate.shop/search?q=akane%20banashi&options%5Bprefix%5D=last
        string url = $"{BASE_URL}/search?options%5Bprefix%5D=last&page={pageNum}&q={InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"))}+{(bookType == BookType.Manga ? "manga" : "novel")}";
        LOGGER.Info("Page {} => {}", pageNum, url);
        return url;
    }

    private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (OmnibusRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
        }
        else
        {
            entryTitle = TitleParseRegex().Replace(entryTitle, string.Empty);
        }
        StringBuilder curTitle = new StringBuilder(entryTitle).Replace(",", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, ":", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Color Edition", "In Color");
        if (bookTitle.Equals("boruto", StringComparison.OrdinalIgnoreCase)) { curTitle.Replace(" Naruto Next Generations", string.Empty); }

        Match findVolNumMatch = MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim());
        if (bookType == BookType.Manga && !entryTitle.Contains("Box Set") && !entryTitle.Contains("Vol") && !string.IsNullOrWhiteSpace(findVolNumMatch.Groups[0].Value))
        {
            curTitle.Insert(findVolNumMatch.Index, "Vol ").TrimEnd();
        }
        else if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Stray Stories") && string.IsNullOrWhiteSpace(findVolNumMatch.Groups[0].Value))
        {
            curTitle.Insert(curTitle.Length, " Vol 1");
        }

        string volNum = findVolNumMatch.Groups[0].Value;
        if (volNum.Length > 1 && volNum.StartsWith('0'))
        {
            curTitle.Replace(volNum, volNum.TrimStart('0'));
        }
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    private static async Task WaitForProductPageLoad(IPage page)
    {
        // Wait for the product grid you were waiting on in Selenium
        ILocator grid = page.Locator("(//div[@class='grid grid--uniform'])[2]");
        await grid.WaitForAsync();
    }

    private static async Task<(string Html, uint MaxPageNum)> GetInitialData(IPage page, string url)
    {
        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForProductPageLoad(page);

        // Get the max page number (//span[@class='page'][last()])
        uint maxPageNum = 1;
        ILocator pages = page.Locator("//span[@class='page']").Last;
        int count = await pages.CountAsync();
        if (count > 0)
        {
            string? lastText = await pages.Nth(count - 1).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(lastText) && uint.TryParse(lastText.Trim(), out uint parsed))
            {
                maxPageNum = parsed;
            }
        }
        LOGGER.Info("Max Page Num = {Num}", maxPageNum);

        // Open currency dropdown: //button[@aria-controls='CurrencyList-toolbar']
        ILocator currencyBtn = page.Locator("//button[@aria-controls='CurrencyList-toolbar']");
        await currencyBtn.ClickAsync();

        // Ensure it's open (aria-expanded == "true")
        await page
            .Locator("button[aria-controls='CurrencyList-toolbar'][aria-expanded='true']")
            .WaitForAsync();

        await page.Locator("button[aria-controls='CurrencyList-toolbar'][aria-expanded='true']").WaitForAsync();

        // Find the controlled menu *by id* from aria-controls, then click AUD inside it
        string? menuId = await currencyBtn.GetAttributeAsync("aria-controls");   // e.g., "CurrencyList-toolbar"
        ILocator menu = page.Locator($"#{menuId}");
        await menu.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        // Now select the AUD option *within that menu only*
        await menu.Locator("a.disclosure-list__option[data-value='AU']").First.ClickAsync();


        // Wait for the page to settle and grid to be present again
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        // await WaitForProductPageLoad(page);

        LOGGER.Info("Clicked AUD Currency");
        return (await page.ContentAsync(), maxPageNum);
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            HtmlWeb html = HtmlFactory.CreateWeb();
            HtmlDocument doc = HtmlFactory.CreateDocument();
            XPathNavigator nav = doc.DocumentNode.CreateNavigator();

            ushort curPageNum = 1;
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            string url = GenerateWebsiteUrl(bookTitle, bookType, curPageNum);
            links.Add(url);

            (string Html, uint MaxPageNum) = await GetInitialData(page!, url);
            doc.LoadHtml(Html);

            while (true)
            {
                XPathNodeIterator titleData = nav.Select(TitleXPath);
                XPathNodeIterator priceData = nav.Select(PriceXPath);
                XPathNodeIterator stockstatusData = nav.Select(StockStatusXPath);
                XPathNodeIterator stockstatusData2 = nav.Select(StockStatusXPath2);
                XPathNodeIterator entryLinkData = nav.Select(EntryLinkXPath);

                while (titleData.MoveNext())
                {
                    priceData.MoveNext();
                    stockstatusData.MoveNext();
                    stockstatusData2.MoveNext();
                    entryLinkData.MoveNext();

                    string entryTitle = titleData.Current!.Value.Trim();
                    if (InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && !(
                            bookType == BookType.Manga
                            && (
                                entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                || (
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Story")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear")
                                )
                            )
                        )
                    )
                    {
                        string? type = (await html.LoadFromWebAsync($"https://mangamate.shop{entryLinkData.Current!.GetAttribute("href", string.Empty)}")).DocumentNode.CreateNavigator().SelectSingleNode(EntryTypeXPath)?.Value;
                        if (type is null)
                        {
                            continue;
                        }
                        type = type.Trim();
                        // LOGGER.Debug("{} | {}", entryTitle, type);

                        if ((bookType == BookType.Manga && (type.Equals("Manga", StringComparison.OrdinalIgnoreCase) || type.Equals("Box Set", StringComparison.OrdinalIgnoreCase))) || (bookType == BookType.LightNovel && (type.Equals("Novel", StringComparison.OrdinalIgnoreCase) || type.Equals("Box Set", StringComparison.OrdinalIgnoreCase))))
                        {
                            data.Add(
                                new EntryModel
                                (
                                    ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                    priceData.Current!.Value.Trim(),
                                    stockstatusData2.Current!.GetAttribute("class", "error").Contains("preorder") ? StockStatus.PO : stockstatusData.Current!.Value.Trim() switch
                                    {
                                        "Sold Out" => StockStatus.OOS,
                                        _ => StockStatus.IS
                                    },
                                    TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                        }
                    }
                    else
                    {
                        LOGGER.Info("Removed (1) {}", entryTitle);
                    }
                }

                if (curPageNum < MaxPageNum)
                {
                    url = GenerateWebsiteUrl(bookTitle, bookType, ++curPageNum);
                    links.Add(url);
                    await page!.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });
                    await WaitForProductPageLoad(page);
                    doc.LoadHtml(await page.ContentAsync());
                }
                else
                {
                    break;
                }
            }

            data.TrimExcess();
            links.TrimExcess();
            data = InternalHelpers.RemoveDuplicateEntries(data);
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