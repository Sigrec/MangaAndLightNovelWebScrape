using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

public partial class Waterstones : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    /// <inheritdoc />
    public const string TITLE = "Waterstones";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.waterstones.com";

    /// <inheritdoc />
    public const Region REGION = Region.Britain;

    private static readonly XPathExpression _titleXPath = XPathExpression.Compile("//a[contains(@class, 'title link-invert')]");
    private static readonly XPathExpression _oneShotTitleXPath = XPathExpression.Compile("//span[@id='scope_book_title']");
    private static readonly XPathExpression _priceXPath = XPathExpression.Compile("//span[@class='price']");
    private static readonly XPathExpression _oneShotPriceXPath = XPathExpression.Compile("//b[@itemprop='price']");
    private static readonly XPathExpression _stockStatusXPath = XPathExpression.Compile("//div[@class='book-price']/span[1]");
    private static readonly XPathExpression _oneShotStockStatusXPath = XPathExpression.Compile("//span[@id='scope_offer_availability']");
    private static readonly XPathExpression _pageCheckXPath = XPathExpression.Compile("//div[@class='pager']/span[2]");
    private static readonly XPathExpression _oneShotCheckXPath = XPathExpression.Compile("//div[@class='span12']/h2");

    [GeneratedRegex(@",|The Manga|(?<=Vol \d{1,3})[^\d{1,3}.].*|(?<=Vol \d{1,3}.\d{1})[^\d{1,3}.]+.*|(?<=Special Edition).*|(?<=Omnibus \d{1,3})[^\d{1,3}].*| \(Paperback\)| - .*", RegexOptions.IgnoreCase)] private static partial Regex FixTitleRegex();
    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex FixOmnibusRegex();
    [GeneratedRegex(@"(?<=Box Set \d{1}).*", RegexOptions.IgnoreCase)] private static partial Regex FixBoxSetRegex();
    [GeneratedRegex(@"\(.*\)", RegexOptions.IgnoreCase)] private static partial Regex FixTitleTwoRegex();
    [GeneratedRegex(@"(?:Vol \d{1,3})$", RegexOptions.IgnoreCase)] private static partial Regex FullTitleCheckRegex();
    [GeneratedRegex(@"Vol\.|Volume|v\.", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            IPage page = await PlaywrightFactory.GetPageAsync(browser!, true);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.MerryManga, Links[0]);
        });
    }

    private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum, bool isOneShot)
    {
        // https://www.waterstones.com/books/search/term/overlord+novel
        string url;
        if (!isOneShot)
        {
            // https://www.waterstones.com/books/search/term/jujutsu+kaisen/category/394/facet/347/sort/pub-date-asc/page/1
            url = $"{(bookType == BookType.Manga ? $"{BASE_URL}/books/search/term/{bookTitle}/category/394/facet/347/sort/pub-date-asc/page1//page/{pageNum}" : $"https://www.waterstones.com/books/search/term/{bookTitle}+novel")}";
            LOGGER.Info($"Url Page {pageNum} = {url}");
        }
        else
        {
            // https://www.waterstones.com/books/search/term/Goodbye+Eri
            url = $"{BASE_URL}/books/search/term/{bookTitle}";
            LOGGER.Info($"OneShot Url = {url}");
        }
        return url;
    }

    private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        StringBuilder curTitle;
        if (FixOmnibusRegex().IsMatch(entryTitle))
        {
            curTitle = new StringBuilder(FixOmnibusRegex().Replace(entryTitle, "Omnibus"));
        }
        else if (FixBoxSetRegex().IsMatch(entryTitle))
        {
            curTitle = new StringBuilder(FixBoxSetRegex().Replace(entryTitle, ""));
        }
        else
        {
            curTitle = new StringBuilder(entryTitle);
        }
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "—", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "–", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Manga", string.Empty);

        if (entryTitle.Contains(" Special Edition", StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace(" Special Edition", string.Empty);
            curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, "Special Edition ");
        }

        if (curTitle.ToString().Contains('('))
        {
            curTitle = new StringBuilder(FixTitleTwoRegex().Replace(curTitle.ToString(), string.Empty));
        }

        if (!entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && MasterScrape.FindVolNumRegex().IsMatch(curTitle.ToString().Trim()))
        {
            curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, "Vol ");
        }

        if (bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel"))
        {
            if (entryTitle.Contains("Vol"))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Novel ");
            }
            else
            {
                curTitle.Insert(curTitle.Length, " Novel");
            }
        }
        else if (bookType == BookType.Manga)
        {
            if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace("Naruto Next Generations", string.Empty);
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
            ushort pageNum = 1, maxPageNum = 1;
            bool isOneShot = false;

            string url = GenerateWebsiteUrl(bookTitle.Replace(",", string.Empty).Replace(" ", "+"), bookType, pageNum, isOneShot);
            links.Add(url);
            await page!.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            HtmlDocument doc = HtmlFactory.CreateDocument();
            doc.LoadHtml(await page.ContentAsync());

            HtmlNode oneShotCheckNode = doc.DocumentNode.SelectSingleNode(_oneShotCheckXPath);
            if (oneShotCheckNode != null && oneShotCheckNode.InnerText.Contains("No results"))
            {
                isOneShot = true;
                links.Clear();

                url = GenerateWebsiteUrl(bookTitle.Replace(",", string.Empty).Replace(" " , "+"), bookType, pageNum, isOneShot);
                links.Add(url);
                await page!.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });
                doc.LoadHtml(await page.ContentAsync());
            }
            else
            {
                // Get the total number of pages
                HtmlNode pageCheckNode = doc.DocumentNode.SelectSingleNode(_pageCheckXPath);
                maxPageNum = pageCheckNode != null ? (ushort)char.GetNumericValue(pageCheckNode.InnerText.TrimEnd()[^1]) : (ushort)1;
                LOGGER.Debug("Entry has {} Page(s)", maxPageNum);
            }

            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            while (true)
            {
                // Get page data
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(!isOneShot ? _titleXPath : _oneShotTitleXPath);
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(!isOneShot ? _priceXPath : _oneShotPriceXPath);
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(!isOneShot ? _stockStatusXPath : _oneShotStockStatusXPath);
                LOGGER.Debug("{} | {} | {}", titleData?.Count, priceData?.Count, stockStatusData?.Count);

                for (int x = 0; x < titleData.Count; x++)
                {
                    string entryTitle = FixVolumeRegex().Replace(titleData[x].InnerText, " Vol").Trim();
                    if (!isOneShot && ((entryTitle.EndsWith('…') && !FullTitleCheckRegex().IsMatch(entryTitle[..entryTitle.IndexOf('…')])) || !entryTitle.Contains("Vol"))) // Check to see if title is cutoff
                    {
                        string oldTitle = entryTitle;
                        url = $"{BASE_URL}/{titleData[x].GetAttributeValue("href", "Error")}";
                        LOGGER.Debug("Checking cutoff series {Url}", url);
                        await page.Locator($"a[href='{titleData[x].GetAttributeValue("href", "Error")}']").First.ForceClickAsync();
                        // entryTitle = FixVolumeRegex().Replace(wait.Until(driver => driver.FindElement(By.Id("scope_book_title"))).Text, " Vol");
                        // await page.WaitForSelectorAsync("#scope_book_title", new PageWaitForSelectorOptions
                        // {
                        //     State = WaitForSelectorState.Attached
                        // });
                        IElementHandle? el = await page.QuerySelectorAsync("#scope_book_title"); // immediate snapshot, no wait
                        if (el != null)
                        {
                            string text = (await el.InnerTextAsync()).Trim();
                            entryTitle = FixVolumeRegex().Replace(text, " Vol");
                        }
                        else
                        {
                            LOGGER.Debug("Removed (0) {Title}", entryTitle);
                            continue;
                        }
                    }

                    if (
                        InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                        && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                        && !(
                            bookType == BookType.Manga
                            && (
                                    (
                                        entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                        && !bookTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                    )
                                    ||
                                    (
                                        entryTitle.Contains("NoVoll", StringComparison.OrdinalIgnoreCase)
                                        && !bookTitle.Contains("NoVoll", StringComparison.OrdinalIgnoreCase)
                                    )
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Bleachers")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Kuklo Unbound")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, ["of Gluttony", "Flame Dragon Knight"])
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                )
                            )
                        )
                    {
                        string price = priceData[x].InnerText.Trim();
                        data.Add(
                            new EntryModel
                            (
                                ParseTitle(FixTitleRegex().Replace(entryTitle, string.Empty), bookTitle, bookType),
                                price.Substring(price.IndexOf('£')),
                                stockStatusData[x].InnerText.Trim() switch
                                {
                                    string status when status.Contains("In stock", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                    string status when status.Contains("Pre-order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                    _ => StockStatus.OOS,
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

                if (!isOneShot && pageNum < maxPageNum)
                {
                    pageNum++;
                    // url = GenerateWebsiteUrl(bookTitle.Replace(",", string.Empty).Replace(" ", "+"), bookType, pageNum, isOneShot);
                    // await page!.GotoAsync(url, new PageGotoOptions
                    // {
                    //     WaitUntil = WaitUntilState.DOMContentLoaded
                    // });
                    // driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle.Replace(",", string.Empty).Replace(" ", "+"), bookType, pageNum, isOneShot));
                    await page.Locator("a[title='Go to next page']").ForceClickAsync();
                    links.Add(page.Url);
                    doc.LoadHtml(await page.ContentAsync());
                }
                else
                {
                    break;
                }
            }

            data.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        catch (Exception ex)
        {
            LOGGER.Error("{} ({}) Error @ {} \n{}", bookTitle, bookType, TITLE, ex);
        }

        return (data, links);
    }
}