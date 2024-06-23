using System.Threading;

namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class TravellingMan
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("TravellingManLogs");
        private List<string> TravellingManLinks = new List<string>();
        private List<EntryModel> TravellingManData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "TravellingMan";
        public const Region REGION = Region.Britain;
        private static readonly List<string> DescRemovalStrings = ["novel", "figure", "sculpture", "collection of", "figurine", "statue", "miniature", "Figuarts"];
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/div/div[2]/div/span");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//li[@class='list-view-item']/div/div/div[3]/dl/div[2]/dd[2]/span[1]");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//ul[@class='list--inline pagination']/li[3]/a");
        [GeneratedRegex(@"Volume|Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@",| The Manga| Manga|\(.*?\)", RegexOptions.IgnoreCase)] private static partial Regex TitleParseRegex();
        [GeneratedRegex(@"(?:3-in-1|2-in-1)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
        [GeneratedRegex(@"(?<=Box Set \d{1,3})[^\d{1,3}.]+.*|(?:Box Set) Vol")] private static partial Regex BoxSetRegex();

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
            
            string url = $"https://travellingman.com/search?page={curPage}&q={bookTitle.Replace(" ", "+")}{(bookType == BookType.Manga ? "+manga" : "+novel")}/";
            LOGGER.Info("Url => {}", url);
            TravellingManLinks.Add(url);
            return url;
        }

        private static string TitleParse(string entryTitle, string bookTitle, BookType bookType)
        {
            if (OmnibusRegex().IsMatch(entryTitle))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
            }
            else if (BoxSetRegex().IsMatch(entryTitle))
            {
                entryTitle = BoxSetRegex().Replace(entryTitle, "Box Set");
                if (entryTitle.EndsWith("Box Set"))
                {
                    entryTitle = entryTitle[..entryTitle.AsSpan().LastIndexOf("Box Set")];
                }
            }

            StringBuilder curTitle = new StringBuilder(TitleParseRegex().Replace(entryTitle, string.Empty));
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

            if (entryTitle.Contains("Hardcover") && !bookTitle.Contains("Hardcover"))
            {
                curTitle.Replace(" Hardcover", string.Empty);
            }

            if (entryTitle.Contains("HC") && !bookTitle.Contains("HC"))
            {
                curTitle.Replace(" HC", string.Empty);
            }

            if (entryTitle.Contains("Box Set") && bookTitle.Equals("attack on titan", StringComparison.OrdinalIgnoreCase))
            {
                if (entryTitle.Contains("One", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("One", "1");
                }
                else if (entryTitle.Contains("Two", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Two", "2");
                }
                else if (entryTitle.Contains("Three", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Three", "3");
                }
            }

            if (bookType == BookType.LightNovel)
            {
                curTitle.Replace("Light Novel", string.Empty);
                int index = curTitle.ToString().AsSpan().IndexOf("Vol");
                if (index != -1) curTitle.Insert(index, "Novel ");
                else curTitle.Insert(curTitle.Length, " Novel");
            }
            else
            {
                if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Naruto Next Generations", string.Empty);
                }
            }
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
        }

        // TODO - Page source issue when a series has multiple pages, unsure why
        private List<EntryModel> GetTravellingManData(string bookTitle, BookType bookType)
        {
            try
            {
                HtmlWeb web = new()
                {
                    UsingCacheIfExists = true,
                    UseCookies = true,
                    OverrideEncoding = Encoding.UTF8
                };
                HtmlDocument doc = new()
                {
                    OptionCheckSyntax = false,
                    OptionDefaultStreamEncoding = Encoding.UTF8
                };

                int nextPage = 1;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                for(int x = 0; x < DescRemovalStrings.Count; x++)
                {
                    if (bookTitle.Contains(DescRemovalStrings[x])) DescRemovalStrings.RemoveAt(x);
                }
                if (bookType == BookType.LightNovel) DescRemovalStrings.Remove("novel");


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
                        // LOGGER.Debug("{} | {}", entryTitle, titleData[x].InnerHtml.Trim());
                        if (!entryTitle.Contains("Banpresto")
                            && !entryTitle.Contains("Nendoroid")
                            && InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && !(
                                bookType == BookType.Manga
                                && (
                                    !(!entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) || bookTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase))
                                    || (
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Itachi")
                                        )
                                    )
                                )
                            && !InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "Unimplemented")
                            )        
                        {
                            bool descIsValid = true;
                            if (!entryTitle.Contains("Volume", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Vol.", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Comic", StringComparison.OrdinalIgnoreCase))
                            {
                                HtmlNodeCollection descData = web.Load($"https://travellingman.com{doc.DocumentNode.SelectSingleNode($"(//li[@class='list-view-item']/div/a)[{x + 1}]").GetAttributeValue("href", string.Empty)}").DocumentNode.SelectNodes("//div[@class='product-single__description rte'] | //div[@class='product-single__description rte']//p");
                                StringBuilder desc = new StringBuilder();
                                foreach (HtmlNode node in descData) { desc.AppendLine(node.InnerText); }
                                // LOGGER.Debug("Checking Desc {} => {}", entryTitle, desc.ToString());
                                descIsValid = !desc.ToString().ContainsAny(DescRemovalStrings);
                            }

                            if (descIsValid)
                            {
                                TravellingManData.Add(
                                    new EntryModel
                                    (
                                        TitleParse(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                        priceData[x].InnerText.Trim(),
                                        StockStatus.IS,
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                            else { LOGGER.Info("Removed (2) {}", entryTitle); }
                        }
                        else { LOGGER.Info("Removed (1) {}", entryTitle); }
                    }

                    Stop:
                    if (priceData != null && priceData.Count == titleData.Count && pageCheck != null)
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
                TravellingManData.RemoveDuplicates(LOGGER);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, TravellingManData, LOGGER);
            }
            return TravellingManData;
        }
    }
}