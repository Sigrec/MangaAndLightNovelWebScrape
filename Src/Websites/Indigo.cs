using System.Security.Cryptography;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class Indigo
    {
        public List<string> IndigoLinks = new();
        public List<EntryModel> IndigoData = new();
        public const string WEBSITE_TITLE = "Indigo";
        private const decimal PLUM_DISCOUNT = 0.1M;
        private static readonly Logger LOGGER = LogManager.GetLogger("IndigoLogs");
        private const Region WEBSITE_REGION = Region.Canada;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='link secondary']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price-wrapper']/span/span");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='mb-0 product-tile-promotion mouse']");
        private static readonly XPathExpression OneShotTitleXPath = XPathExpression.Compile("//h1[@class='product-name font-weight-mid']");
        private static readonly XPathExpression OneShotPriceXPath = XPathExpression.Compile("//span[@class='value']");
        private static readonly XPathExpression OneShotCheckXPath = XPathExpression.Compile("//span[@class='search-result-count' and contains(text(), 'Results for')]");

        [GeneratedRegex(@",| \(manga\)|(?<=\d{1,3}): .*| Manga|\s+\(.*?\)| The Manga|", RegexOptions.IgnoreCase)] private static partial Regex TitleRegex();
        [GeneratedRegex(@"(?<=Box Set \d{1}).*|\s+Complete", RegexOptions.IgnoreCase)] private static partial Regex BoxSetTitleRegex();
        [GeneratedRegex(@"(?<=Vol \d{1,3})[^\d{1,3}.]+.*", RegexOptions.IgnoreCase)] private static partial Regex NovelitleRegex();
        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)")] private static partial Regex OmnibusRegex();

        protected internal async Task CreateIndigoTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetIndigoData(bookTitle, book, isMember));
            });
        }

        protected internal void ClearData()
        {
            if (this != null)
            {
                IndigoLinks.Clear();
                IndigoData.Clear();
            }
        }

        protected internal string GetUrl()
        {
            return IndigoLinks.Count != 0 ? IndigoLinks[0] : $"{WEBSITE_TITLE} Has no Link"; 
        }

        // https://www.indigo.ca/en-ca/search?q=one+piece+Manga&prefn1=BISACBindingTypeID&prefv1=Paperback%7CHardcover&prefn2=Language&prefv2=English&start=0&sz=1000
        private string GetUrl(string bookTitle, BookType bookType)
        {
            string url = $"https://www.indigo.ca/en-ca/search?q={bookTitle.Replace(' ', '+')}+{(bookType == BookType.Manga ? "manga" : "novel")}&prefn1=BISACBindingTypeID&prefv1=Paperback%7CHardcover&prefn2=Language&prefv2=English&start=0&sz=1000";
            LOGGER.Debug(url);
            IndigoLinks.Add(url);
            return url;
        }

        private string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
        {
            if (OmnibusRegex().IsMatch(entryTitle))
            {
                LOGGER.Debug(entryTitle);
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
            }
            else if (entryTitle.Contains("Box Set"))
            {
                entryTitle = BoxSetTitleRegex().Replace(entryTitle, "");
            }
            
            if (bookType == BookType.LightNovel)
            {
                entryTitle = NovelitleRegex().Replace(entryTitle, "");
            }

            StringBuilder curTitle = new StringBuilder(TitleRegex().Replace(entryTitle, ""));
            curTitle.Replace("Vols.", "Vol");
            curTitle.Replace(" Color Edition", "In Color");
            
            // LOGGER.Debug("{} | {} | {} | {} | {}", curTitle, !entryTitle.Contains("Vol"), !entryTitle.Contains("Box Set"), !entryTitle.Contains("Anniversary Book"), char.IsDigit(curTitle.ToString()[curTitle.Length - 1]));

            // MasterScrape.RemoveCharacterFromTitle(ref curTitle, bookTitle, '.');
            MasterScrape.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

            if (entryTitle.Contains("Deluxe") && !bookTitle.Contains("Deluxe"))
            {
                curTitle.Replace("Deluxe", "Deluxe Edition");
            }
            if (entryTitle.Contains("Complete") && !bookTitle.Contains("Complete"))
            {
                curTitle.Replace(" Complete", " ");
            }
            if (entryTitle.Contains('-') && !bookTitle.Contains('-'))
            {
                curTitle.Replace("-", " ");
            }

            if (entryTitle.Contains("Special Edition", StringComparison.OrdinalIgnoreCase))
            {
                int startIndex = curTitle.ToString().IndexOf(" Special Edition");
                curTitle.Remove(startIndex, curTitle.Length - startIndex);
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index, "Special Edition Vol ");
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

        protected internal List<EntryModel> GetIndigoData(string bookTitle, BookType bookType, bool isMember)
        {
            try
            {
                HtmlDocument doc = new();
                HtmlWeb web = new HtmlWeb();
                HtmlNodeCollection titleData = null;
                HtmlNodeCollection priceData = null;
                HtmlNodeCollection stockStatusData = null;
                HtmlNode oneShotStockCheck = null;
                bool isOneShot = false;
                doc = web.Load(GetUrl(bookTitle, bookType));
                bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle);

                if (doc.DocumentNode.SelectSingleNode(OneShotCheckXPath) != null)
                {
                    titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
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
                    // LOGGER.Debug("{} {}", entryTitle, titleDesc);
                    if ((!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                        && (MasterScrape.TitleContainsBookTitle(bookTitle, entryTitle) || MasterScrape.TitleStartsWithCheck(bookTitle, entryTitle))
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
                                        MasterScrape.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Ace's Story") 
                                        || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear Your Own World")
                                        || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                        || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "Flame Dragon Knight")
                                        || MasterScrape.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Kuklo Unbound")
                                        || (MasterScrape.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Lost Girls") && !entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase))
                                        || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
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
                        price = priceData[x].InnerText.Trim();
                        IndigoData.Add(
                            new EntryModel(
                                ParseTitle(MasterScrape.FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                isMember ? $"${EntryModel.ApplyDiscount(Convert.ToDecimal(price[1..]), PLUM_DISCOUNT)}" : price,
                                !isOneShot ? (!stockStatusData[x].InnerText.Contains("Pre-Order") ? StockStatus.IS : StockStatus.PO) : (oneShotStockCheck == null ? StockStatus.IS : StockStatus.PO),
                                WEBSITE_TITLE
                            )
                        );
                    }
                    else
                    {
                        LOGGER.Info("Removed {}", entryTitle);
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGER.Error($"{bookTitle} Does Not Exist @ Indigo {ex}");
            }

            IndigoData.Sort(MasterScrape.VolumeSort);

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\IndigoData.txt"))
                {
                    if (IndigoData.Count != 0)
                    {
                        foreach (EntryModel data in IndigoData)
                        {
                            LOGGER.Info(data);
                            outputFile.WriteLine(data);
                        }
                    }
                    else
                    {
                        LOGGER.Info($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                    }
                } 
            }

            return IndigoData;
        }
    }
}