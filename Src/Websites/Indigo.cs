using System.Security.Cryptography;

namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class Indigo
    {
        public List<string> IndigoLinks = new();
        public List<EntryModel> IndigoData = new();
        public const string WEBSITE_TITLE = "Indigo";
        private const decimal PLUM_DISCOUNT = 0.1M;
        private static readonly Logger LOGGER = LogManager.GetLogger("IndigoLogs");
        public const Region REGION = Region.Canada;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='link secondary']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price-wrapper']/span/span");
        private static readonly XPathExpression EntryLinkXPath = XPathExpression.Compile("//a[@class='link secondary']");
        private static readonly XPathExpression OneShotTitleXPath = XPathExpression.Compile("//h1[@class='product-name font-weight-mid']");
        private static readonly XPathExpression OneShotPriceXPath = XPathExpression.Compile("//span[@class='value']");
        private static readonly XPathExpression OneShotCheckXPath = XPathExpression.Compile("//span[@class='search-result-count' and contains(text(), 'Results for')]");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='col-12 pdp-checkout-button']/button[2]/text()");
        private static readonly XPathExpression FormatCheckXPath = XPathExpression.Compile("//tr[@class='format-spec ']/td");

        [GeneratedRegex(@",| \(manga\)|(?<=\d{1,3}): .*| Manga|\s+\(.*?\)| The Manga|", RegexOptions.IgnoreCase)] private static partial Regex TitleRegex();
        [GeneratedRegex(@"(?<=Box Set \d{1}).*|\s+Complete", RegexOptions.IgnoreCase)] private static partial Regex BoxSetTitleRegex();
        [GeneratedRegex(@"(?<=Vol \d{1,3})[^\d{1,3}.]+.*", RegexOptions.IgnoreCase)] private static partial Regex NovelitleRegex();
        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)")] private static partial Regex OmnibusRegex();
        [GeneratedRegex(@"Vol\.|Vols\.|Volume", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();

        protected internal async Task CreateIndigoTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetIndigoData(bookTitle, book, isMember));
            });
        }

        protected internal void ClearData()
        {
            IndigoLinks.Clear();
            IndigoData.Clear();
        }

        protected internal string GetUrl()
        {
            return IndigoLinks.Count != 0 ? IndigoLinks[0] : $"{WEBSITE_TITLE} Has no Link"; 
        }

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType)
        {
            // https://www.indigo.ca/en-ca/books/?q=One+Piece+manga&prefn1=Language&prefv1=English&start=0&sz=1000
            // https://www.indigo.ca/en-ca/books/?q=fullmetal+alchemist+novel&prefn1=Language&prefv1=English&start=0&sz=1000
            string url = $"https://www.indigo.ca/en-ca/books/?q={bookTitle.Replace(' ', '+')}+{(bookType == BookType.Manga ? "manga" : "novel")}&prefn1=Language&prefv1=English&start=0&sz=1000";
            LOGGER.Info(url);
            IndigoLinks.Add(url);
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
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
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
            
            // LOGGER.Debug("{} | {} | {} | {} | {}", curTitle, !entryTitle.Contains("Vol"), !entryTitle.Contains("Box Set"), !entryTitle.Contains("Anniversary Book"), char.IsDigit(curTitle.ToString()[curTitle.Length - 1]));

            // InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '.');
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Deluxe", "Deluxe Edition");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, " Complete", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, " Color Edition", "In Color");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
            // InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '-');

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

        private List<EntryModel> GetIndigoData(string bookTitle, BookType bookType, bool isMember)
        {
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(GenerateWebsiteUrl(bookTitle, bookType));
                HtmlNodeCollection titleData = null;
                HtmlNodeCollection priceData = null;
                HtmlNodeCollection entryLinkData = doc.DocumentNode.SelectNodes(EntryLinkXPath);
                HtmlNode oneShotStockCheck = null;
                bool isOneShot = false;
                bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle);

                if (doc.DocumentNode.SelectSingleNode(OneShotCheckXPath) != null)
                {
                    titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                }
                else
                {
                    titleData = doc.DocumentNode.SelectNodes(OneShotTitleXPath);
                    priceData = doc.DocumentNode.SelectNodes(OneShotPriceXPath);
                    oneShotStockCheck = doc.DocumentNode.SelectSingleNode(OneShotCheckXPath);
                    isOneShot = true;
                }

                string price = string.Empty;
                for(int x = 0; x < titleData.Count; x++)
                {
                    string entryTitle = System.Net.WebUtility.HtmlDecode(titleData[x].InnerText);
                    string titleDesc = titleData[x].GetAttributeValue("data-adobe-tracking", "Book Type Error");
                    // LOGGER.Debug("{} {}", entryTitle, titleDesc.Contains("novel", StringComparison.OrdinalIgnoreCase));
                    if ((!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                        && (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) || InternalHelpers.TitleStartsWithCheck(bookTitle, entryTitle))
                        && !entryTitle.Contains("library edition", StringComparison.OrdinalIgnoreCase)
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
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "Flame Dragon Knight")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Kuklo Unbound")
                                        || (InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Lost Girls") && !entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase))
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
                        if (!isOneShot) { doc = web.Load($"https://www.indigo.ca{entryLinkData[x].GetAttributeValue("href", "error")}"); }
                        // LOGGER.Debug("{} | {} | {}", entryTitle, doc.DocumentNode.SelectSingleNode(StockStatusXPath).InnerText.Trim(), $"https://www.indigo.ca{entryLinkData[x].GetAttributeValue("href", "error")}");
                        string format = doc.DocumentNode.SelectSingleNode(FormatCheckXPath).InnerText;
                        // LOGGER.Debug("{} | {}", entryTitle, eBookCheckNode == null);

                        if (!format.EndsWith("eBook", StringComparison.OrdinalIgnoreCase) && !format.EndsWith("Binding", StringComparison.OrdinalIgnoreCase))
                        {
                            price = priceData[x].InnerText.Trim();
                            IndigoData.Add(
                                new EntryModel(
                                    ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                    isMember ? $"${EntryModel.ApplyDiscount(Convert.ToDecimal(price[1..]), PLUM_DISCOUNT)}" : price,
                                    doc.DocumentNode.SelectSingleNode(StockStatusXPath).InnerText.Trim() switch
                                    {
                                        "Add to Bag" => StockStatus.IS,
                                        "Pre-Order" => StockStatus.PO,
                                        "Coming Soon" => StockStatus.OOS,
                                        _ => StockStatus.NA
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else { LOGGER.Info("Removed {}", entryTitle); }
                    }
                    else { LOGGER.Info("Removed {}", entryTitle); }
                }

            }
            catch (Exception ex)
            {
                LOGGER.Error($"{bookTitle} Does Not Exist @ Indigo {ex}");
            }

            IndigoData = IndigoData.Distinct().ToList();
            IndigoData.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, IndigoData, LOGGER);

            return IndigoData;
        }
    }
}