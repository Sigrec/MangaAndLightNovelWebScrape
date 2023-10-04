namespace MangaLightNovelWebScrape.Websites.America
{
    public partial class RobertsAnimeCornerStore
    {
        private List<string> RobertsAnimeCornerStoreLinks = new List<string>(2);
        private List<EntryModel> RobertsAnimeCornerStoreData = new();
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

        [GeneratedRegex("-|\\s+")] private static partial Regex FilterBookTitleRegex();
        [GeneratedRegex(",|#| Graphic Novel| :|\\(.*?\\)|\\[Novel\\]")] private static partial Regex TitleFilterRegex();
        [GeneratedRegex(",| #\\d+-\\d+| #\\d+|Graphic Novel| :|\\(.*?\\)|\\[Novel\\]")] private static partial Regex OmnibusTitleFilterRegex();
        [GeneratedRegex("-(\\d+)")] private static partial Regex OmnibusVolNumberRegex();
        [GeneratedRegex("\\s+|[^a-zA-Z0-9]")] private static partial Regex FindTitleRegex();
        [GeneratedRegex("Official|Guidebook", RegexOptions.IgnoreCase)] private static partial Regex TitleRemovalRegex();

        internal async Task CreateRobertsAnimeCornerStoreTask(string bookTitle, BookType book, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetRobertsAnimeCornerStoreData(bookTitle, book, driver));
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
            LOGGER.Info(url);
            return url;
        }

        internal void ClearData()
        {
            if (this != null)
            {
                RobertsAnimeCornerStoreLinks.Clear();
                RobertsAnimeCornerStoreData.Clear();
            }
        }

        private static string TitleParse(string titleText, BookType book)
        {
            StringBuilder curTitle;
            if (titleText.Contains("Omnibus"))
            {
                uint volNum = Convert.ToUInt32(OmnibusVolNumberRegex().Match(titleText).Groups[1].Value);
                curTitle = new StringBuilder(OmnibusTitleFilterRegex().Replace(titleText, "").Trim());
                curTitle.Replace("Omnibus Edition", "Omnibus");
                if (!curTitle.ToString().Contains(" Vol"))
                {
                    curTitle.Append(' ');
                    curTitle.Append("Vol");
                }
                curTitle.Append(' ');
                curTitle.Append(volNum / 3);
            }
            else
            {
                titleText = TitleFilterRegex().Replace(titleText, "").Trim();
                curTitle = new StringBuilder(titleText);
                curTitle.Replace("Deluxe Edition", "Deluxe Vol");
                if (titleText.Contains("Box Set") && !titleText.Any(char.IsDigit))
                {
                    curTitle.Append(' ');
                    curTitle.Append('1');
                }
            }
            return book == BookType.Manga ? MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ") : MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Vol", "Novel Vol").ToString(), " ");
        }
        
        private List<EntryModel> GetRobertsAnimeCornerStoreData(string bookTitle, BookType bookType, WebDriver driver)
        {
            bool DriverHasQuit = false;
            try
            {
                // Start scraping the URL where the data is found
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                HtmlDocument doc = new HtmlDocument();
                string link = string.Empty;

                driver.Navigate().GoToUrl(GetUrl(bookTitle));
                wait.Until(e => e.FindElement(By.XPath("/html/body/center/table/tbody/tr/td/font/b/a")));
                doc.LoadHtml(driver.PageSource);

                foreach (HtmlNode series in doc.DocumentNode.SelectNodes(SeriesTitleXPath))
                {
                    string seriesText = series.InnerText;
                    if (MasterScrape.TitleContainsBookTitle(bookTitle, seriesText))
                    {
                        link = series.Attributes["href"].Value;
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(link))
                {
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.CssSelector($"[href='{link}']"))));
                    wait.Until(e => e.FindElement(By.CssSelector("tbody")));
                    doc.LoadHtml(driver.PageSource);
                    driver.Close();
                    driver.Quit();
                    DriverHasQuit = true;
                    
                    LOGGER.Info($"https://www.animecornerstore.com/{link}");
                    RobertsAnimeCornerStoreLinks.Add($"https://www.animecornerstore.com/{link}");

                    List<HtmlNode> titleData = doc.DocumentNode.SelectNodes(TitleXPath).Where(title => !string.IsNullOrWhiteSpace(title.InnerText)).ToList();
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string titleText = titleData[x].InnerText;
                        if (
                                TitleRemovalRegex().IsMatch(titleText)
                                || string.IsNullOrWhiteSpace(titleText) 
                                || titleText.Contains("Poster") 
                                || (
                                        titleText.Contains("[Novel]") 
                                        && bookType == BookType.Manga
                                    ) 
                                || (
                                        titleText.Contains("Graphic") 
                                        && bookType == BookType.LightNovel
                                    )
                            ) // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                        {
                            continue;
                        }

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

                    RobertsAnimeCornerStoreData.Sort(new VolumeSort());
                }
            }
            catch(Exception ex)
            {
                if (!DriverHasQuit)
                {
                    driver.Close();
                    driver.Quit();
                }
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
                            LOGGER.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
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