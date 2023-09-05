using System.Threading.Tasks;

namespace MangaLightNovelWebScrape.Websites
{
    partial class RobertsAnimeCornerStore
    {
        public static List<string> robertsAnimeCornerStoreLinks = new();
        private static List<EntryModel> robertsAnimeCornerStoreDataList = new();
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
                        robertsAnimeCornerStoreLinks.Add(url);
                        state.Stop();
                    }
                });
            }
            else
            { //Gets the actual page that houses the data the user is looking for
                url = "https://www.animecornerstore.com/" + htmlString;
                robertsAnimeCornerStoreLinks.Add(url);
            }
            Logger.Debug(url);
            return url;
        }

        /**
         * TODO: Figure out a way to when checking for title for it to ignore case
         */
        private static string GetPageData(EdgeDriver edgeDriver, string bookTitle, char bookType, HtmlDocument doc, WebDriverWait wait)
        {
            string link = "";
            // string typeCheck = bookType == 'N' ? "not(contains(text()[2], ' Graphic'))" : "contains(text()[2], ' Graphic')";
            edgeDriver.Navigate().GoToUrl(GetUrl(bookTitle, false));
            wait.Until(e => e.FindElement(By.XPath($"//b//a[1]")));
            doc.LoadHtml(edgeDriver.PageSource);

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

        public static List<EntryModel> GetRobertsAnimeCornerStoreData(string bookTitle, char bookType, EdgeOptions edgeOptions)
        {
            EdgeDriver edgeDriver = new(edgeOptions);
            WebDriverWait wait = new(edgeDriver, TimeSpan.FromSeconds(30));

            // Initialize the html doc for crawling
            HtmlDocument doc = new();

            string linkPage = GetPageData(edgeDriver, FilterBookTitleRegex().Replace(bookTitle, ""), bookType, doc, wait);
            string errorMessage;
            if (string.IsNullOrEmpty(linkPage))
            {
                errorMessage = "Error! Invalid Series Title";
                Logger.Error(errorMessage);
                edgeDriver.Quit();
            }
            else
            {
                try
                {
                    // Start scraping the URL where the data is found
                    edgeDriver.Navigate().GoToUrl(linkPage);
                    wait.Until(e => e.FindElement(By.XPath("//font[@face='dom bold, arial, helvetica']/b")));

                    // Get the html doc for crawling
                    doc.LoadHtml(edgeDriver.PageSource);

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

                    edgeDriver.Quit();
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
                        
                        // if (currTitle.Contains("0"))
                        // {
                        //     robertsAnimeCornerStoreDataList.Add(new EntryModel(currTitle, "$3.00", stockStatus.Trim(), "RobertsAnimeCornerStore"));
                        //     continue;
                        // }

                        robertsAnimeCornerStoreDataList.Add(new EntryModel(currTitle, priceData[x].InnerText.Trim(), stockStatus.Trim(), "RobertsAnimeCornerStore"));
                    }

                    robertsAnimeCornerStoreDataList.Sort(new VolumeSort(bookTitle));
                    if (MasterScrape.IsDebugEnabled)
                    {
                        using (StreamWriter outputFile = new(@"Data\RobertsAnimeCornerStoreData.txt"))
                        {
                            if (robertsAnimeCornerStoreDataList.Count != 0)
                            {
                                foreach (EntryModel data in robertsAnimeCornerStoreDataList)
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
                catch(NullReferenceException ex)
                {
                    Logger.Error(bookTitle + " Does Not Exist at RobertsAnimeCornerStore\n" + ex);
                }
            }
            
            return robertsAnimeCornerStoreDataList;
        }
    }
}