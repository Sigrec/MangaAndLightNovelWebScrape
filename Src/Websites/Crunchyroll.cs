namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class Crunchyroll
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private List<string> CrunchyrollLinks = new List<string>();
        private List<EntryModel> CrunchyrollData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Crunchyroll";
        // private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        public const Region REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='pdp-link']/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='sales']/span");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='product-sashes']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//a[@class='right-arrow']");
        [GeneratedRegex(@"Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@",|\(.*?\)| Manga| Graphic Novel|:|(?<=(?:Vol|Box Set)\s+\d{1,3}(?:\.\d)?\s+).*|Hardcover", RegexOptions.IgnoreCase)] private static partial Regex ParseAndCleanTitleRegex();
        [GeneratedRegex(@",| Manga| Graphic Novel|:|(?:Vol|Box Set)\s+\d{1,3}(\.\d)?[^\d]+.*|Hardcover", RegexOptions.IgnoreCase)] private static partial Regex BundleParseRegex();
        [GeneratedRegex(@"(?:\d-in-\d|Omnibus) Edition", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
        [GeneratedRegex(@"\((\d{1,3}-\d{1,3})\) Bundle", RegexOptions.IgnoreCase)] private static partial Regex BundleVolRegex();

        internal void ClearData()
        {
            CrunchyrollLinks.Clear();
            CrunchyrollData.Clear();
        }

        internal async Task CreateCrunchyrollTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetCrunchyrollData(bookTitle, bookType));
            });
        }

        internal string GetUrl()
        {
            return CrunchyrollLinks.Count != 0 ? CrunchyrollLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GenerateWebsiteUrl(BookType bookType, string bookTitle, uint maxTotalProducts, bool retry = false)
        {
            // https://store.crunchyroll.com/search?q=naruto&prefn1=subcategory&prefv1=Light%20Novels
            // https://store.crunchyroll.com/collections/jujutsu-kaisen/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles
            // https://store.crunchyroll.com/collections/one-piece/?cgid=one-piece&prefn1=category&prefv1=Manga%20%26%20Books&sz=200
            // https://store.crunchyroll.com/collections/one-piece/?cgid=one-piece&prefn1=category&prefv1=Manga%20%26%20Books&start=100&sz=100
            // https://store.crunchyroll.com/search?q=Akane-Banashi&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga&sz={maxTotalProducts}
            
            string url = bookType == BookType.Manga
                ? (retry
                    ? $"https://store.crunchyroll.com/search?q={bookTitle.Replace(" ", "-").ToLower()}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz={maxTotalProducts}"
                    : $"https://store.crunchyroll.com/collections/{bookTitle.Replace(" ", "-").ToLower()}/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CManga%7CBundles&sz={maxTotalProducts}")
                : (retry
                    ? $"https://store.crunchyroll.com/search?q={bookTitle.Replace(" ", "-").ToLower()}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CLight%20Novels%7CBundles&sz={maxTotalProducts}"
                    : $"https://store.crunchyroll.com/collections/{bookTitle.Replace(" ", "-").ToLower()}/?prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Specialty%20Books%7CLight%20Novels%7CBundles&sz={maxTotalProducts}");

            LOGGER.Info(url);
            CrunchyrollLinks.Add(url);
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

        internal List<EntryModel> GetCrunchyrollData(string bookTitle, BookType bookType)
        {
            try
            {
                // Initialize once and reuse if necessary.
                HtmlWeb web = new HtmlWeb { UsingCacheIfExists = true, UseCookies = false };
                HtmlDocument doc;

                uint maxTotalProducts = 500;
                bool bookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);

                // Load the document once after preparation.
                doc = web.Load(GenerateWebsiteUrl(bookType, bookTitle, maxTotalProducts));

                // Get the page data from the HTML doc
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);

                if (titleData == null && priceData == null && stockStatusData == null)
                {
                    LOGGER.Info("Trying Second Link");
                    ClearData();
                    doc = web.Load(GenerateWebsiteUrl(bookType, bookTitle, maxTotalProducts, true));
                    titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                }

                for (int x = 0; x < titleData.Count; x++)
                {
                    string entryTitle = titleData[x].InnerText.Trim();
                    // First check: does the book title contain the entry title?
                    if (!InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle))
                        continue;

                    // Second check: Is the entry title removed based on the regex or the removal flag?
                    if (MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) && !bookTitleRemovalCheck)
                        continue;

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
                        string stockStatusText = stockStatusData[x].SelectSingleNode("./div[1]/span")?.InnerText.Trim() ?? string.Empty;
                        StockStatus stockStatus = stockStatusText switch
                        {
                            "IN-STOCK" => StockStatus.IS,
                            "SOLD-OUT" => StockStatus.OOS,
                            "PRE-ORDER" => StockStatus.PO,
                            "Back Order" => StockStatus.BO,
                            "COMING-SOON" => StockStatus.CS,
                            _ => StockStatus.BO,
                        };

                        // Create the EntryModel and add it to CrunchyrollData
                        CrunchyrollData.Add(new EntryModel(cleanedTitle, $"${priceData[x].GetAttributeValue("content", "ERROR")}", stockStatus, WEBSITE_TITLE));
                    }
                    else
                    {
                        LOGGER.Info("Removed {}", entryTitle);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("{} ({}) Error @ {} \n{}", bookTitle, bookType, WEBSITE_TITLE, ex);
            }
            
            CrunchyrollData.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, bookType, CrunchyrollData, LOGGER);
            return CrunchyrollData;
        }
    }
}