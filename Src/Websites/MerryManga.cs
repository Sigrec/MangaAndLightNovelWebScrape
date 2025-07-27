
using System.Collections.Frozen;

namespace MangaAndLightNovelWebScrape.Websites;

public sealed partial class MerryManga
{
    private readonly List<string> MerryMangaLinks = [];
    private readonly List<EntryModel> MerryMangaData = [];
    public const string WEBSITE_TITLE = "MerryManga";
    public const string WEBSITE_URL = "https://www.merrymanga.com";
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
    public const Region REGION = Region.America;

    private static readonly FrozenSet<string> _stockClasses = FrozenSet.Create(
        StringComparer.Ordinal,
        "instock",
        "outofstock",
        "onbackorder",
        "preorder",
        "available_at_warehouse"
    );

    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|Omnibus( \d{1,2})(?:, |\s{1})Vol \d{1,3}-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex FixOmnibusRegex();
    [GeneratedRegex(@"(?<=Box Set \d{1}).*", RegexOptions.IgnoreCase)] private static partial Regex FixBoxSetRegex();
    [GeneratedRegex(@" \(.*\)|,")] private static partial Regex FixTitleRegex();
    [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

    internal async Task CreateMerryMangaTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> MasterDataList, WebDriver driver)
    {
        await Task.Run(() => 
        {
            MasterDataList.Add(GetMerryMangaData(bookTitle, bookType, driver));
        });
    }

    internal void ClearData()
    {
        MerryMangaLinks.Clear();
        MerryMangaData.Clear();
    }

    internal string GetUrl()
    {
        return string.Join(" , ", MerryMangaLinks);
    }

    // https://www.merrymanga.com/?s=jujutsu+kaisen&post_type=product&orderby=date&_categories=manga
    // https://www.merrymanga.com/?s=Naruto&post_type=product&_categories=box-sets
    private string GenerateWebsiteUrl(string bookTitle, BookType bookType, bool hasBoxSet)
    {
        string url;
        if (hasBoxSet && bookType != BookType.LightNovel)
        {
            url = $"{WEBSITE_URL}/?s={InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"))}&post_type=product&orderby=date&_categories=box-sets";
        }
        else
        {
            url = $"{WEBSITE_URL}/?s={InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"))}&post_type=product&orderby=date&_categories={(bookType == BookType.Manga ? "manga" : "light-novels")}";
        }
        LOGGER.Info(url);
        MerryMangaLinks.Add(url);
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

    private void ExtractProductData(
        HtmlDocument doc,
        out List<string> titles,
        out List<string> prices,
        out List<StockStatus> statuses)
    {
        titles   = new List<string>();
        prices   = new List<string>();
        statuses = new List<StockStatus>();

        foreach (HtmlNode node in doc.DocumentNode.Descendants())
        {
            string nodeName = node.Name;

            // 1) TITLE: <h2 class="woocommerce-loop-product__title">
            if (nodeName == "h2")
            {
                // //h2[@class='woocommerce-loop-product__title']
                string classAttr = node.GetAttributeValue("class", string.Empty);
                if (classAttr.Equals("woocommerce-loop-product__title", StringComparison.Ordinal))
                {
                    string text = node.InnerText.Trim();
                    titles.Add(text);
                }
            }

            // 2) PRICE: match either
            //    a) <span class="price"> → <ins> → <span class="woocommerce-Price-amount amount"> → <bdi>
            // or b) <span class="price"> → <span class="woocommerce-Price-amount amount"> → <bdi>
            if (nodeName == "bdi")
            {
            HtmlNode parentNode = node.ParentNode;
            if (parentNode != null
                && parentNode.Name == "span"
                && parentNode.GetAttributeValue("class", string.Empty)
                            .Contains("woocommerce-Price-amount amount", StringComparison.Ordinal))
                    {
                        HtmlNode grandParent = parentNode.ParentNode;
                        if (grandParent != null)
                        {
                            // case (a): under <ins>
                            if (grandParent.Name == "ins")
                            {
                                HtmlNode greatGrand = grandParent.ParentNode;
                                if (greatGrand != null
                                    && greatGrand.Name == "span"
                                    && greatGrand.GetAttributeValue("class", string.Empty)
                                                .Equals("price", StringComparison.Ordinal))
                                {
                                    string text = node.InnerText.Trim();
                                    prices.Add(text);
                                }
                            }
                            // case (b): directly under <span class="price">
                            else if (grandParent.Name == "span"
                                    && grandParent.GetAttributeValue("class", string.Empty)
                                                    .Equals("price", StringComparison.Ordinal))
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

    internal List<EntryModel> GetMerryMangaData(string bookTitle, BookType bookType, WebDriver driver)
    {
        try
        {
            bool hasBoxSet = bookType == BookType.Manga;
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
        Restart:
            driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle.ToLower(), bookType, hasBoxSet));
            wait.Until(driver => driver.FindElement(By.CssSelector("div[class='container main-content']")));

            HtmlDocument doc = new HtmlDocument
            {
                OptionCheckSyntax = false,
                DisableServerSideCode = true,
                OptionFixNestedTags = false,
                OptionAutoCloseOnEnd = false,
                OptionOutputOptimizeAttributeValues = false,
                OptionExtractErrorSourceText = false,
                OptionUseIdAttribute = false,
                OptionReadEncoding = false
            };
            doc.LoadHtml(driver.PageSource);

            if (hasBoxSet && doc.Text.Contains("No products were found matching your selection."))
            {
                LOGGER.Warn("No Entries Found, Checking Manga Only Link");
                MerryMangaLinks.Clear();
                hasBoxSet = false;
                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle.ToLower(), bookType, hasBoxSet));
                wait.Until(driver => driver.FindElement(By.CssSelector("div[class='container main-content']")));
            }

            if (driver.FindElements(By.ClassName("facetwp-load-more")).Count != 0)
            {
                // LOGGER.Info("Loading More Entries...");
                while (wait.Until(driver => driver.FindElements(By.ClassName("facetwp-load-more"))).Count != 0)
                {
                    LOGGER.Info("Loading More Entries...");
                    if (driver.FindElements(By.ClassName("woocommerce-info")).Count == 1 || driver.FindElements(By.CssSelector("button[class='facetwp-load-more facetwp-hidden']")).Count == 1)
                    {
                        break;
                    }
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.ClassName("facetwp-load-more"))));
                    wait.Until(driver => driver.FindElement(By.CssSelector("div[class='facetwp-facet facetwp-facet-load_more facetwp-type-pager']")).Displayed);
                }
                doc.LoadHtml(driver.PageSource);
            }

            bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);

            // Get the page data from the HTML doc

            // HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
            // HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
            // HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);

            ExtractProductData(doc, out List<string> titleData, out List<string> priceData, out List<StockStatus> stockStatusData);

            for (int x = 0; x < titleData.Count; x++)
            {
                string entryTitle = titleData[x];
                if (
                    InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                    && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
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
                    MerryMangaData.Add(
                        new EntryModel
                        (
                            ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol").Trim(), bookTitle, bookType),
                            priceData[x],
                            stockStatusData[x],
                            WEBSITE_TITLE
                        )
                    );
                }
                else
                {
                    LOGGER.Debug("Removed {}", entryTitle);
                }
            }

            if (hasBoxSet)
            {
                hasBoxSet = false;
                goto Restart;
            }
        }
        catch (Exception ex)
        {
            LOGGER.Error("{} ({}) Error @ {} \n{}", bookTitle, bookType, WEBSITE_TITLE, ex);
        }
        finally
        {
            if (!MasterScrape.IsWebDriverPersistent)
            {
                driver?.Quit();
            }
            else
            {
                driver?.Close();
            }
            MerryMangaData.Sort(EntryModel.VolumeSort);
            MerryMangaData.RemoveDuplicates(LOGGER);

            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, bookType, MerryMangaData, LOGGER);
        }

        return MerryMangaData;
    }
}