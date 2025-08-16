
using System.Collections.Frozen;
using System.Threading;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class MerryManga : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|Omnibus( \d{1,2})(?:, |\s{1})Vol \d{1,3}-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex FixOmnibusRegex();
    [GeneratedRegex(@"(?<=Box Set \d{1}).*", RegexOptions.IgnoreCase)] private static partial Regex FixBoxSetRegex();
    [GeneratedRegex(@" \(.*\)|,")] private static partial Regex FixTitleRegex();
    [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

    /// <inheritdoc />
    public const string TITLE = "MerryManga";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.merrymanga.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    private static readonly FrozenSet<string> _stockClasses = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "instock",
        "outofstock",
        "onbackorder",
        "preorder",
        "available_at_warehouse"
    );

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            IPage page = await PlaywrightFactory.GetPageAsync(browser!);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.MerryManga, Links[0]);
        });
    }

    // https://www.merrymanga.com/?s=Naruto&post_type=product&_categories=box-sets
    // https://www.merrymanga.com/?s=jujutsu+kaisen&post_type=product&orderby=release_date&_categories=manga
    private static string GenerateWebsiteUrl(string bookTitle, BookType bookType, bool hasBoxSet)
    {
        string url;
        if (hasBoxSet && bookType != BookType.LightNovel)
        {
            url = $"{BASE_URL}/?s={bookTitle.Replace(" ", "+")}&post_type=product&orderby=release_date&_categories=box-sets";
        }
        else
        {
            url = $"{BASE_URL}/?s={bookTitle.Replace(" ", "+")}&post_type=product&orderby=release_date&_categories={(bookType == BookType.Manga ? "manga" : "light-novels")}";
        }
        LOGGER.Info(url);
        return url;
    }

    private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        string s = entryTitle;

        if (FixOmnibusRegex().IsMatch(s))
        {
            s = FixOmnibusRegex().Replace(s, "Omnibus$1");
            if (!s.Contains("Vol", StringComparison.Ordinal))
            {
                int pos = s.IndexOf("Omnibus", StringComparison.Ordinal) + "Omnibus".Length;
                s = s.Insert(pos, " Vol");
            }
        }
        else if (FixBoxSetRegex().IsMatch(s))
        {
            s = FixBoxSetRegex().Replace(s, string.Empty);
        }

        s = FixTitleRegex().Replace(s, string.Empty);

        var sb = new StringBuilder(s);

        if (bookType == BookType.LightNovel && !s.Contains("Novel", StringComparison.Ordinal))
        {
            int idx = s.IndexOf("Vol", StringComparison.Ordinal);
            sb.Insert(idx >= 0 ? idx : sb.Length, " Novel ");
        }
        else if (bookType == BookType.Manga && 
                bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
        {
            sb.Replace("Naruto Next Generations", string.Empty);
        }

        InternalHelpers.RemoveCharacterFromTitle(ref sb, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref sb, bookTitle, "–", " ");

        string collapsed = MasterScrape
            .MultipleWhiteSpaceRegex()
            .Replace(sb.ToString().Trim(), " ");

        return collapsed;
    }

    private static void ExtractProductData(
        HtmlDocument doc,
        out List<string> titles,
        out List<string> prices,
        out List<StockStatus> statuses)
    {
        titles = [];
        prices = [];
        statuses = [];

        foreach (HtmlNode node in doc.DocumentNode.Descendants())
        {
            string nodeName = node.Name;

            // 1) TITLE: <h2 class="woocommerce-loop-product__title">
            if (nodeName == "h2")
            {
                // //h2[@class='woocommerce-loop-product__title']
                string classAttr = node.GetAttributeValue("class", string.Empty);
                if (classAttr.Equals("woocommerce-loop-product__title", StringComparison.OrdinalIgnoreCase))
                {
                    string text = node.InnerText.Trim();
                    titles.Add(text);
                    LOGGER.Debug(text);
                }
            }

            // 2) PRICE: match either
            //    a) <span class="price"> → <ins> → <span class="woocommerce-Price-amount amount"> → <bdi>
            // or b) <span class="price"> → <span class="woocommerce-Price-amount amount"> → <bdi>
            if (nodeName == "bdi")
            {
            HtmlNode parentNode = node.ParentNode;
            if (parentNode is not null
                && parentNode.Name == "span"
                && parentNode.GetAttributeValue("class", string.Empty)
                            .Contains("woocommerce-Price-amount amount", StringComparison.OrdinalIgnoreCase))
                    {
                        HtmlNode grandParent = parentNode.ParentNode;
                        if (grandParent is not null)
                        {
                            // case (a): under <ins>
                            if (grandParent.Name == "ins")
                            {
                                HtmlNode greatGrand = grandParent.ParentNode;
                                if (greatGrand is not null
                                    && greatGrand.Name == "span"
                                    && greatGrand.GetAttributeValue("class", string.Empty)
                                                .Equals("price", StringComparison.OrdinalIgnoreCase))
                                {
                                    string text = node.InnerText.Trim();
                                    prices.Add(text);
                                }
                            }
                            // case (b): directly under <span class="price">
                            else if (grandParent.Name == "span"
                                    && grandParent.GetAttributeValue("class", string.Empty)
                                                    .Equals("price", StringComparison.OrdinalIgnoreCase))
                            {
                                string text = node.InnerText.Trim();
                                prices.Add(text);
                            }
                        }
                    }
                    continue;
            }

            // 3) STOCK: //li[contains(@class, 'instock')] | //li[contains(@class, 'outofstock')] | //li[contains(@class, 'onbackorder')] | //li[contains(@class, 'preorder')] | //li[contains(@class, 'available_at_warehouse')]
            if (nodeName == "li")
            {
                string classAttr = node.GetAttributeValue("class", string.Empty);
                string[] parts = classAttr.Split(' ');
                foreach (string part in parts)
                {
                    if (_stockClasses.Contains(part))
                    {
                        StockStatus stockStatus = part switch
                        {
                            "instock" or "available_at_warehouse" => StockStatus.IS,
                            "outofstock" => StockStatus.OOS,
                            "preorder" => StockStatus.PO,
                            "onbackorder" => StockStatus.BO,
                            _ or "Unknown" => StockStatus.NA,
                        };

                        statuses.Add(stockStatus);
                        break;
                    }
                }
            }
        }
    }

    private static async Task CheckAndProceedIfRated18Async(IPage page)
    {
        ILocator heading = page.Locator("h2.popup_heading");

        // Wait a short moment to see if it appears (optional)
        if (await heading.CountAsync() > 0)
        {
            string text = (await heading.First.InnerTextAsync()).Trim();

            if (text.Equals("This product is rated 18+", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator("button.btn_submit#submit").ClickAsync();
                LOGGER.Info("Proceeded from 18+ popup");
            }
        }
    }

    // TODO: Needs perf improvments
    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            bool hasBoxSet = true;
        Restart:
            string url = GenerateWebsiteUrl(bookTitle.ToLower(), bookType, hasBoxSet);
            links.Add(url);

            await page!.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await page.WaitForSelectorAsync("div.container.main-content");

            HtmlDocument doc = HtmlFactory.CreateDocument();
            doc.LoadHtml(await page.ContentAsync());

            if (hasBoxSet && doc.Text.Contains("No products were found matching your selection."))
            {
                LOGGER.Info("No box set entries found, Checking Manga Only Link");
                hasBoxSet = false;
                url = GenerateWebsiteUrl(bookTitle.ToLower(), bookType, hasBoxSet);
                links.Clear();
                links.Add(url);

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });
                await page.WaitForSelectorAsync("div.container.main-content");
            }

            await CheckAndProceedIfRated18Async(page);

            // Load all data
            if (await page.Locator("button.facetwp-load-more:not(.facetwp-hidden)").CountAsync() > 0)
            {
                ILocator visibleBtn = page.Locator("button.facetwp-load-more:not(.facetwp-hidden)");
                ILocator hiddenBtn = page.Locator("button.facetwp-load-more.facetwp-hidden");
                ILocator pager = page.Locator("div.facetwp-facet.facetwp-facet-load_more.facetwp-type-pager");
                ILocator noProducts = page.Locator("div.woocommerce-info"); // <-- "No products were found..." banner

                // While the *visible* load-more button exists:
                while (true)
                {
                    if (await noProducts.CountAsync() > 0)
                    {
                        string text = (await noProducts.First.InnerTextAsync()).Trim();
                        if (text.Equals("No products were found matching your selection.", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }

                    LOGGER.Info("Loading more entries...");
                    // no visible button -> done
                    if (await visibleBtn.CountAsync() == 0) break;

                    // click the visible button (use Force if header still intercepts)
                    await visibleBtn.First.ClickAsync();

                    // wait until the pager is attached (your requirement)
                    await pager.First.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = 5000
                    });

                    // after the click, wait for EITHER: hidden button appears OR visible button detaches
                    Task tHidden = hiddenBtn.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 4000 });
                    Task tGone = visibleBtn.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 4000 });
                    await Task.WhenAny(tHidden, tGone);

                    // small settle so class flip/DOM swap can complete
                    await page.WaitForTimeoutAsync(50);

                    // break conditions
                    if (await hiddenBtn.CountAsync() > 0 || await visibleBtn.CountAsync() == 0)
                    {
                        break;
                    }
                }

                LOGGER.Info("Finished loading more entries");
                string html = await page.ContentAsync();
                doc.LoadHtml(html);
            }

            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);

            ExtractProductData(doc, out List<string> titleData, out List<string> priceData, out List<StockStatus> stockStatusData);

            for (int x = 0; x < titleData.Count; x++)
            {
                string entryTitle = titleData[x];
                if (
                    InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                    && (!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                    && !(
                            (
                                bookType == BookType.Manga
                                && (
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                    )
                            )
                        ||
                            (
                                bookType == BookType.LightNovel
                                && (
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented")
                                    )
                            )
                        )
                    )
                {
                    data.Add(
                        new EntryModel
                        (
                            ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol").Trim(), bookTitle, bookType),
                            priceData[x],
                            stockStatusData[x],
                            TITLE
                        )
                    );
                }
                else
                {
                    LOGGER.Debug("Removed {}", entryTitle);
                }
                LOGGER.Debug("CHECK 1");
            }

            if (hasBoxSet)
            {
                hasBoxSet = false;
                goto Restart;
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
        }

        return (data, links);
    }
}