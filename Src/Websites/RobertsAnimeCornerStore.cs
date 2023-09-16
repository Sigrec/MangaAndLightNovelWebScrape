using System;
using System.Threading.Tasks;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class RobertsAnimeCornerStore
    {
        private static List<string> RobertsAnimeCornerStoreLinks = new();
        private static List<EntryModel> RobertsAnimeCornerStoreData = new();
        public const string WEBSITE_TITLE = "RobertsAnimeCornerStore";
        private static readonly Logger Logger = LogManager.GetLogger("RobertsAnimeCornerStoreLogs");
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
        [GeneratedRegex(",|#| Graphic Novel| :|\\(.*?\\)|\\[Novel\\]")] private static partial Regex TitleFilterRegex();
        [GeneratedRegex(",| #\\d+|Graphic Novel| :|\\(.*?\\)|\\[Novel\\]")] private static partial Regex OmnibusTitleFilterRegex();
        [GeneratedRegex("\\s{2,}")] private static partial Regex MultipleWhiteSpaceRegex();
        [GeneratedRegex("-(\\d+)")] private static partial Regex OmnibusVolNumberRegex();
        [GeneratedRegex("\\s+|[^a-zA-Z0-9]")] private static partial Regex FindTitleRegex();

        public static List<string> GetUrls()
        {
            return RobertsAnimeCornerStoreLinks;
        }
        
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
                url = $"https://www.animecornerstore.com/{htmlString}";
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
        private static string GetPageData(WebDriver driver, string bookTitle, Book book, HtmlDocument doc, WebDriverWait wait)
        {
            string link = "";
            driver.Navigate().GoToUrl(GetUrl(bookTitle, false));
            wait.Until(e => e.FindElement(By.XPath($"//b//a[1]")));
            doc.LoadHtml(driver.PageSource);

            HtmlNodeCollection seriesTitle = doc.DocumentNode.SelectNodes($"//b//a[1]");
            string seriesText;
            foreach (HtmlNode series in seriesTitle)
            {
                seriesText = series.InnerText;
                if (MasterScrape.TitleContainsBookTitle(bookTitle, seriesText))
                {
                    link = GetUrl(series.Attributes["href"].Value, true);
                    break;
                }
            }
            return link;
        }

        private static string ParseTitle(string curTitle, Book book)
        {
            if (curTitle.Contains("Omnibus"))
            {
                ushort volNum = Convert.ToUInt16(OmnibusVolNumberRegex().Match(curTitle).Groups[1].Value);
                curTitle = OmnibusTitleFilterRegex().Replace(curTitle, "").Replace("Omnibus Edition", "Omnibus").Trim();
                if (curTitle.LastIndexOf("Vol") != -1)
                {
                    curTitle = $"{curTitle[..(curTitle.LastIndexOf("Vol") + 3)]} {volNum / 3}";
                }
                else
                {
                    curTitle = $"{curTitle} Vol {volNum / 3}";
                }
            }
            else
            {
                curTitle = TitleFilterRegex().Replace(curTitle, "").Trim();
                if (curTitle.Contains("Deluxe Edition"))
                {
                    curTitle = curTitle.Replace("Edition", "Vol");
                }
                else if (curTitle.Contains("Box Set") && !curTitle.Any(char.IsAsciiDigit))
                {
                    curTitle = $"{curTitle} 1";
                }
            }
            return book == Book.Manga ? MultipleWhiteSpaceRegex().Replace(curTitle, " ") : MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Vol", "Novel Vol"), " ");
        }
        
        public static List<EntryModel> GetRobertsAnimeCornerStoreData(string bookTitle, Book book)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);
            WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));

            // Initialize the html doc for crawling
            HtmlDocument doc = new();

            string linkPage = GetPageData(driver, FilterBookTitleRegex().Replace(bookTitle, ""), book, doc, wait);
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

                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//font[@face='dom bold, arial, helvetica']/b");

                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//form[@method='POST'][contains(text()[2], '$')]//font[@color='#ffcc33'][2]"); // //form[@method='POST'][contains(text()[2], '$')]/text()[2] | //font[2][@color='#ffcc33']
                    driver.Close();
                    driver.Quit();

                    // Remove All Empty Titles
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if (string.IsNullOrWhiteSpace(titleData[x].InnerText))
                        {
                            titleData.RemoveAt(x);
                        }
                    }

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        if (string.IsNullOrWhiteSpace(titleData[x].InnerText) || titleData[x].InnerText.Contains("Poster") || (titleData[x].InnerText.Contains("[Novel]") && book == Book.Manga) || (titleData[x].InnerText.Contains("Graphic") && book == Book.LightNovel)) // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                        {
                            continue;
                        }

                        RobertsAnimeCornerStoreData.Add(
                            new EntryModel(
                                ParseTitle(titleData[x].InnerText, book),
                                priceData[x].InnerText.Trim(),
                                titleData[x].InnerText switch
                                {
                                    string curTitle when curTitle.Contains("Pre Order") => "PO",
                                    string curTitle when curTitle.Contains("Backorder") => "OOS",
                                    _ => "IS"
                                },
                                WEBSITE_TITLE
                            )
                        );
                    }

                    RobertsAnimeCornerStoreData.Sort(new VolumeSort());
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