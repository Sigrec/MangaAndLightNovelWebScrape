using System.Threading.Tasks;

namespace MangaLightNovelWebScrape.Websites
{
    partial class RobertsAnimeCornerStore
    {
        public static List<string> RobertsAnimeCornerStoreLinks = new();
        public static List<EntryModel> RobertsAnimeCornerStoreData = new();
        public const string WEBSITE_TITLE = "RobertsAnimeCornerStore";
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("RobertsAnimeCornerStoreLogs");
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

        [GeneratedRegex("-|\\s+")] private static partial Regex FilterBookTitleRegex();
        [GeneratedRegex(",|#|Graphic Novel| :|\\(.*?\\)|\\[Novel\\]")] private static partial Regex TitleFilterRegex();
        [GeneratedRegex("[ ]{2,}")] private static partial Regex TitleFilterNumRegex();
        [GeneratedRegex("\\s+")] private static partial Regex GetWhiteSpaceRegex();
        
        private static string GetUrl(string htmlString, bool pageExists)
        {
            string url = "";
            if (!pageExists) // Gets the starting page based on first letter and checks if we are looking for the 1st webpage (false) or 2nd webpage containing the actual item data (true)
            {
                Parallel.ForEach(URL_MAP_DICT, (link, state) =>
                {
                    if (new Regex(link.Value).Match(htmlString).Success)
                    {
                        url = $"https://www.animecornerstore.com/{link.Key}.html";
                        RobertsAnimeCornerStoreLinks.Add(url);
                        state.Stop();
                    }
                });
            }
            else
            { //Gets the actual page that houses the data the user is looking for
                url = "https://www.animecornerstore.com/" + htmlString;
                RobertsAnimeCornerStoreLinks.Add(url);
            }
            Logger.Debug(url);
            return url;
        }

        public static void ClearData()
        {
            RobertsAnimeCornerStoreLinks.Clear();
            RobertsAnimeCornerStoreData.Clear();
        }

        /**
         * TODO: Figure out a way to when checking for title for it to ignore case
         */
        private static string GetPageData(WebDriver driver, string bookTitle, char bookType, HtmlDocument doc, WebDriverWait wait)
        {
            string link = "";
            // string typeCheck = bookType == 'N' ? "not(contains(text()[2], ' Graphic'))" : "contains(text()[2], ' Graphic')";
            driver.Navigate().GoToUrl(GetUrl(bookTitle, false));
            wait.Until(e => e.FindElement(By.XPath($"//b//a[1]")));
            doc.LoadHtml(driver.PageSource);

            HtmlNodeCollection seriesTitle = doc.DocumentNode.SelectNodes($"//b//a[1]");
            try
            {
                foreach (HtmlNode series in seriesTitle)
                {
                    //Logger.Debug(Regex.Replace(series.InnerText.ToLower(), @"\s+", ""));
                    if (GetWhiteSpaceRegex().Replace(series.InnerText.ToLower(), "").Contains(bookTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        link = GetUrl(series.Attributes["href"].Value, true);
                        return link;
                    }
                }
            }
            catch(NullReferenceException ex)
            {
                Logger.Error(ex);
            }
            return "DNE";
        }

        public static List<EntryModel> GetRobertsAnimeCornerStoreData(string bookTitle, char bookType)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(30));

            // Initialize the html doc for crawling
            HtmlDocument doc = new();

            string linkPage = GetPageData(driver, FilterBookTitleRegex().Replace(bookTitle, ""), bookType, doc, wait);
            string errorMessage;
            if (string.IsNullOrEmpty(linkPage))
            {
                errorMessage = "Error! Invalid Series Title";
                Logger.Error(errorMessage);
                driver.Close();
                driver.Quit();
            }
            else
            {
                try
                {
                    // Start scraping the URL where the data is found
                    driver.Navigate().GoToUrl(linkPage);
                    wait.Until(e => e.FindElement(By.XPath("//font[@face='dom bold, arial, helvetica']/b")));

                    // Get the html doc for crawling
                    doc.LoadHtml(driver.PageSource);

                    //Gets the title for each available item
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//font[@face='dom bold, arial, helvetica']/b/text()[1]");

                    // Gets the lowest price for each item, for loop removes the larger price
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//form[@method='POST'][contains(text()[2], '$')]/text()[2] | //font[2][@color='#ffcc33']");
                    for(int x = 0; x < priceData.Count; x++)
                    {
                        if (priceData[x].InnerText[0].Equals(' '))
                        {
                            priceData.RemoveAt(x);
                        }
                    }

                    driver.Close();
                    driver.Quit();
                    string currTitle, stockStatus;
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if (titleData[x].InnerText.Contains("[Novel]") && bookType == 'M' || titleData[x].InnerText.Contains("Graphic") && bookType == 'N') // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                        {
                            continue;
                        }

                        stockStatus = titleData[x].InnerText switch
                        {
                            string curTitle when curTitle.Contains("Pre Order") => "PO",
                             string curTitle when curTitle.Contains("Backorder") => "OOS",
                             _ => "IS"
                        };
                        currTitle = TitleFilterNumRegex().Replace(TitleFilterRegex().Replace(titleData[x].InnerText, ""), " ").Replace("Edition", "Vol").Trim();

                        if (currTitle.Contains("Omnibus") && currTitle.Contains("Vol"))
                        {
                            if (currTitle.Contains("One Piece") && currTitle.Contains("Vol 10-12")) // Fix naming issue with one piece
                            {
                                currTitle = $"{currTitle[..currTitle.IndexOf(" Vol")]}4";
                            }
                            else
                            {
                                currTitle = currTitle[..currTitle.IndexOf("Vol")];
                            }
                            currTitle = $"{currTitle[..$"{currTitle.IndexOf("Omnibus ")}Omnibus ".Length]}Vol {currTitle[(currTitle.IndexOf("Omnibus ") + "Omnibus ".Length)..]}".Trim();
                        }

                        RobertsAnimeCornerStoreData.Add(new EntryModel(currTitle, priceData[x].InnerText.Trim(), stockStatus.Trim(), WEBSITE_TITLE));
                    }

                    RobertsAnimeCornerStoreData.Sort(new VolumeSort(bookTitle));
                }
                catch(Exception ex)
                {
                    driver.Close();
                    driver.Quit();
                    Logger.Error(bookTitle + " Does Not Exist at RobertsAnimeCornerStore\n" + ex);
                }

                if (MasterScrape.IsDebugEnabled)
                {
                    using (StreamWriter outputFile = new(@"Data\RobertsAnimeCornerStoreData.txt"))
                    {
                        if (RobertsAnimeCornerStoreData.Count != 0)
                        {
                            foreach (EntryModel data in RobertsAnimeCornerStoreData)
                            {
                                Logger.Debug(data.ToString());
                                outputFile.WriteLine(data.ToString());
                            }
                        }
                        else
                        {
                            errorMessage = bookTitle + " Does Not Exist at RobertsAnimeCornerStore";
                            outputFile.WriteLine(errorMessage);
                        }
                    } 
                }
            }
            
            return RobertsAnimeCornerStoreData;
        }
    }
}