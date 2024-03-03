namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class TravellingMan
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("TravellingManLogs");
        private List<string> TravellingManLinks = new List<string>();
        private List<EntryModel> TravellingManData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "TravellingMan";
        // private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        public const Region REGION = Region.Britain;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/a/span");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/div/div[3]/dl/div[2]/dd[2]/span[1]");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//ul[@class='list--inline pagination']/li[3]/a");
        [GeneratedRegex(@"Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@",|\(.*?\)| Manga| Graphic Novel|:|Hardcover|(?<=Vol \d{1,3})[^\d{1,3}.]+.*|(?<=Vol \d{1,3}.\d{1})[^\d{1,3}.]+.*")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(@"(?:3-in-1|2-in-1|Omnibus) Edition", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

        internal void ClearData()
        {
            TravellingManLinks.Clear();
            TravellingManData.Clear();
        }

        internal async Task CreateTravellingManTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetTravellingManData(bookTitle, bookType));
            });
        }

        internal string GetUrl()
        {
            return TravellingManLinks.Count != 0 ? TravellingManLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType, int curPage)
        {
            // https://travellingman.com/search?page=2&q=naruto+manga
            
            string url = $"https://travellingman.com/search?page={curPage}&q={bookTitle.Replace(" ", "+")}+{(bookType == BookType.Manga ? "manga" : "novel")}";
            LOGGER.Info("Url => {}", url);
            TravellingManLinks.Add(url);
            return url;
        }

        private static string TitleParse(string entryTitle, string bookTitle, BookType bookType)
        {
            StringBuilder curTitle = new StringBuilder(entryTitle).Replace(",", string.Empty);
            return curTitle.ToString();
        }

        private List<EntryModel> GetTravellingManData(string bookTitle, BookType bookType)
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

                int nextPage = 1;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                doc = web.Load(GenerateWebsiteUrl(bookTitle, bookType, nextPage));
                while (true)
                {
                    // Initialize the html doc for crawling
                    doc = web.Load(GenerateWebsiteUrl(bookTitle, bookType, nextPage));

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                    if (priceData == null) { goto Stop; }

                    for (int x = 0; x < priceData.Count; x++)
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
                            TravellingManData.Add(
                                new EntryModel
                                (
                                    TitleParse(FixVolumeRegex().Replace(entryTitle, "Vol").Trim(), bookTitle, bookType),
                                    priceData[x].InnerText.Trim(),
                                    StockStatus.IS,
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                        }
                    }

                    Stop:
                    if (pageCheck != null)
                    {
                        nextPage++;
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
            finally
            {
                TravellingManData.Sort(EntryModel.VolumeSort);
                for(int x = 1; x < TravellingManData.Count; x++)
                {
                    if (TravellingManData[x] == TravellingManData[x - 1])
                    {
                        TravellingManData.RemoveAt(x);
                    }
                }
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, TravellingManData, LOGGER);
            }
            return TravellingManData;
        }
    }
}