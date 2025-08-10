using System.Threading;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class Indigo : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='link secondary']");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price-wrapper']/span/span");
    private static readonly XPathExpression EntryLinkXPath = XPathExpression.Compile("//a[@class='link secondary']");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//p[@class='delivery-option-details mouse']/span[2]");
    private static readonly XPathExpression FormatXPath = XPathExpression.Compile("//span[@class='tile-text-light mouse variant-format-label']");

    [GeneratedRegex(@",| \(manga\)|(?<=\d{1,3}): .*| Manga|\s+\(.*?\)| The Manga|", RegexOptions.IgnoreCase)] private static partial Regex TitleRegex();
    [GeneratedRegex(@"(?<=Box Set \d{1}).*|\s+Complete", RegexOptions.IgnoreCase)] private static partial Regex BoxSetTitleRegex();
    [GeneratedRegex(@"(?<=Vol \d{1,3})[^\d{1,3}.]+.*", RegexOptions.IgnoreCase)] private static partial Regex NovelitleRegex();
    [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|Omnibus\s+(\d{1,3}).*")] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"Vol\.|Vols\.|Volume", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();

    /// <inheritdoc />
    public const string TITLE = "Indigo";

    /// <inheritdoc />
    public const string BASE_URL = "https://www.indigo.ca";

    /// <inheritdoc />
    public const Region REGION = Region.Canada;
    private const decimal PLUM_DISCOUNT = 0.1M;
    
    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember, bool IsIndigoMember) memberships = default)
    {
        return Task.Run(async () =>
        {
            IPage page = await PlaywrightFactory.GetPageAsync(browser!);
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType, page, memberships.IsIndigoMember);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.MangaMart, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(string bookTitle, BookType bookType)
    {
        // https://www.indigo.ca/en-ca/books/?q=world+trigger+&prefn1=BISACBindingTypeID&prefv1=TP%7CPO&prefn2=Language&prefv2=English&start=0&sz=1000
        string url = $"{BASE_URL}/en-ca/books/?q={bookTitle.Replace(' ', '+')}{(bookType == BookType.Manga ? string.Empty : "+novel")}&prefn1=BISACBindingTypeID&prefv1=TP%7CPO&prefn2=Language&prefv2=English&start=0&sz=1000";
        LOGGER.Info(url);
        return url;
    }

    private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
    {
        if (bookType == BookType.LightNovel)
        {
            entryTitle = NovelitleRegex().Replace(entryTitle, string.Empty);
        }

        if (OmnibusRegex().IsMatch(entryTitle))
        {
            entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus $1");
        }
        else if (entryTitle.Contains("Box Set"))
        {
            entryTitle = BoxSetTitleRegex().Replace(entryTitle, string.Empty);
        }

        StringBuilder curTitle = new StringBuilder(TitleRegex().Replace(entryTitle, string.Empty));
        if (bookTitle.Contains("Boruto", StringComparison.OrdinalIgnoreCase))
        {
            curTitle.Replace(" Naruto Next Generations", string.Empty);
        }
        
        if (entryTitle.Contains("Collector's Edition"))
        {
            curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Collectors Edition ");
        }
        
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Deluxe", "Deluxe Edition");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, " Complete", " ");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, " Color Edition", "In Color");
        InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");

        if (entryTitle.Contains("Special Edition", StringComparison.OrdinalIgnoreCase))
        {
            int startIndex = curTitle.ToString().IndexOf(" Special Edition");
            curTitle.Remove(startIndex, curTitle.Length - startIndex);
            curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index, "Special Edition ");
        }

        if ((!entryTitle.Contains("Vol") || !curTitle.ToString().Contains("Vol")) && !entryTitle.Contains("Box Set") && !entryTitle.Contains("Anniversary Book") && char.IsDigit(curTitle.ToString()[curTitle.Length - 1]))
        {
            curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, "Vol ");
        }

        if (bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel") && !bookTitle.Contains("Novel"))
        {
            int startIndex = curTitle.ToString().IndexOf("Vol");
            curTitle.Insert(startIndex != -1 ? startIndex : curTitle.Length, startIndex != -1 ? "Novel " : " Novel");
        }

        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ");
    }

    // TODO - Indigo still not working need to fix
    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            //HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = HtmlFactory.CreateDocument();
            bool BookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);

            string url = GenerateWebsiteUrl(bookTitle, bookType);
            links.Add(url);

            //doc = web.Load(url);
            // driver!.Navigate().GoToUrl(url);
            // Thread.Sleep(10000);
            // wait.Until(driver => driver.FindElement(By.CssSelector("div[class='row product-grid mt-4 search-analytics']")));
            // doc.LoadHtml(driver.PageSource);

            HtmlDocument innerDoc = new();
            HtmlNodeCollection entryLinkData = doc.DocumentNode.SelectNodes(EntryLinkXPath);
            HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
            HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
            HtmlNodeCollection formatData = doc.DocumentNode.SelectNodes(FormatXPath);
            LOGGER.Debug("{} | {} | {}", titleData.Count, priceData.Count, formatData.Count);

            string price = string.Empty;
            for (int x = 0; x < titleData.Count; x++)
            {
                string entryTitle = titleData[x].InnerText.Trim();
                string titleDesc = titleData[x].GetAttributeValue("data-adobe-tracking", "Book Type Error");
                string format = formatData[x].InnerText.Trim();
                // LOGGER.Debug("{} | {} | {} | {}", bookTitle, entryTitle, !InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck, InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle));
                if ((!InternalHelpers.ShouldRemoveEntry(entryTitle) || BookTitleRemovalCheck)
                    && InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle)
                    && !entryTitle.Contains("library edition", StringComparison.OrdinalIgnoreCase)
                    && !format.ContainsAny(["eBook", "Binding", "Picture", "Board", "Audio", "Mass Market", "Toy", "Bound"])
                    && (
                        (
                            bookType == BookType.Manga
                            && !entryTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase)
                            && (
                                    entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase)
                                    ||
                                    !titleDesc.Contains("novel", StringComparison.OrdinalIgnoreCase)
                                )
                            && !(
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Ace's Story")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear Your Own World")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony", "Flame Dragon Knight")
                                    || (InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Kuklo Unbound", "Lost Girls") && !entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase))
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                            )
                        )
                        ||
                        (
                            bookType == BookType.LightNovel
                            && (
                                entryTitle.Contains("light novel", StringComparison.OrdinalIgnoreCase)
                                || entryTitle.Contains("novel", StringComparison.OrdinalIgnoreCase)
                                || titleDesc.Contains("novel", StringComparison.OrdinalIgnoreCase))
                        )
                        )
                    )
                {
                    // driver.Navigate().GoToUrl($"https://www.indigo.ca{entryLinkData[x].GetAttributeValue("href", "error")}");
                    // wait.Until(driver => driver.FindElements(By.CssSelector("p[class='delivery-option-details mouse']")));
                    // innerDoc.LoadHtml(driver.PageSource);
                    // LOGGER.Debug(innerDoc.Text);

                    price = priceData[x].InnerText.Trim();
                    // LOGGER.Debug("{} | {}", entryTitle, price);
                    data.Add(
                        new EntryModel(
                            ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                            isMember ? $"${EntryModel.ApplyDiscount(Convert.ToDecimal(price[1..]), PLUM_DISCOUNT)}" : price,
                            innerDoc.DocumentNode.SelectSingleNode(StockStatusXPath).InnerText switch
                            {
                                string status when status.Contains("In stock", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                string status when status.Contains("Pre-order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                string status when status.Contains("Out of stock", StringComparison.OrdinalIgnoreCase) => StockStatus.OOS,
                                _ => StockStatus.NA
                            },
                            TITLE
                        )
                    );
                }
                else { LOGGER.Info("Removed {}", entryTitle); }
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