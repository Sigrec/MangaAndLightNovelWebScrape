namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class RobertsAnimeCornerStore
    {
        private readonly List<string> RobertsAnimeCornerStoreLinks = new List<string>();
        private readonly List<EntryModel> RobertsAnimeCornerStoreData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "RobertsAnimeCornerStore";
        private static readonly Logger LOGGER = LogManager.GetLogger("RobertsAnimeCornerStoreLogs");
        private static readonly Dictionary<string, string> URL_MAP_DICT = new()
        { 
            {"mangalitenovab", @"^[a-bA-B\d]"}, // https://www.animecornerstore.com/mangalitenovab.html
            {"mangalitenovcd", @"^[c-dC-D]"}, // https://www.animecornerstore.com/mangalitenovcd.html
            {"mangalitenovef", @"^[e-fE-F]"}, // https://www.animecornerstore.com/mangalitenovef.html
            {"mangalitenovgh", @"^[g-hG-h]"}, // https://www.animecornerstore.com/mangalitenovgh.html
            {"mangalitenovik", @"^[i-kI-K]"}, // https://www.animecornerstore.com/mangalitenovik.html
            {"mangalitenovlm", @"^[l-mL-M]"}, // https://www.animecornerstore.com/mangalitenovlm.html
            {"mangalitenovnomagrnors", @"^[n-oN-O]"}, // https://www.animecornerstore.com/mangalitenovno.html
            {"mangalitenovpq", @"^[p-qP-Q]"}, // https://www.animecornerstore.com/mangalitenovpq.html
            {"mangalitenovrs", @"^[r-sR-S]"}, // https://www.animecornerstore.com/mangalitenovrs.html
            {"mangalitenovtu", @"^[t-uT-U]"}, // https://www.animecornerstore.com/mangalitenovtu.html
            {"mangalitenovvw", @"^[v-wV-W]"}, // https://www.animecornerstore.com/mangalitenovvw.html
            {"mangalitenovxz", @"^[x-zX-Z]"}, // https://www.animecornerstore.com/mangalitenovxz.html
        };
        public const Region REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//font[@face='dom bold, arial, helvetica']/b");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//form[@method='POST'][contains(text()[2], '$')]//font[@color='#ffcc33'][2]");
        private static readonly XPathExpression SeriesTitleXPath = XPathExpression.Compile("//b//a[1]");

        [GeneratedRegex(@",|#| Graphic Novel| :|\(.*?\)|\[Novel\]")] private static partial Regex TitleFilterRegex();
        [GeneratedRegex(@",| #\d+-\d+| #\d+|Graphic Novel|:.*(?=Omnibus)|\(.*?\)|\[Novel\]")] private static partial Regex OmnibusTitleFilterRegex();
        [GeneratedRegex(@"-(\d+)")] private static partial Regex OmnibusVolNumberRegex();
        [GeneratedRegex(@"\d{1,3}")] private static partial Regex FindVolNumRegex();

        internal async Task CreateRobertsAnimeCornerStoreTask(string bookTitle, BookType book, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetRobertsAnimeCornerStoreData(bookTitle, book));
            });
        }

        internal List<string> GetUrls()
        {
            return RobertsAnimeCornerStoreLinks.Count != 0 ? RobertsAnimeCornerStoreLinks : null;
        }
        
        private static string GenerateWebsiteUrl(string bookTitle)
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
                HtmlWeb web = new HtmlWeb() { UsingCacheIfExists = true };

                HashSet<string> links = new HashSet<string>();
                HtmlDocument doc = web.Load(starterUrl);
                int bookTitleSpaceCount = bookTitle.AsSpan().Count(" ");
                foreach (HtmlNode series in doc.DocumentNode.SelectNodes(SeriesTitleXPath))
                {
                    string innerSeriesText = series.InnerText;
                    string seriesText = MasterScrape.MultipleWhiteSpaceRegex().Replace(series.InnerText.Replace("Graphic Novels", string.Empty).Replace("Novels", string.Empty), " ").Trim();
                    bool isSimilar = EntryModel.Similar(bookTitle, seriesText, (string.IsNullOrWhiteSpace(seriesText) || bookTitle.Length > seriesText.Length ? bookTitle.Length / 6 : seriesText.Length / 6) + bookTitleSpaceCount) != -1;

                    if ((seriesText.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) || isSimilar) && ((bookType == BookType.Manga && innerSeriesText.Contains("Graphic Novels")) || bookType == BookType.LightNovel))
                    {
                        links.Add($"https://www.animecornerstore.com/{series.GetAttributeValue("href", "Url Error")}");
                    }
                }

                if (links.Count != 0)
                {
                    bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                    foreach (string link in links)
                    {
                        LOGGER.Info($"Url = {link}");
                        doc = web.Load(link);

                        List<HtmlNode> titleData = doc.DocumentNode.SelectNodes(TitleXPath).Where(title => !string.IsNullOrWhiteSpace(title.InnerText)).ToList();
                        HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);

                        for (int x = 0; x < titleData.Count; x++)
                        {
                            string entryTitle = titleData[x].InnerText.Trim();
                            // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                            //InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) && 
                            if ((InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) 
                                || bookType == BookType.Manga && InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Spoof")
                                )
                                && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                                && ((bookType == BookType.Manga && entryTitle.Contains("Graphic Novel")) || (bookType == BookType.LightNovel && !entryTitle.Contains("Graphic Novel"))))
                            {
                                // LOGGER.Debug("{} | {}", entryTitle, priceData[x].InnerText.Trim());
                                if (!RobertsAnimeCornerStoreLinks.Contains(link)) { RobertsAnimeCornerStoreLinks.Add(link); }
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