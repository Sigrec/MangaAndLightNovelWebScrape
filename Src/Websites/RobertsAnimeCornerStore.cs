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
        private const Region WEBSITE_REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//font[@face='dom bold, arial, helvetica']/b");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//form[@method='POST'][contains(text()[2], '$')]//font[@color='#ffcc33'][2]");
        private static readonly XPathExpression SeriesTitleXPath = XPathExpression.Compile("//b//a[1]");

        [GeneratedRegex(@"-|\s+")] private static partial Regex FilterBookTitleRegex();
        [GeneratedRegex(@",|#| Graphic Novel| :|\(.*?\)|\[Novel\]")] private static partial Regex TitleFilterRegex();
        [GeneratedRegex(@",| #\d+-\d+| #\d+|Graphic Novel| :|\(.*?\)|\[Novel\]")] private static partial Regex OmnibusTitleFilterRegex();
        [GeneratedRegex(@"-(\d+)")] private static partial Regex OmnibusVolNumberRegex();
        [GeneratedRegex(@"\s+|[^a-zA-Z0-9]")] private static partial Regex FindTitleRegex();

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
        
        private string GetUrl(string bookTitle)
        {
            string url = "";
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

        private static string TitleParse(string titleText, BookType book)
        {
            StringBuilder curTitle;
            bool specialEditionCheck = false;
            if (titleText.Contains("Special Edition", StringComparison.OrdinalIgnoreCase))
            {
                specialEditionCheck = true;
            }

            if (titleText.Contains("Omnibus"))
            {
                LOGGER.Debug(titleText);
                uint volNum = Convert.ToUInt32(OmnibusVolNumberRegex().Match(titleText).Groups[1].Value);
                LOGGER.Debug(volNum);
                curTitle = new StringBuilder(OmnibusTitleFilterRegex().Replace(titleText, "").Trim());
                curTitle.Replace("Colossal Omnibus Edition", "Colossal Edition");
                curTitle.Replace("Omnibus Edition", "Omnibus");
                LOGGER.Debug(curTitle);
                if (!curTitle.ToString().Contains(" Vol"))
                {
                    curTitle.Append(" Vol");
                }
                curTitle.AppendFormat(" {0}", Math.Ceiling((decimal)volNum / 3));
                LOGGER.Debug(curTitle);
            }
            else
            {
                titleText = TitleFilterRegex().Replace(titleText, "").Trim();
                curTitle = new StringBuilder(titleText);
                curTitle.Replace("Deluxe Edition", "Deluxe Vol");
                if (titleText.Contains("Box Set") && !titleText.Any(char.IsDigit))
                {
                    curTitle.Append(" 1");
                }
            }

            if (specialEditionCheck)
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Special Edition ");
            }

            return book == BookType.Manga ? MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ") : MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Vol", "Novel Vol").ToString(), " ");
        }
        
        // TODO - Need to add special edition check (AoT)
        private List<EntryModel> GetRobertsAnimeCornerStoreData(string bookTitle, BookType bookType)
        {
            try
            {
                // Start scraping the URL where the data is found
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(GetUrl(bookTitle));
                string link = string.Empty;

                Parallel.ForEach(doc.DocumentNode.SelectNodes(SeriesTitleXPath), (series, state) =>
                {
                    string seriesText = MasterScrape.MultipleWhiteSpaceRegex().Replace(series.InnerText.Replace("Graphic Novels", "").Replace("Novels", ""), " ").Trim();
                    if (EntryModel.Similar(bookTitle, seriesText, string.IsNullOrWhiteSpace(seriesText) || bookTitle.Length > seriesText.Length ? bookTitle.Length / 6 : seriesText.Length / 6) != -1)
                    {
                        LOGGER.Debug(seriesText);
                        link = $"https://www.animecornerstore.com/{series.GetAttributeValue("href", "Url Error")}";
                        state.Stop();
                    }
                });

                if (!string.IsNullOrWhiteSpace(link))
                {
                    LOGGER.Info($"Final Url = {link}");
                    doc = web.Load(link);
                    RobertsAnimeCornerStoreLinks.Add(link);

                    List<HtmlNode> titleData = doc.DocumentNode.SelectNodes(TitleXPath).Where(title => !string.IsNullOrWhiteSpace(title.InnerText)).ToList();
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string titleText = titleData[x].InnerText;
                        // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                        if ( string.IsNullOrWhiteSpace(titleText) 
                            || titleText.Contains("Poster") 
                            || (
                                    (!titleText.Contains("Graphic Novel", StringComparison.OrdinalIgnoreCase))
                                    && bookType == BookType.Manga
                                ) 
                            || (
                                    titleText.Contains("Graphic") 
                                    && bookType == BookType.LightNovel
                                )
                            )
                        {
                            LOGGER.Debug($"Removed {titleText}");
                        }
                        else if (!MasterScrape.EntryRemovalRegex().IsMatch(titleText) || BookTitleRemovalCheck)
                        {
                            RobertsAnimeCornerStoreData.Add(
                                new EntryModel(
                                    TitleParse(titleText, bookType),
                                    priceData[x].InnerText.Trim(),
                                    titleText switch
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
                            LOGGER.Debug($"Removed {titleText}");
                        }
                    }

                    RobertsAnimeCornerStoreData.Sort(MasterScrape.VolumeSort);
                }
            }
            catch(Exception ex)
            {
                LOGGER.Warn(bookTitle + " Does Not Exist at RobertsAnimeCornerStore\n" + ex);
            }

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\RobertsAnimeCornerStoreData.txt"))
                {
                    if (RobertsAnimeCornerStoreData.Count != 0)
                    {
                        foreach (EntryModel data in RobertsAnimeCornerStoreData)
                        {
                            LOGGER.Debug(data);
                            outputFile.WriteLine(data);
                        }
                    }
                    else
                    {
                        LOGGER.Warn($"{bookTitle} Does Not Exist at RobertsAnimeCornerStore");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist at RobertsAnimeCornerStore");
                    }
                } 
            }
            
            return RobertsAnimeCornerStoreData;
        }
    }
}