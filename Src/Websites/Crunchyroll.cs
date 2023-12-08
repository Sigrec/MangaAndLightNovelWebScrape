namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class Crunchyroll
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("CrunchyrollLogs");
        private List<string> CrunchyrollLinks = new List<string>();
        private List<EntryModel> CrunchyrollData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Crunchyroll";
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        private const Region WEBSITE_REGION = Region.America | Region.Canada;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='pdp-link']/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='sales']/span");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[contains(@class, 'sash-content')]");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//a[@class='right-arrow']");
        
        [GeneratedRegex(@",|\(.*?\)| Manga|:|Comics|Hardcover|(?<=Vol \d{1,3})[^\d{1,3}.]+.*|(?<=Vol \d{1,3}.\d{1})[^\d{1,3}.]+.*")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(@"(?:3 in 1|2 in 1|Omnibus) Edition", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

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

        private string GetUrl(BookType bookType, string bookTitle, int nextPage)
        {
            // https://store.crunchyroll.com/search?q=jujutsu%20kaisen&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga
            // https://store.crunchyroll.com/search?q=overlord&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga
            // https://store.crunchyroll.com/search?q=overlord&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Novels
            
            string url = $"https://store.crunchyroll.com/search?q={MasterScrape.FilterBookTitle(bookTitle)}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2={(bookType == BookType.Manga ? "Manga" : "Novels")}&srule=Product%20Name%20(A-Z)&start={nextPage}&sz=100";
            LOGGER.Debug(url);
            CrunchyrollLinks.Add(url);
            return url;
        }

        private static string TitleParse(string titleText, string baseTitleText, string bookTitle, BookType bookType)
        {
            StringBuilder curTitle;
            if (OmnibusRegex().IsMatch(titleText))
            {
                curTitle = new StringBuilder(OmnibusRegex().Replace(titleText, "Omnibus"));
            }
            else
            {
                curTitle = new StringBuilder(titleText);
            }

            if (bookType == BookType.Manga)
            {
                if (titleText.Contains("Deluxe Edition"))
                {
                    curTitle.Replace("Omnibus ", "").Replace("Deluxe Edition", "Deluxe");
                }

                if (titleText.Contains("with Playing Cards", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace(" with Playing Cards", "");
                    curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index, "Special Edition Vol ");
                }
                titleText = curTitle.ToString();

                if (!titleText.Contains("Vol") && !titleText.Contains("Box Set"))
                {
                    if (MasterScrape.FindVolNumRegex().IsMatch(titleText))
                    {
                        curTitle.Insert(MasterScrape.FindVolNumRegex().Match(titleText).Index, "Vol ");
                    }
                    else if (baseTitleText.Contains("(Hardcover)"))
                    {
                        return curTitle.Append(" Hardcover").ToString();
                    }
                }
            }
            else if (bookType == BookType.LightNovel && !titleText.Contains("Novel"))
            {
                if (titleText.Contains("Vol"))
                {
                    curTitle.Insert(titleText.IndexOf("Vol"), "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }

            Match volGroup = MasterScrape.FindVolNumRegex().Match(curTitle.ToString());
            if (!string.IsNullOrWhiteSpace(volGroup.Value) && volGroup.Value[0] == '0')
            {
                curTitle.Remove(volGroup.Index, volGroup.Value.Length);
                curTitle.Insert(volGroup.Index, volGroup.Value.TrimStart('0'));
            }

            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '-');
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Hardcover", "").ToString(), " ").Trim();
        }

        private List<EntryModel> GetCrunchyrollData(string bookTitle, BookType bookType)
        {
            try
            {
                HtmlWeb web = new();
                HtmlDocument doc = new()
                {
                    OptionCheckSyntax = false,
                };
                int nextPage = 0;
                bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle);

                while (true)
                {
                    // Initialize the html doc for crawling
                    doc = web.Load(GetUrl(bookType, bookTitle, nextPage));

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string titleText = titleData[x].InnerText.Trim();
                        if (InternalHelpers.TitleContainsBookTitle(bookTitle, titleText)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(titleText) || BookTitleRemovalCheck)
                            && !(
                                bookType == BookType.Manga
                                && (
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText, "of Gluttony")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Boruto")
                                    )
                                )
                            )        
                        {
                            CrunchyrollData.Add(
                                new EntryModel
                                (
                                    TitleParse(TitleParseRegex().Replace(MasterScrape.FixVolumeRegex().Replace(titleText, "Vol"), "").Trim(), titleText, bookTitle, bookType),
                                    priceData[x].InnerText.Trim(),
                                    stockStatusData[x].InnerText.Trim() switch
                                    {
                                        "IN-STOCK" => StockStatus.IS,
                                        "SOLD-OUT" => StockStatus.OOS,
                                        "PRE-ORDER" => StockStatus.PO,
                                        _ => StockStatus.NA,
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Debug("Removed {}", titleText);
                        }
                    }

                    if (pageCheck != null)
                    {
                        nextPage += 100;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error($"{bookTitle} Does Not Exist @ Crunchyroll \n{ex}");
            }
            CrunchyrollData.Sort(MasterScrape.VolumeSort);

            MasterScrape.PrintWebsiteData(WEBSITE_TITLE, bookTitle, CrunchyrollData, LOGGER);
            return CrunchyrollData;
        }
    }
}