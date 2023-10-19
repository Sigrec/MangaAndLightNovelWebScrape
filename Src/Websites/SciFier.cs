namespace MangaLightNovelWebScrape.Websites
{
    public partial class SciFier
    {
        private List<string> SciFierLinks = new List<string>();
        private List<EntryModel> SciFierData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "SciFier";
        private static readonly Logger LOGGER = LogManager.GetLogger("SciFierLogs");
        private const Region WEBSITE_REGION = Region.America | Region.Europe | Region.Britain | Region.Canada;
        private static readonly Dictionary<Region, ushort> CURRENCY_DICTIONARY = new Dictionary<Region, ushort>
        {
            {Region.America, 2},
            {Region.Canada, 6},
            {Region.Europe, 5},
            {Region.Britain, 1}
        };
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//h3[@class='card-title']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price price--withTax price--main _hasSale']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//a[@aria-label='Next']");
        [GeneratedRegex(@",|(?<=Vol (?:\d{1,3}|\d{1,3}\.\d{1}))[^\d{1,3}.].*|\(Manga\)", RegexOptions.IgnoreCase)] private static partial Regex TitleFixRegex();

        internal async Task CreateSciFierTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, Region curRegion)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetSciFierData(bookTitle, bookType, curRegion));
            });
        }

        // Has issues where the search is not very strict unforunate
        private string GetUrl(string bookTitle, BookType bookType, Region curRegion)
        {
            // https://scifier.com/search.php?setCurrencyId=4&section=product&search_query_adv=jujutsu+kaisen&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2060&section=product

            // https://scifier.com/search.php?setCurrencyId=6&section=product&search_query_adv=classroom+of+the+elite&searchsubs=ON&brand=&price_from=&price_to=&featured=&category=2060&limit=100&sort=alphaasc&mode=6
            string url = $"https://scifier.com/search.php?setCurrencyId={CURRENCY_DICTIONARY[curRegion]}&section=product&search_query_adv={bookTitle.Replace(' ', '+')}{(bookType == BookType.LightNovel ? "+light+novel" : "")}&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2060&section=product&limit=100&sort=alphaasc&mode=6";
            LOGGER.Debug(url);
            SciFierLinks.Add(url);
            return url;
        }

        internal string GetUrl()
        {
            return SciFierLinks.Count != 0 ? SciFierLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }
        
        internal void ClearData()
        {
            SciFierLinks.Clear();
            SciFierData.Clear();
        }

        private static string TitleParse(string entryTitle, string bookTitle, BookType bookType)
        {
            if (bookType == BookType.Manga && !entryTitle.Contains("Vol"))
            {
                entryTitle = entryTitle.Insert(new Regex(@"\d{1,3}").Match(entryTitle).Index, "Vol ");
            }
            StringBuilder curTitle = new StringBuilder(TitleFixRegex().Replace(entryTitle, string.Empty));

            if (bookType == BookType.LightNovel)
            {
                curTitle.Replace("(Light Novel)", "Novel");
            }
            return curTitle.ToString();
        }

        private List<EntryModel> GetSciFierData(string bookTitle, BookType bookType, Region curRegion)
        {

            try
            {
                HtmlWeb web = new HtmlWeb();
                string url = GetUrl(bookTitle, bookType, curRegion);
                HtmlDocument doc = web.Load(url);

                while (true)
                {
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();
                        if (
                            !MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) 
                            && MasterScrape.TitleContainsBookTitle(bookTitle, entryTitle)
                            && !(
                                    (
                                        bookType == BookType.Manga
                                        && entryTitle.Contains("(Light Novel)")
                                    )
                                    ||
                                    (
                                        bookType == BookType.LightNovel
                                        && entryTitle.Contains("(Manga)")
                                    )
                                )
                            )
                        {
                            SciFierData.Add(
                                new EntryModel
                                (
                                    TitleParse(MasterScrape.FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                    priceData[x].InnerText.Trim(), 
                                    StockStatus.IS, 
                                    WEBSITE_TITLE
                                )
                            );
                        }
                    }
                    
                    if (pageCheck != null)
                    {
                        url = $"https://scifier.com/{pageCheck.GetAttributeValue("href", "Url Error")}";
                        doc = web.Load($"https://scifier.com/{pageCheck.GetAttributeValue("href", "Url Error")}");
                        LOGGER.Info($"Next Page {url}");
                    }
                    else
                    {
                        SciFierData.Sort(new VolumeSort());
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Debug($"{bookTitle} Does Not Exist @ {WEBSITE_TITLE} \n{e}");
            }

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\SciFierData.txt"))
                {
                    if (SciFierData.Count != 0)
                    {
                        foreach (EntryModel data in SciFierData)
                        {
                            LOGGER.Debug(data);
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        LOGGER.Debug($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                    }
                }
            }
            return SciFierData;
        }
    }
}