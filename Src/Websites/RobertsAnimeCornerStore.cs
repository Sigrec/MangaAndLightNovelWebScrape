namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class RobertsAnimeCornerStore
    {
        private List<string> RobertsAnimeCornerStoreLinks = new List<string>(2);
        private List<EntryModel> RobertsAnimeCornerStoreData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "RobertsAnimeCornerStore";
        private static readonly Logger LOGGER = LogManager.GetLogger("RobertsAnimeCornerStoreLogs");
        private static readonly Dictionary<string, string> URL_MAP_DICT = new()
        { 
            {"mangrapnovag", @"^[a-bA-B\d]"},
            {"mangrapnovhp", @"^[c-dC-D]"},
            {"mangrapnovqz", @"^[e-gE-G]"},
            {"magrnomo", @"^[h-kH-K]"},
            {"magrnops", @"^[l-nL-N]"},
            {"magrnotz", @"^[o-qO-Q]"},
            {"magrnors", @"^[r-sR-S]"},
            {"magrnotv", @"^[t-vT-V]"},
            {"magrnowz", @"^[w-zW-Z]"}
        };
        public const Region REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//font[@face='dom bold, arial, helvetica']/b");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//form[@method='POST'][contains(text()[2], '$')]//font[@color='#ffcc33'][2]");
        private static readonly XPathExpression SeriesTitleXPath = XPathExpression.Compile("//b//a[1]");

        [GeneratedRegex(@"-|\s+")] private static partial Regex FilterBookTitleRegex();
        [GeneratedRegex(@",|#| Graphic Novel| :|\(.*?\)|\[Novel\]")] private static partial Regex TitleFilterRegex();
        [GeneratedRegex(@",| #\d+-\d+| #\d+|Graphic Novel| :|\(.*?\)|\[Novel\]")] private static partial Regex OmnibusTitleFilterRegex();
        [GeneratedRegex(@"-(\d+)")] private static partial Regex OmnibusVolNumberRegex();
        [GeneratedRegex(@"\s+|[^a-zA-Z0-9]")] private static partial Regex FindTitleRegex();
        [GeneratedRegex(@"\d{1,3}")] private static partial Regex FindVolNumRegex();

        internal async Task CreateRobertsAnimeCornerStoreTask(string bookTitle, BookType book, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetRobertsAnimeCornerStoreData(bookTitle, book));
            });
        }

        internal string GetUrl()
        {
            return RobertsAnimeCornerStoreLinks.Count != 0 ? RobertsAnimeCornerStoreLinks[^1] : $"{WEBSITE_TITLE} Has no Link";
        }
        
        private string GenerateWebsiteUrl(string bookTitle)
        {
            string url = string.Empty;
            // Gets the starting page based on first letter and checks if we are looking for the 1st webpage (false) or 2nd webpage containing the actual item data (true)
            Parallel.ForEach(URL_MAP_DICT, (link, state) =>
            {
                if (new Regex(link.Value).Match(bookTitle).Success)
                {
                    url = $"https://www.animecornerstore.com/{link.Key}.html";
                    state.Stop();
                }
            });
            RobertsAnimeCornerStoreLinks.Add(url);
            LOGGER.Info($"Initial Url = {url}");
            return url;
        }

        internal void ClearData()
        {
            RobertsAnimeCornerStoreLinks.Clear();
            RobertsAnimeCornerStoreData.Clear();
        }

        private static string TitleParse(string entryTitle, string bookTitle, BookType bookType)
        {
            StringBuilder curTitle;
            bool specialEditionCheck = false;
            if (entryTitle.Contains("Special Edition", StringComparison.OrdinalIgnoreCase))
            {
                specialEditionCheck = true;
            }

            if (entryTitle.Contains("Omnibus"))
            {
                uint volNum = Convert.ToUInt32(OmnibusVolNumberRegex().Match(entryTitle).Groups[1].Value);
                curTitle = new StringBuilder(OmnibusTitleFilterRegex().Replace(entryTitle, string.Empty).Trim());
                curTitle.Replace("Colossal Omnibus Edition", "Colossal Edition");
                curTitle.Replace("Omnibus Edition", "Omnibus");
                if (!curTitle.ToString().Contains(" Vol"))
                {
                    curTitle.Append(" Vol");
                }
                curTitle.AppendFormat(" {0}", Math.Ceiling((decimal)volNum / 3));
            }
            else
            {
                entryTitle = TitleFilterRegex().Replace(entryTitle, string.Empty).Trim();
                curTitle = new StringBuilder(entryTitle);
                curTitle.Replace("Deluxe Edition", "Deluxe Vol");
                if (entryTitle.Contains("Box Set") && !entryTitle.Contains("Collection") && !entryTitle.Any(char.IsDigit))
                {
                    curTitle.Append(" 1");
                }
            }
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

            if (specialEditionCheck)
            {
                curTitle.Replace(" Special Edition", string.Empty);
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Special Edition ");
            }

            if (bookType == BookType.LightNovel)
            {
                entryTitle = curTitle.ToString().Trim();
                if (!entryTitle.Contains("Vol") && FindVolNumRegex().IsMatch(entryTitle) && !FindVolNumRegex().IsMatch(bookTitle))
                {
                    curTitle.Replace(" Novel", string.Empty);
                    curTitle.Insert(FindVolNumRegex().Match(entryTitle).Index, " Vol ");
                }

                entryTitle = curTitle.ToString().Trim();
                if (entryTitle.Contains("Vol"))
                {
                    curTitle.Replace("Vol", "Novel Vol");
                }
                else if (!entryTitle.Contains("Novel"))
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ");
        }
        
        // TODO - Need to add special edition check (AoT)
        private List<EntryModel> GetRobertsAnimeCornerStoreData(string bookTitle, BookType bookType)
        {
            try
            {
                // Start scraping the URL where the data is found
                string starterUrl = GenerateWebsiteUrl(bookTitle);
                HtmlWeb web = new HtmlWeb();
                bool trySecondLink = false;

                Retry:
                string link = string.Empty;
                HtmlDocument doc = web.Load(starterUrl);
                Parallel.ForEach(doc.DocumentNode.SelectNodes(SeriesTitleXPath), (series, state) =>
                {
                    string innerSeriesText = series.InnerText;
                    string seriesText = MasterScrape.MultipleWhiteSpaceRegex().Replace(series.InnerText.Replace("Graphic Novels", string.Empty).Replace("Novels", string.Empty), " ").Trim();
                    bool isSimilar = EntryModel.Similar(bookTitle, seriesText, string.IsNullOrWhiteSpace(seriesText) || bookTitle.Length > seriesText.Length ? bookTitle.Length / 6 : seriesText.Length / 6) != -1;
                    if (isSimilar && (!trySecondLink || (bookType == BookType.LightNovel && trySecondLink && !innerSeriesText.Contains("Graphic Novels"))))
                    {
                        link = $"https://www.animecornerstore.com/{series.GetAttributeValue("href", "Url Error")}";
                        state.Stop();
                    }
                });

                if (!string.IsNullOrWhiteSpace(link))
                {
                    LOGGER.Info($"{(trySecondLink ? "2nd" : string.Empty)} Url = {link}");
                    doc = web.Load(link);
                    RobertsAnimeCornerStoreLinks.Add(link);

                    List<HtmlNode> titleData = doc.DocumentNode.SelectNodes(TitleXPath).Where(title => !string.IsNullOrWhiteSpace(title.InnerText)).ToList();
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText;
                        // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                        if ( string.IsNullOrWhiteSpace(entryTitle) 
                            || entryTitle.Contains("Poster") 
                            || (
                                    (!entryTitle.Contains("Graphic Novel", StringComparison.OrdinalIgnoreCase))
                                    && bookType == BookType.Manga
                                ) 
                            || (
                                    entryTitle.Contains("Graphic") 
                                    && bookType == BookType.LightNovel
                                )
                            )
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                        }
                        else if (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                        {
                            RobertsAnimeCornerStoreData.Add(
                                new EntryModel(
                                    TitleParse(entryTitle, bookTitle, bookType),
                                    priceData[x].InnerText.Trim(),
                                    entryTitle switch
                                    {
                                        string curTitle when curTitle.Contains("Pre Order") => StockStatus.PO,
                                        string curTitle when curTitle.Contains("Backorder") => StockStatus.OOS,
                                        _ => StockStatus.IS
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
                }

                if (!trySecondLink && bookType == BookType.LightNovel && RobertsAnimeCornerStoreData.Count == 0)
                {
                    LOGGER.Info("Trying Another Link");
                    trySecondLink = true;
                    goto Retry;
                }
            }
            catch(Exception ex)
            {
                LOGGER.Warn(bookTitle + " Does Not Exist @ RobertsAnimeCornerStore\n" + ex);
            }

            RobertsAnimeCornerStoreData.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, RobertsAnimeCornerStoreData, LOGGER);            
            return RobertsAnimeCornerStoreData;
        }
    }
}