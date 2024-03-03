namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class Crunchyroll
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("CrunchyrollLogs");
        private List<string> CrunchyrollLinks = new List<string>();
        private List<EntryModel> CrunchyrollData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Crunchyroll";
        // private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        public const Region REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='pdp-link']/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='sales']/span");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='product-sashes']/div[1]/span");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//a[@class='right-arrow']");
        [GeneratedRegex(@"Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@",|\(.*?\)| Manga| Graphic Novel|:|Hardcover|(?<=Vol \d{1,3})[^\d{1,3}.]+.*|(?<=Vol \d{1,3}.\d{1})[^\d{1,3}.]+.*")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(@"(?:3-in-1|2-in-1|Omnibus) Edition", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

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

        private string GenerateWebsiteUrl(BookType bookType, string bookTitle, int nextPage)
        {
            // https://store.crunchyroll.com/search?q=jujutsu%20kaisen&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga
            // https://store.crunchyroll.com/search?q=overlord&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Manga
            // https://store.crunchyroll.com/search?q=overlord&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2=Novels
            
            string url = $"https://store.crunchyroll.com/search?q={InternalHelpers.FilterBookTitle(bookTitle)}&prefn1=category&prefv1=Manga%20%26%20Books&prefn2=subcategory&prefv2={(bookType == BookType.Manga ? "Manga" : "Novels")}&srule=Product%20Name%20(A-Z)&start={nextPage}&sz=100";
            LOGGER.Info(url);
            CrunchyrollLinks.Add(url);
            return url;
        }

        private static string TitleParse(string entryTitle, string baseTitleText, string bookTitle, BookType bookType)
        {
            StringBuilder curTitle;
            if (OmnibusRegex().IsMatch(entryTitle))
            {
                curTitle = new StringBuilder(OmnibusRegex().Replace(entryTitle, "Omnibus"));
            }
            else
            {
                curTitle = new StringBuilder(entryTitle);
            }

            if (bookType == BookType.Manga)
            {
                if (entryTitle.Contains("Deluxe Edition"))
                {
                    curTitle.Replace("Omnibus ", string.Empty).Replace("Deluxe Edition", "Deluxe");
                }

                if (entryTitle.Contains("with Playing Cards", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace(" with Playing Cards", string.Empty);
                    curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index, "Special Edition Vol ");;
                }
                entryTitle = curTitle.ToString();

                if (!entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set"))
                {
                    if (MasterScrape.FindVolNumRegex().IsMatch(entryTitle))
                    {
                        curTitle.Insert(MasterScrape.FindVolNumRegex().Match(entryTitle).Index, "Vol ");
                    }
                    // else if (baseTitleText.Contains("(Hardcover)"))
                    // {
                    //     return curTitle.Append(" Hardcover").ToString();
                    // }
                }

                if (bookTitle.Equals("attack on titan", StringComparison.OrdinalIgnoreCase) && baseTitleText.Contains("(Hardcover)") && !curTitle.ToString().Contains("In Color"))
                {
                    return curTitle.Append(" In Color").ToString();
                }
            }
            else if (bookType == BookType.LightNovel && !entryTitle.Contains("Novel"))
            {
                if (entryTitle.Contains("Vol"))
                {
                    curTitle.Insert(entryTitle.AsSpan().IndexOf("Vol"), "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }

            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '-');
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Hardcover", string.Empty).ToString(), " ").Trim();
        }

        private List<EntryModel> GetCrunchyrollData(string bookTitle, BookType bookType)
        {
            try
            {
                HtmlWeb web = new()
                {
                    UsingCacheIfExists = true,
                    UseCookies = false
                };
                HtmlDocument doc = new()
                {
                    OptionCheckSyntax = false,
                };
                int nextPage = 0;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                while (true)
                {
                    // Initialize the html doc for crawling
                    doc = web.Load(GenerateWebsiteUrl(bookType, bookTitle, nextPage));

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();
                        if (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && !(
                                bookType == BookType.Manga
                                && (
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                    )
                                )
                            )        
                        {
                            CrunchyrollData.Add(
                                new EntryModel
                                (
                                    TitleParse(TitleParseRegex().Replace(FixVolumeRegex().Replace(entryTitle, "Vol"), string.Empty).Trim(), entryTitle, bookTitle, bookType),
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
                            LOGGER.Info("Removed {}", entryTitle);
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
                LOGGER.Error("{} Does Not Exist @ {} \n{}", bookTitle, WEBSITE_TITLE, ex);
            }
            
            CrunchyrollData.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, CrunchyrollData, LOGGER);
            return CrunchyrollData;
        }
    }
}