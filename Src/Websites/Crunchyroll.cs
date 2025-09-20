using System.Net;
using MangaAndLightNovelWebScrape.Services;
using Microsoft.Playwright;

namespace MangaAndLightNovelWebScrape.Websites;

internal sealed partial class Crunchyroll : IWebsite
{
    private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

    private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='tab-content']//div[@class='pdp-link']/a");
    private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//div[@class='tab-content']//span[@class='sales']/span");
    private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='tab-content']//div[@class='product-tile js-product-tile']//div[@class='image-container']//div[@class='product-sashes']");

    /// <inheritdoc />
    public const string TITLE = "Crunchyroll";

    /// <inheritdoc />
    public const string BASE_URL = "https://store.crunchyroll.com";

    /// <inheritdoc />
    public const Region REGION = Region.America;

    [GeneratedRegex(@"Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
    [GeneratedRegex(@",|\(.*?\)| Manga| Graphic Novel|:|(?<=(?:Vol|Box Set)\s+\d{1,3}(?:\.\d)?\s+).*|Hardcover", RegexOptions.IgnoreCase)] private static partial Regex ParseAndCleanTitleRegex();
    [GeneratedRegex(@",| Manga| Graphic Novel|:|(?:Vol|Box Set)\s+\d{1,3}(\.\d)?[^\d]+.*|Hardcover", RegexOptions.IgnoreCase)] private static partial Regex BundleParseRegex();
    [GeneratedRegex(@"(?:\d-in-\d|Omnibus) Edition", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
    [GeneratedRegex(@"\((\d{1,3}-\d{1,3})\) Bundle", RegexOptions.IgnoreCase)] private static partial Regex BundleVolRegex();

    public Task CreateTask(string bookTitle, BookType bookType, ConcurrentBag<List<EntryModel>> masterDataList, ConcurrentDictionary<Website, string> masterLinkList, IBrowser? browser, Region curRegion, (bool IsBooksAMillionMember, bool IsKinokuniyaUSAMember) memberships)
    {
        return Task.Run(async () =>
        {
            (List<EntryModel> Data, List<string> Links) = await GetData(bookTitle, bookType);
            masterDataList.Add(Data);
            masterLinkList.TryAdd(Website.Crunchyroll, Links[0]);
        });
    }

    private static string GenerateWebsiteUrl(BookType bookType, string bookTitle, bool retry = false)
    {
        // https://store.crunchyroll.com/search?q=naruto&prefn1=subcategory&prefv1=Light%20Novels
        // https://store.crunchyroll.com/collections/jujutsu-kaisen/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles
        // https://store.crunchyroll.com/collections/one-piece/?cgid=one-piece&prefn1=category&prefv1=Manga%20%26%20Books&sz=200
        // https://store.crunchyroll.com/collections/one-piece/?cgid=one-piece&prefn1=category&prefv1=Manga%20%26%20Books&start=100&sz=100
        // https://store.crunchyroll.com/search?q=Akane-Banashi&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga&sz={maxTotalProducts}
        
        bookTitle = InternalHelpers.FilterBookTitle(bookTitle);
        string url = bookType == BookType.Manga
            ? (retry
                ? $"{BASE_URL}/collections/{bookTitle}/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz={int.MaxValue}"
                : $"{BASE_URL}/search?q={bookTitle}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz={int.MaxValue}")
            : (retry
                ? $"{BASE_URL}/collections/{bookTitle}/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Novels&sz={int.MaxValue}" :
                $"{BASE_URL}/search?q={bookTitle}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Novels&sz={int.MaxValue}");

        LOGGER.Info(url);
        return url;
    }

    private static string ParseAndCleanTitle(string entryTitle, string baseTitleText, string bookTitle, BookType bookType)
    {
        StringBuilder curTitle;

        // Check if we need to replace "Omnibus" or "Bundle"
        if (OmnibusRegex().IsMatch(entryTitle))
        {
            curTitle = new StringBuilder(OmnibusRegex().Replace(entryTitle, "Omnibus"));
        }
        else if (!bookTitle.Contains("Bundle") && entryTitle.Contains("Bundle"))
        {
            curTitle = new StringBuilder(BundleVolRegex().Replace(entryTitle, "Bundle Vol $1"));
        }
        else
        {
            curTitle = new StringBuilder(entryTitle);
        }

        // Perform specific changes for Manga books
        if (bookType == BookType.Manga)
        {
            if (entryTitle.Contains("Deluxe Edition"))
            {
                curTitle.Replace("Omnibus ", string.Empty).Replace("Deluxe Edition", "Deluxe");
            }

            if (entryTitle.Contains("with Playing Cards", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(" with Playing Cards", string.Empty);
                int index = MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index;
                if (index > 0)
                {
                    curTitle.Insert(index, "Special Edition Vol ");
                }
            }

            if (!entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set"))
            {
                var volMatch = MasterScrape.FindVolNumRegex().Match(entryTitle);
                if (volMatch.Success)
                {
                    curTitle.Insert(volMatch.Index, "Vol ");
                }
            }

            if (bookTitle.Equals("attack on titan", StringComparison.OrdinalIgnoreCase) && baseTitleText.Contains("(Hardcover)") && !curTitle.ToString().Contains("In Color")&& !curTitle.ToString().Contains("Color Edition"))
            {
                curTitle.Append(" In Color");
            }
        }
        else if (bookType == BookType.LightNovel && !entryTitle.Contains("Novel"))
        {
            if (entryTitle.Contains("Vol"))
            {
                int volIndex = entryTitle.IndexOf("Vol");
                curTitle.Insert(volIndex, "Novel ");
            }
            else
            {
                curTitle.Append(" Novel");
            }
        }

        // Remove unwanted characters from the title
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '-', "Bundle");
        InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

        // Final cleanup and return
        return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
    }

    public async Task<(List<EntryModel> Data, List<string> Links)> GetData(string bookTitle, BookType bookType, IPage? page = null, bool isMember = false, Region curRegion = Region.America)
    {
        List<EntryModel> data = [];
        List<string> links = [];

        try
        {
            // Initialize once and reuse if necessary.
            HtmlWeb html = HtmlFactory.CreateWeb();

            bool bookTitleRemovalCheck = InternalHelpers.ShouldRemoveEntry(bookTitle);
            LOGGER.Debug(bookTitleRemovalCheck);

            // Load the document once after preparation.
            string url = GenerateWebsiteUrl(bookType, bookTitle);
            links.Add(url);

            HtmlDocument doc = await html.LoadFromWebAsync(url);
            doc.ConfigurePerf();

            // Get the page data from the HTML doc
            HtmlNodeCollection? titleData = doc.DocumentNode.SelectNodes(TitleXPath);
            HtmlNodeCollection? priceData = doc.DocumentNode.SelectNodes(PriceXPath);
            HtmlNodeCollection? stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);

            if (titleData == null && priceData == null && stockStatusData == null)
            {
                LOGGER.Info("Trying Second Link");
                data.Clear();
                links.Clear();

                url = GenerateWebsiteUrl(bookType, bookTitle, true);
                links.Add(url);
                doc = html.Load(url);
                titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
            }

            for (int x = 0; x < titleData!.Count; x++)
            {
                string entryTitle = WebUtility.HtmlDecode(titleData[x].InnerText.Trim());
                // First check: does the book title contain the entry title?
                if (!InternalHelpers.EntryTitleContainsBookTitle(bookTitle, entryTitle))
                {
                    LOGGER.Debug("Removed (1) {}", entryTitle);
                    continue;
                }

                // Second check: Is the entry title removed based on the regex or the removal flag?
                if (InternalHelpers.ShouldRemoveEntry(entryTitle) && !bookTitleRemovalCheck)
                {
                    LOGGER.Debug("Removed (2) {}", entryTitle);
                    continue;
                }

                bool shouldRemoveEntry = false;
                if (bookType == BookType.Manga)
                {
                    shouldRemoveEntry =
                        (!bookTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)) ||
                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, ["of Gluttony", "Darkness Ink"]) ||
                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto") ||
                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Pirate Recipes");
                }
                else if (bookType == BookType.LightNovel)
                {
                    shouldRemoveEntry = InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented");
                }

                if (!shouldRemoveEntry)
                {
                    entryTitle = FixVolumeRegex().Replace(entryTitle, "Vol");

                    // Apply different parsing logic based on whether it's a bundle
                    entryTitle = !entryTitle.Contains("Bundle")
                        ? ParseAndCleanTitleRegex().Replace(entryTitle, string.Empty)
                        : BundleParseRegex().Replace(entryTitle, string.Empty);

                    string cleanedTitle = ParseAndCleanTitle(entryTitle, titleData[x].InnerText, bookTitle, bookType);

                    // Retrieve stock status in a more efficient manner
                    string stockStatusText = stockStatusData![x].SelectSingleNode("./div/span")?.InnerText.Trim() ?? string.Empty;
                    StockStatus stockStatus = stockStatusText switch
                    {
                        "SOLD-OUT" => StockStatus.OOS,
                        "PRE-ORDER" => StockStatus.PO,
                        "Back Order" => StockStatus.BO,
                        "COMING-SOON" => StockStatus.CS,
                        _ => StockStatus.IS,
                    };

                    // Create the EntryModel and add it to Data
                    data.Add(new EntryModel(cleanedTitle, $"${priceData![x].GetAttributeValue("content", "ERROR")}", stockStatus, TITLE));
                }
                else
                {
                    LOGGER.Debug("Removed (3) {}", entryTitle);
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
            InternalHelpers.PrintWebsiteData(TITLE, bookTitle, bookType, data, LOGGER);
        }
        
        return (data, links);
    }
}